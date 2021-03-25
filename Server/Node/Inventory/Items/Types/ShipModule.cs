﻿using System;
using System.Runtime.InteropServices;
using Node.Dogma.Interpreter;
using Node.Dogma.Interpreter.Opcodes;
using Node.Exceptions.dogma;
using Node.Inventory.Items.Attributes;
using Node.Inventory.Items.Dogma;
using Node.Inventory.Notifications;
using PythonTypes.Types.Collections;
using PythonTypes.Types.Exceptions;
using PythonTypes.Types.Primitives;

namespace Node.Inventory.Items.Types
{
    public class ShipModule : ItemEntity
    {
        public GodmaShipEffectsList Effects { get; }
        
        public ShipModule(ItemEntity @from) : base(@from)
        {
            this.Effects = new GodmaShipEffectsList();

            foreach ((int effectID, Effect effect) in this.Type.Effects)
            {
                // create effects entry in the list
                this.Effects[effectID] = new GodmaShipEffect()
                {
                    AffectedItem = this,
                    Effect = effect,
                    ShouldStart = false,
                    StartTime = 0,
                    Duration = 0,
                };
            }
            
            // special case, check for the isOnline attribute and put the module online if so
            if (this.Attributes[AttributeEnum.isOnline] == 1)
            {
                GodmaShipEffect effect = this.Effects[16];

                effect.ShouldStart = true;
                effect.StartTime = DateTime.UtcNow.ToFileTimeUtc();
            }
        }
        
        public override PyDictionary GetEffects()
        {
            return this.Effects;
        }

        public void ApplyEffect(string effectName, Client forClient)
        {
            // check if the module has the given effect in it's list
            if (this.Type.EffectsByName.TryGetValue(effectName, out Effect effect) == false)
                throw new EffectNotActivatible(this.Type);
            if (this.Effects.TryGetEffect(effect.EffectID, out GodmaShipEffect godmaEffect) == false)
                throw new CustomError("Cannot apply the given effect, our type has it but we dont");
            
            this.ApplyEffect(effect, godmaEffect, forClient);
        }

        private void ApplyEffect(Effect effect, GodmaShipEffect godmaEffect, Client forClient)
        {
            if (godmaEffect.ShouldStart == true)
                return;
            
            Ship ship = this.ItemFactory.ItemManager.GetItem<Ship>((int) forClient.ShipID);
            Character character = this.ItemFactory.ItemManager.GetItem<Character>(forClient.EnsureCharacterIsSelected());
            
            // create the environment for this run
            Node.Dogma.Interpreter.Environment env = new Node.Dogma.Interpreter.Environment()
            {
                Character = character,
                Self = this,
                Ship = ship,
                Target = null,
                Client = forClient
            };

            Opcode opcode = new Interpreter(env).Run(effect.PreExpression.VMCode);
            
            if (opcode is OpcodeRunnable runnable)
                runnable.Execute();
            else if (opcode is OpcodeWithBooleanOutput booleanOutput)
                booleanOutput.Execute();
            else if (opcode is OpcodeWithDoubleOutput doubleOutput)
                doubleOutput.Execute();

            // ensure the module is saved
            this.Persist();

            PyDataType duration = 0;

            if (effect.DurationAttributeID is not null)
                duration = this.Attributes[(int) effect.DurationAttributeID];

            // update things like duration, start, etc
            godmaEffect.StartTime = DateTime.UtcNow.ToFileTimeUtc();
            godmaEffect.ShouldStart = true;
            godmaEffect.Duration = duration;
            
            // notify the client about it
            forClient.NotifyMultiEvent(new OnGodmaShipEffect(godmaEffect));
            
            if (effect.EffectID == (int) EffectsEnum.Online)
                this.ApplyOnlineEffects(forClient);
        }

        public void StopApplyingEffect(string effectName, Client forClient)
        {
            // check if the module has the given effect in it's list
            if (this.Type.EffectsByName.TryGetValue(effectName, out Effect effect) == false)
                throw new EffectNotActivatible(this.Type);
            if (this.Effects.TryGetEffect(effect.EffectID, out GodmaShipEffect godmaEffect) == false)
                throw new CustomError("Cannot apply the given effect, our type has it but we dont");

            this.StopApplyingEffect(effect, godmaEffect, forClient);
        }

        private void StopApplyingEffect(Effect effect, GodmaShipEffect godmaEffect, Client forClient)
        {
            // ensure the effect is being applied before doing anything
            if (godmaEffect.ShouldStart == false)
                return;
            
            Ship ship = this.ItemFactory.ItemManager.GetItem<Ship>((int) forClient.ShipID);
            Character character = this.ItemFactory.ItemManager.GetItem<Character>(forClient.EnsureCharacterIsSelected());
            
            // create the environment for this run
            Node.Dogma.Interpreter.Environment env = new Node.Dogma.Interpreter.Environment()
            {
                Character = character,
                Self = this,
                Ship = ship,
                Target = null,
                Client = forClient
            };

            Opcode opcode = new Interpreter(env).Run(effect.PostExpression.VMCode);
            
            if (opcode is OpcodeRunnable runnable)
                runnable.Execute();
            else if (opcode is OpcodeWithBooleanOutput booleanOutput)
                booleanOutput.Execute();
            else if (opcode is OpcodeWithDoubleOutput doubleOutput)
                doubleOutput.Execute();

            // ensure the module is saved
            this.Persist();
                
            // update things like duration, start, etc
            godmaEffect.StartTime = 0;
            godmaEffect.ShouldStart = false;
            godmaEffect.Duration = 0;
            
            // notify the client about it
            forClient.NotifyMultiEvent(new OnGodmaShipEffect(godmaEffect));

            // online effect, this requires some special processing as all the passive effects should also be applied
            if (effect.EffectID == (int) EffectsEnum.Online)
                this.StopApplyingOnlineEffects(forClient);
        }

        private void ApplyEffectsByCategory(EffectCategory category, Client forClient)
        {
            foreach ((int _, GodmaShipEffect effect) in this.Effects)
                if (effect.Effect.EffectCategory == category)
                    this.ApplyEffect(effect.Effect, effect, forClient);
        }

        private void StopApplyingEffectsByCategory(EffectCategory category, Client forClient)
        {
            foreach ((int _, GodmaShipEffect effect) in this.Effects)
                if (effect.Effect.EffectCategory == category)
                    this.StopApplyingEffect(effect.Effect, effect, forClient);
        }
        
        private void ApplyOnlineEffects(Client forClient)
        {
            this.ApplyEffectsByCategory(EffectCategory.Online, forClient);
        }

        private void StopApplyingOnlineEffects(Client forClient)
        {
            this.StopApplyingEffectsByCategory(EffectCategory.Online, forClient);
        }

        public void ApplyPassiveEffects(Client forClient)
        {
            this.ApplyEffectsByCategory(EffectCategory.Passive, forClient);
        }

        public void StopApplyingPassiveEffects(Client forClient)
        {
            this.StopApplyingEffectsByCategory(EffectCategory.Passive, forClient);
        }
    }
}