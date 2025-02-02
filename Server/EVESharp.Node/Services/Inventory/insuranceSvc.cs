﻿using System;
using EVESharp.EVE;
using EVESharp.EVE.Packets.Exceptions;
using EVESharp.Node.Chat;
using EVESharp.Node.Database;
using EVESharp.Node.Exceptions.insuranceSvc;
using EVESharp.Node.Exceptions.jumpCloneSvc;
using EVESharp.Node.Inventory;
using EVESharp.Node.Inventory.Items.Types;
using EVESharp.Node.Market;
using EVESharp.Node.Network;
using EVESharp.Node.Services.Account;
using EVESharp.Node.Services.Chat;
using EVESharp.Node.StaticData.Inventory;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Database;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Services.Inventory
{
    public class insuranceSvc : ClientBoundService
    {
        private int mStationID = 0;
        private InsuranceDB DB { get; }
        private ItemFactory ItemFactory { get; }
        private MarketDB MarketDB { get; }
        private SystemManager SystemManager => this.ItemFactory.SystemManager;
        private WalletManager WalletManager { get; }
        private MailManager MailManager { get; }

        public insuranceSvc(ItemFactory itemFactory, InsuranceDB db, MarketDB marketDB, WalletManager walletManager, MailManager mailManager, BoundServiceManager manager, MachoNet machoNet) : base(manager)
        {
            this.DB = db;
            this.ItemFactory = itemFactory;
            this.MarketDB = marketDB;
            this.WalletManager = walletManager;
            this.MailManager = mailManager;

            machoNet.OnClusterTimer += PerformTimedEvents;
        }

        protected insuranceSvc(ItemFactory itemFactory, InsuranceDB db, MarketDB marketDB, WalletManager walletManager, MailManager mailManager, BoundServiceManager manager, int stationID, Client client) : base (manager, client, stationID)
        {
            this.mStationID = stationID;
            this.DB = db;
            this.ItemFactory = itemFactory;
            this.MarketDB = marketDB;
            this.WalletManager = walletManager;
            this.MailManager = mailManager;
        }

        public PyList<PyPackedRow> GetContracts(CallInformation call)
        {
            if (this.mStationID == 0)
            {
                int? shipID = call.Client.ShipID;
                
                if (shipID is null)
                    throw new CustomError($"The character is not onboard any ship");

                return new PyList<PyPackedRow>(1)
                {
                    [0] = this.DB.GetContractForShip(call.Client.EnsureCharacterIsSelected(), (int) shipID)
                };
            }
            else
            {
                return this.DB.GetContractsForShipsOnStation(call.Client.EnsureCharacterIsSelected(), this.mStationID);
            }
        }

        public PyPackedRow GetContractForShip(PyInteger itemID, CallInformation call)
        {
            return this.DB.GetContractForShip(call.Client.EnsureCharacterIsSelected(), itemID);
        }

        public PyList<PyPackedRow> GetContracts(PyInteger includeCorp, CallInformation call)
        {
            if (includeCorp == 0)
                return this.DB.GetContractsForShipsOnStation(call.Client.EnsureCharacterIsSelected(), this.mStationID);
            else
                return this.DB.GetContractsForShipsOnStationIncludingCorp(call.Client.EnsureCharacterIsSelected(), call.Client.CorporationID, this.mStationID);
        }

        public PyBool InsureShip(PyInteger itemID, PyDecimal insuranceCost, PyInteger isCorpItem, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            
            if (this.ItemFactory.TryGetItem(itemID, out Ship item) == false)
                throw new CustomError("Ships not loaded for player and hangar!");

            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);

            if (isCorpItem == 1 && item.OwnerID != call.Client.CorporationID && item.OwnerID != callerCharacterID)
                throw new MktNotOwner();

            if (item.Singleton == false)
                throw new InsureShipFailed("Only assembled ships can be insured");

            if (this.DB.IsShipInsured(item.ID, out int oldOwnerID, out int numberOfInsurances) == true && (call.NamedPayload.TryGetValue("voidOld", out PyBool voidOld) == false || voidOld == false))
            {
                // throw the proper exception based on the number of insurances available
                if (numberOfInsurances > 1)
                    throw new InsureShipFailedMultipleContracts();
                
                throw new InsureShipFailedSingleContract(oldOwnerID);
            }

            using Wallet wallet = this.WalletManager.AcquireWallet(character.ID, WalletKeys.MAIN_WALLET);
            {
                wallet.EnsureEnoughBalance(insuranceCost);
                wallet.CreateJournalRecord(
                    MarketReference.Insurance, this.ItemFactory.OwnerSCC.ID, -item.ID, -insuranceCost, $"Insurance fee for {item.Name}"
                );
            }
            
            // insurance was charged to the player, so old insurances can be void now
            this.DB.UnInsureShip(item.ID);

            double fraction = insuranceCost * 100 / item.Type.BasePrice;

            // create insurance record
            DateTime expirationTime = DateTime.UtcNow.AddDays(7 * 12);
            int referenceID = this.DB.InsureShip(item.ID, isCorpItem == 0 ? callerCharacterID : call.Client.CorporationID, fraction / 5, expirationTime);

            // TODO: CHECK IF THE INSURANCE SHOULD BE CHARGED TO THE CORP
            
            this.MailManager.SendMail(this.ItemFactory.OwnerSCC.ID, callerCharacterID, 
                "Insurance Contract Issued",
                "Dear valued customer, <br><br>" +
                "Congratulations on the insurance on your ship. A very wise choice indeed.<br>" +
                $"This letter is to confirm that we have issued an insurance contract for your ship, <b>{item.Name}</b> (<b>{item.Type.Name}</b>) at a level of {fraction * 100 / 30}%.<br>" +
                $"This contract will expire at <b>{expirationTime.ToLongDateString()} {expirationTime.ToShortTimeString()}</b>, after 12 weeks.<br><br>" +
                "Best,<br>" +
                "The Secure Commerce Commission<br>" +
                $"Reference ID: <b>{referenceID}</b>"
            );

            return true;
        }

        public PyDataType UnInsureShip(PyInteger itemID, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            
            if (this.ItemFactory.TryGetItem(itemID, out Ship item) == false)
                throw new CustomError("Ships not loaded for player and hangar!");

            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);

            if (item.OwnerID != call.Client.CorporationID && item.OwnerID != callerCharacterID)
                throw new MktNotOwner();

            // remove insurance record off the database
            this.DB.UnInsureShip(itemID);
            
            return null;
        }

        public void PerformTimedEvents(object? sender, EventArgs args)
        {
            foreach (InsuranceDB.ExpiredContract contract in this.DB.GetExpiredContracts())
            {
                DateTime insuranceTime = DateTime.FromFileTimeUtc(contract.StartDate);
                
                this.MailManager.SendMail(this.ItemFactory.OwnerSCC.ID, contract.OwnerID, 
                    "Insurance Contract Expired",
                    "Dear valued customer, <br><br>" +
                    $"The insurance contract between yourself and SCC for the insurance of the ship <b>{contract.ShipName}</b> (<b>{contract.ShipType.Name}</b>) issued at" +
                    $" <b>{insuranceTime.ToLongDateString()} {insuranceTime.ToShortTimeString()}</b> has expired." +
                    "Please purchase a new insurance as quickly as possible to protect your investment.<br><br>" +
                    "Best,<br>" +
                    "The Secure Commerce Commission<br>" +
                    $"Reference ID: <b>{contract.InsuranceID}</b>"
                );
            }
        }

        protected override long MachoResolveObject(ServiceBindParams parameters, CallInformation call)
        {
            int solarSystemID = this.ItemFactory.GetStaticStation(parameters.ObjectID).SolarSystemID;

            if (this.SystemManager.SolarSystemBelongsToUs(solarSystemID) == true)
                return this.BoundServiceManager.Container.NodeID;

            return this.SystemManager.GetNodeSolarSystemBelongsTo(solarSystemID);
        }

        protected override BoundService CreateBoundInstance(ServiceBindParams bindParams, CallInformation call)
        {
            if (this.MachoResolveObject(bindParams, call) != this.BoundServiceManager.Container.NodeID)
                throw new CustomError("Trying to bind an object that does not belong to us!");
            
            return new insuranceSvc(this.ItemFactory, this.DB, this.MarketDB, this.WalletManager, this.MailManager, this.BoundServiceManager, bindParams.ObjectID, call.Client);
        }
    }
}