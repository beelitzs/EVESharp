﻿using System;
using EVESharp.EVE.Packets.Exceptions;
using EVESharp.Node.Exceptions.ship;
using EVESharp.Node.Inventory;
using EVESharp.Node.Inventory.Items;
using EVESharp.Node.Inventory.Items.Types;
using EVESharp.Node.Network;
using EVESharp.Node.Notifications.Client.Inventory;
using EVESharp.Node.StaticData.Inventory;
using EVESharp.Node.Exceptions.jumpCloneSvc;
using EVESharp.Node.StaticData;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Primitives;
using Type = EVESharp.Node.StaticData.Inventory.Type;

namespace EVESharp.Node.Services.Inventory
{
    public class ship : ClientBoundService
    {
        private ItemEntity Location { get; }
        private ItemFactory ItemFactory { get; }
        private TypeManager TypeManager => this.ItemFactory.TypeManager;
        private SystemManager SystemManager => this.ItemFactory.SystemManager;
        public ship(ItemFactory itemFactory, BoundServiceManager manager) : base(manager)
        {
            this.ItemFactory = itemFactory;
        }

        protected ship(ItemEntity location, ItemFactory itemFactory, BoundServiceManager manager, Client client) : base(manager, client, location.ID)
        {
            this.Location = location;
            this.ItemFactory = itemFactory;
        }

        public PyInteger LeaveShip(CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);
            // get the item type
            StaticData.Inventory.Type capsuleType = this.TypeManager[Types.Capsule];
            // create a pod for this character
            ItemInventory capsule = this.ItemFactory.CreateShip(capsuleType, this.Location, character);
            // update capsule's name
            capsule.Name = character.Name + "'s Capsule";
            // change character's location to the pod
            character.LocationID = capsule.ID;
            // notify the client about the item changes
            call.Client.NotifyMultiEvent(OnItemChange.BuildLocationChange(capsule, Flags.Capsule, capsule.LocationID));
            call.Client.NotifyMultiEvent(OnItemChange.BuildLocationChange(character, Flags.Pilot, call.Client.ShipID));
            // update session
            call.Client.ShipID = capsule.ID;
            
            // persist changes!
            capsule.Persist();
            character.Persist();
            
            // TODO: CHECKS FOR IN-SPACE LEAVING!

            return capsule.ID;
        }

        public PyDataType Board(PyInteger itemID, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            // ensure the item is loaded somewhere in this node
            // this will usually be taken care by the EVE Client
            if (this.ItemFactory.TryGetItem(itemID, out Ship newShip) == false)
                throw new CustomError("Ships not loaded for player and hangar!");

            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);
            Ship currentShip = this.ItemFactory.GetItem<Ship>((int) call.Client.ShipID);

            if (newShip.Singleton == false)
                throw new CustomError("TooFewSubSystemsToUndock");

            // TODO: CHECKS FOR IN-SPACE BOARDING!
            
            // check skills required to board the given ship
            newShip.EnsureOwnership(callerCharacterID, call.Client.CorporationID, call.Client.CorporationRole, true);
            newShip.CheckPrerequisites(character);
            
            // move the character into this new ship
            character.LocationID = newShip.ID;
            // finally update the session
            call.Client.ShipID = newShip.ID;
            // notify the client about the change in location
            call.Client.NotifyMultiEvent(OnItemChange.BuildLocationChange(character, Flags.Pilot, currentShip.ID));

            character.Persist();

            // ensure the character is not removed when the capsule is removed
            currentShip.RemoveItem(character);

            if (currentShip.Type.ID == (int) Types.Capsule)
            {
                // destroy the pod from the database
                this.ItemFactory.DestroyItem(currentShip);
                // notify the player of the item change
                call.Client.NotifyMultiEvent(OnItemChange.BuildLocationChange(currentShip, this.Location.ID));
            }
            
            return null;
        }

        public PyDataType AssembleShip(PyInteger itemID, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            int stationID = call.Client.EnsureCharacterIsInStation();
            
            // ensure the item is loaded somewhere in this node
            // this will usually be taken care by the EVE Client
            if (this.ItemFactory.TryGetItem(itemID, out Ship ship) == false)
                throw new CustomError("Ships not loaded for player and hangar!");

            Character character = this.ItemFactory.GetItem<Character>(callerCharacterID);

            if (ship.OwnerID != callerCharacterID)
                throw new AssembleOwnShipsOnly(ship.OwnerID);

            // do not do anything if item is already assembled
            if (ship.Singleton == true)
                return new ShipAlreadyAssembled(ship.Type);

            // first split the stack
            if (ship.Quantity > 1)
            {
                // subtract one off the stack
                ship.Quantity -= 1;
                ship.Persist();
                // notify the quantity change
                call.Client.NotifyMultiEvent(OnItemChange.BuildQuantityChange(ship, ship.Quantity + 1));
  
                // create the new item in the database
                Station station = this.ItemFactory.GetStaticStation(stationID);
                ship = this.ItemFactory.CreateShip(ship.Type, station, character);
                // notify the new item
                call.Client.NotifyMultiEvent(OnItemChange.BuildNewItemChange(ship));
            }
            else
            {
                // stack of one, simple as changing the singleton flag
                ship.Singleton = true;
                call.Client.NotifyMultiEvent(OnItemChange.BuildSingletonChange(ship, false));                
            }

            // save the ship
            ship.Persist();

            return null;
        }

        public PyDataType AssembleShip(PyList itemIDs, CallInformation call)
        {
            foreach (PyInteger itemID in itemIDs.GetEnumerable<PyInteger>())
                this.AssembleShip(itemID, call);

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
            if (this.MachoResolveObject(bindParams, call) != this.BoundServiceManager.Container.NodeID)
                throw new CustomError("Trying to bind an object that does not belong to us!");

            if (bindParams.ExtraValue != (int) Groups.Station && bindParams.ExtraValue != (int) Groups.SolarSystem)
                throw new CustomError("Cannot bind ship service to non-solarsystem and non-station locations");
            if (this.ItemFactory.TryGetItem(bindParams.ObjectID, out ItemEntity location) == false)
                throw new CustomError("This bind request does not belong here");

            if (location.Type.Group.ID != bindParams.ExtraValue)
                throw new CustomError("Location and group do not match");

            return new ship(location, this.ItemFactory, this.BoundServiceManager, call.Client);
        }
    }
}