﻿using System;
using System.Collections.Generic;
using EVESharp.EVE;
using EVESharp.EVE.Packets.Exceptions;
using EVESharp.Node.Database;
using EVESharp.Node.Exceptions.jumpCloneSvc;
using EVESharp.Node.Inventory;
using EVESharp.Node.Inventory.Items;
using EVESharp.Node.Inventory.Items.Types;
using EVESharp.Node.Market;
using EVESharp.Node.Network;
using EVESharp.Node.Notifications.Client.Clones;
using EVESharp.Node.StaticData.Inventory;
using EVESharp.Node.Exceptions;
using EVESharp.Node.Inventory.SystemEntities;
using EVESharp.Node.Services.Account;
using EVESharp.Node.StaticData;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Database;
using EVESharp.PythonTypes.Types.Primitives;
using Type = EVESharp.Node.StaticData.Inventory.Type;

namespace EVESharp.Node.Services.Characters
{
    public class jumpCloneSvc : ClientBoundService
    {
        private ItemDB ItemDB { get; }
        private MarketDB MarketDB { get; }
        private ItemFactory ItemFactory { get; }
        private TypeManager TypeManager => this.ItemFactory.TypeManager;
        private SystemManager SystemManager { get; }
        private NotificationManager NotificationManager { get; }
        private WalletManager WalletManager { get; }
        
        public jumpCloneSvc(ItemDB itemDB, MarketDB marketDB, ItemFactory itemFactory,
            SystemManager systemManager, WalletManager walletManager, NotificationManager notificationManager, BoundServiceManager manager) : base(manager)
        {
            this.ItemDB = itemDB;
            this.MarketDB = marketDB;
            this.ItemFactory = itemFactory;
            this.SystemManager = systemManager;
            this.WalletManager = walletManager;
            this.NotificationManager = notificationManager;
        }
        
        protected jumpCloneSvc(int locationID, ItemDB itemDB, MarketDB marketDB, ItemFactory itemFactory,
            SystemManager systemManager, BoundServiceManager manager, WalletManager walletManager, NotificationManager notificationManager, Client client) : base(manager, client, locationID)
        {
            this.ItemDB = itemDB;
            this.MarketDB = marketDB;
            this.ItemFactory = itemFactory;
            this.SystemManager = systemManager;
            this.WalletManager = walletManager;
            this.NotificationManager = notificationManager;
        }

        /// <summary>
        /// Sends a OnJumpCloneCacheInvalidated notification to the specified character so the clone window
        /// is reloaded
        /// </summary>
        /// <param name="characterID">The character to notify</param>
        public void OnCloneUpdate(int characterID)
        {
            this.NotificationManager.NotifyCharacter(characterID, new OnJumpCloneCacheInvalidated());
        }

        public PyDataType GetCloneState(CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            
            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);

            return KeyVal.FromDictionary(new PyDictionary
                {
                    ["clones"] = this.ItemDB.GetClonesForCharacter(callerCharacterID, (int) character.ActiveCloneID),
                    ["implants"] = this.ItemDB.GetImplantsForCharacterClones(callerCharacterID),
                    ["timeLastJump"] = character.TimeLastJump
                }
            );
        }

        public PyDataType DestroyInstalledClone(PyInteger jumpCloneID, CallInformation call)
        {
            // if the clone is not loaded the clone cannot be removed, players can only remove clones from where they're at
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            
            if (this.ItemFactory.TryGetItem(jumpCloneID, out ItemEntity clone) == false)
                throw new JumpCantDestroyNonLocalClone();
            if (clone.LocationID != call.Client.LocationID)
                throw new JumpCantDestroyNonLocalClone();
            if (clone.OwnerID != callerCharacterID)
                throw new MktNotOwner();

            // finally destroy the clone, this also destroys all the implants in it
            this.ItemFactory.DestroyItem(clone);
            
            // let the client know that the clones were updated
            this.OnCloneUpdate(callerCharacterID);
            
            return null;
        }

