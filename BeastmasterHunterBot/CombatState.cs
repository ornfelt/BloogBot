// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// The namespace BeastMasterHunterBot contains classes related to the combat state of the Beast Master Hunter bot.
/// </summary>
namespace BeastMasterHunterBot
{
    /// <summary>
    /// Represents a combat state in the game. Inherits from CombatStateBase and implements IBotState.
    /// </summary>
    /// <summary>
    /// Represents a combat state in the game. Inherits from CombatStateBase and implements IBotState.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The Lua script for auto attacking. If the current action is not '84', it casts the 'Attack' spell.
        /// </summary>
        const string AutoAttackLuaScript = "if IsCurrentAction('84') == nil then CastSpellByName('Attack') end";
        /// <summary>
        /// The Lua script for the gun, which casts the 'Auto Shot' spell if the player is not already performing an auto-repeat action.
        /// </summary>
        const string GunLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Auto Shot') end"; // 8-35 yards
        /// <summary>
        /// Represents the error message for when the target is not in line of sight.
        /// </summary>
        const string LosErrorMessage = "Target not in line of sight";
        /// <summary>
        /// Error message displayed when attempting to fire without ammo in the paper doll ammo slot.
        /// </summary>
        const string OutOfAmmoErrorMessage = "Ammo needs to be in the paper doll ammo slot before it can be fired";

        /// <summary>
        /// The constant string representing "Raptor Strike".
        /// </summary>
        const string RaptorStrike = "Raptor Strike";
        /// <summary>
        /// Represents the constant string value for "Arcane Shot".
        /// </summary>
        const string ArcaneShot = "Arcane Shot";
        /// <summary>
        /// Represents the constant string "Serpent Sting".
        /// </summary>
        const string SerpentSting = "Serpent Sting";
        /// <summary>
        /// Represents the constant string "Multi-Shot".
        /// </summary>
        const string MultiShot = "Multi-Shot";
        /// <summary>
        /// Represents the constant string value for "Immolation Trap".
        /// </summary>
        const string ImmolationTrap = "Immolation Trap";
        /// <summary>
        /// Represents the constant string "Mongoose Bite".
        /// </summary>
        const string MongooseBite = "Mongoose Bite";
        /// <summary>
        /// Represents the constant string "Hunter's Mark".
        /// </summary>
        const string HuntersMark = "Hunter's Mark";
        /// <summary>
        /// Represents a constant string named "Parry".
        /// </summary>
        const string Parry = "Parry";
        /// <summary>
        /// Represents the constant string "Rapid Fire".
        /// </summary>
        const string RapidFire = "Rapid Fire";
        /// <summary>
        /// Represents the constant string value for "Concussive Shot".
        /// </summary>
        const string ConcussiveShot = "Concussive Shot";
        /// <summary>
        /// Represents the constant string "Scare Beast".
        /// </summary>
        const string ScareBeast = "Scare Beast";
        /// <summary>
        /// Represents the constant string value for "Aspect of the Hawk".
        /// </summary>
        const string AspectOfTheHawk = "Aspect of the Hawk";
        /// <summary>
        /// Represents the constant string "Call Pet".
        /// </summary>
        const string CallPet = "Call Pet";
        /// <summary>
        /// Represents the constant string "Mend Pet".
        /// </summary>
        const string MendPet = "Mend Pet";
        /// <summary>
        /// Represents the constant string value for "Distracting Shot".
        /// </summary>
        const string DistractingShot = "Distracting Shot";
        /// <summary>
        /// Represents the constant string "Wing Clip".
        /// </summary>
        const string WingClip = "Wing Clip";

        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Initializes a new instance of the CombatState class.
        /// </summary>
        //readonly pet;

        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 30)
        {
            player = ObjectManager.Player;
            this.target = target;
            //pet = ObjectManager.Pet;
        }

        /// <summary>
        /// Updates the player's actions based on their current state and target.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Console: WriteLine("foo")
        /// Update -> Inventory: GetEquippedItem(EquipSlot.Ranged)
        /// Inventory --> Update: gun
        /// Update -> player: Position.DistanceTo(target.Position)
        /// player --> Update: distance
        /// Update -> player: LuaCall(AutoAttackLuaScript)
        /// Update -> player: LuaCall(GunLuaScript)
        /// Update -> target: HasDebuff(SerpentSting)
        /// target --> Update: hasDebuff
        /// Update -> player: TryCastSpell(SerpentSting, 0, 34)
        /// Update -> player: TryCastSpell(ArcaneShot, 0, 34)
        /// Update -> player: TryCastSpell(RaptorStrike, 0, 5)
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (base.Update())
                return;

            Console.WriteLine("foo");

            var gun = Inventory.GetEquippedItem(EquipSlot.Ranged);
            var canUseRanged = gun != null && player.Position.DistanceTo(target.Position) > 5 && player.Position.DistanceTo(target.Position) < 34;
            if (gun == null)
            {
                player.LuaCall(AutoAttackLuaScript);
            }
            else if (canUseRanged && player.ManaPercent < 60)
            {
                player.LuaCall(GunLuaScript);
            }
            else if (gun != null && canUseRanged)
            {
                //if (!target.HasDebuff(HuntersMark)) 
                //{
                //     TryCastSpell(HuntersMark, 0, 34);
                //}
                //else 
                if (!target.HasDebuff(SerpentSting))
                {
                    TryCastSpell(SerpentSting, 0, 34);
                }
                else if (player.ManaPercent > 60)
                {
                    TryCastSpell(ArcaneShot, 0, 34);
                }
                return;


                //TryCastSpell(ConcussiveShot, 0, 34);
            }
            else
            {
                // melee rotation
                TryCastSpell(RaptorStrike, 0, 5);
            }
        }
    }
}
