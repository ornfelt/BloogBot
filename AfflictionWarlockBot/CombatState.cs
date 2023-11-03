using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the classes and functions related to the Affliction Warlock Bot.
/// </summary>
namespace AfflictionWarlockBot
{
    /// <summary>
    /// Represents a combat state in the bot, implementing the IBotState interface.
    /// </summary>
    /// <summary>
    /// Represents a combat state in the bot, implementing the IBotState interface.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The Lua script for casting the "Shoot" spell if the action with ID 11 is not set to auto-repeat.
        /// </summary>
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";
        /// <summary>
        /// Lua script to turn off the wand if the action is set to auto-repeat.
        /// </summary>
        const string TurnOffWandLuaScript = "if IsAutoRepeatAction(11) ~= nil then CastSpellByName('Shoot') end";

        /// <summary>
        /// Represents a constant string named "Corruption".
        /// </summary>
        const string Corruption = "Corruption";
        /// <summary>
        /// Represents the constant string "Curse of Agony".
        /// </summary>
        const string CurseOfAgony = "Curse of Agony";
        /// <summary>
        /// Represents the constant string "Death Coil".
        /// </summary>
        const string DeathCoil = "Death Coil";
        /// <summary>
        /// Represents the constant string "Drain Soul".
        /// </summary>
        const string DrainSoul = "Drain Soul";
        /// <summary>
        /// Represents the constant string "Immolate".
        /// </summary>
        const string Immolate = "Immolate";
        /// <summary>
        /// Represents the constant string "Life Tap".
        /// </summary>
        const string LifeTap = "Life Tap";
        /// <summary>
        /// Represents the constant string value "Shadow Bolt".
        /// </summary>
        const string ShadowBolt = "Shadow Bolt";
        /// <summary>
        /// Represents the constant string "Siphon Life".
        /// </summary>
        const string SiphonLife = "Siphon Life";

        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;

        /// <summary>
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 30)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the behavior of the pet.
        /// If the target's health is low, turns off the wand and casts Drain Soul.
        /// If the player has a wand equipped and their mana is low, or the target's health is between 20% and 60%, and the player is not channeling or casting, uses the wand.
        /// Otherwise, tries to cast various spells based on different conditions.
        /// </summary>
        public new void Update()
        {
            if (base.Update())
                return;

            ObjectManager.Pet?.Attack();

            // if target is low on health, turn off wand and cast drain soul
            if (target.HealthPercent <= 20)
            {
                player.LuaCall(TurnOffWandLuaScript);
                TryCastSpell(DrainSoul, 0, 29);
            }

            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (wand != null && (player.ManaPercent <= 10 || (target.HealthPercent <= 60 && target.HealthPercent > 20) && !player.IsChanneling && !player.IsCasting))
                player.LuaCall(WandLuaScript);
            else
            {
                TryCastSpell(DeathCoil, 0, 30, (target.IsCasting || target.IsChanneling) && target.HealthPercent > 20);

                TryCastSpell(LifeTap, 0, int.MaxValue, player.HealthPercent > 85 && player.ManaPercent < 80);

                TryCastSpell(CurseOfAgony, 0, 30, !target.HasDebuff(CurseOfAgony) && target.HealthPercent > 90);

                TryCastSpell(Immolate, 0, 30, !target.HasDebuff(Immolate) && target.HealthPercent > 30);

                TryCastSpell(Corruption, 0, 30, !target.HasDebuff(Corruption) && target.HealthPercent > 30);

                TryCastSpell(SiphonLife, 0, 30, !target.HasDebuff(SiphonLife) && target.HealthPercent > 50);

                TryCastSpell(ShadowBolt, 0, 30, target.HealthPercent > 40 || wand == null);
            }
        }
    }
}