        public PyDataType GetShipCloneState(CallInformation call)
        {
            return this.ItemDB.GetClonesInShipForCharacter(call.Client.EnsureCharacterIsSelected());
        }

        public PyDataType CloneJump(PyInteger locationID, PyBool unknown, CallInformation call)
        {
            // TODO: IMPLEMENT THIS CALL PROPERLY, INVOLVES SESSION CHANGES
            // TODO: AND SEND PROPER NOTIFICATION AFTER A JUMP CLONE OnJumpCloneTransitionCompleted
            return null;
        }

        public PyInteger GetPriceForClone(CallInformation call)
        {
            // TODO: CALCULATE THIS ON POS, AS THIS VALUE IS STATIC OTHERWISE
            
            // seems to be hardcoded for npc's stations
            return 100000;
        }

        public PyDataType InstallCloneInStation(CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            int stationID = call.Client.EnsureCharacterIsInStation();
            
            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);
            
            // check the maximum number of clones the character has assigned
            long maximumClonesAvailable = character.GetSkillLevel(Types.InfomorphPsychology);
            
            // the skill is not trained
            if (maximumClonesAvailable == 0)
                throw new JumpCharStoringMaxClonesNone();

            // get list of clones (excluding the medical clone)
            Rowset clones = this.ItemDB.GetClonesForCharacter(character.ID, (int) character.ActiveCloneID);

            // ensure we don't have more than the allowed clones
            if (clones.Rows.Count >= maximumClonesAvailable)
                throw new JumpCharStoringMaxClones(clones.Rows.Count, maximumClonesAvailable);
            
            // ensure that the character has enough money
            int cost = this.GetPriceForClone(call);
            
            // get character's station
            Station station = this.ItemFactory.GetStaticStation(stationID);

            using Wallet wallet = this.WalletManager.AcquireWallet(character.ID, WalletKeys.MAIN_WALLET);
            {
                wallet.EnsureEnoughBalance(cost);
                wallet.CreateJournalRecord(
                    MarketReference.JumpCloneInstallationFee, null, station.ID, -cost, $"Installed clone at {station.Name}"
                );
            }
            
            // create an alpha clone
            StaticData.Inventory.Type cloneType = this.TypeManager[Types.CloneGradeAlpha];
            
            // create a new clone on the itemDB
            Clone clone = this.ItemFactory.CreateClone(cloneType, station, character);

            // finally create the jump clone and invalidate caches
            this.OnCloneUpdate(callerCharacterID);
            
            // persist the character information
            character.Persist();
            
            return null;
        }

        protected override long MachoResolveObject(ServiceBindParams parameters, CallInformation call)
        {
            int solarSystemID = 0;

            if (parameters.ExtraValue == (int) Groups.SolarSystem)
                solarSystemID = this.ItemFactory.GetStaticSolarSystem(parameters.ObjectID).ID;
            else if (parameters.ExtraValue == (int) Groups.Station)
                solarSystemID = this.ItemFactory.GetStaticStation(parameters.ObjectID).SolarSystemID;
            else
                throw new CustomError("Unknown item's groupID");

            if (this.SystemManager.SolarSystemBelongsToUs(solarSystemID) == true)
                return this.BoundServiceManager.Container.NodeID;

            return this.SystemManager.GetNodeSolarSystemBelongsTo(solarSystemID);
        }

        protected override BoundService CreateBoundInstance(ServiceBindParams bindParams, CallInformation call)
        {
            // ensure this node will take care of the instance
            long nodeID = this.MachoResolveObject(bindParams, call);

            if (nodeID != this.BoundServiceManager.Container.NodeID)
                throw new CustomError("Trying to bind an object that does not belong to us!");

            return new jumpCloneSvc(bindParams.ObjectID, this.ItemDB, this.MarketDB, this.ItemFactory, this.SystemManager, this.BoundServiceManager, this.WalletManager, this.NotificationManager, call.Client);
        }
    }
}