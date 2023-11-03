using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functions related to the combat state of the rogue bot.
/// </summary>
namespace CombatRogueBot
{
    /// <summary>
    /// Represents a combat state for the bot.
    /// </summary>
    /// <summary>
    /// Represents a combat state for the bot.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// Represents the constant string value for "Adrenaline Rush".
        /// </summary>
        const string AdrenalineRush = "Adrenaline Rush";
        /// <summary>
        /// Represents the constant string "Blade Flurry".
        /// </summary>
        const string BladeFlurry = "Blade Flurry";
        /// <summary>
        /// Represents the constant string "Evasion".
        /// </summary>
        const string Evasion = "Evasion";
        /// <summary>
        /// Represents the constant string "Eviscerate".
        /// </summary>
        const string Eviscerate = "Eviscerate";
        /// <summary>
        /// Represents a constant string named "Gouge".
        /// </summary>
        const string Gouge = "Gouge";
        /// <summary>
        /// Represents the constant string "Blood Fury".
        /// </summary>
        const string BloodFury = "Blood Fury";
        /// <summary>
        /// Represents a constant string value for "Kick".
        /// </summary>
        const string Kick = "Kick";
        /// <summary>
        /// Represents a constant string named "Riposte".
        /// </summary>
        const string Riposte = "Riposte";
        /// <summary>
        /// Represents the constant string "Sinister Strike".
        /// </summary>
        const string SinisterStrike = "Sinister Strike";
        /// <summary>
        /// Represents the constant string "Slice and Dice".
        /// </summary>
        const string SliceAndDice = "Slice and Dice";

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
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 3)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the character's abilities and performs actions based on certain conditions.
        /// </summary>
        public new void Update()
        {
            if (base.Update())
                return;

            TryUseAbility(AdrenalineRush, 0, ObjectManager.Aggressors.Count() == 3 && player.HealthPercent > 80);

            TryUseAbilityById(BloodFury, 3, 0, target.HealthPercent > 80);

            TryUseAbility(Evasion, 0, ObjectManager.Aggressors.Count() > 1);

            TryUseAbility(BladeFlurry, 25, ObjectManager.Aggressors.Count() > 1);

            TryUseAbility(SliceAndDice, 25, !player.HasBuff(SliceAndDice) && target.HealthPercent > 70 && player.ComboPoints == 2);

            TryUseAbility(Riposte, 10, player.CanRiposte);

            TryUseAbility(Kick, 25, ReadyToInterrupt(target));

            TryUseAbility(Gouge, 45, ReadyToInterrupt(target) && !player.IsSpellReady(Kick));

            var readyToEviscerate =
                target.HealthPercent <= 15 && player.ComboPoints >= 2
                || target.HealthPercent <= 25 && player.ComboPoints >= 3
                || target.HealthPercent <= 35 && player.ComboPoints >= 4
                || player.ComboPoints == 5;
            TryUseAbility(Eviscerate, 35, readyToEviscerate);

            TryUseAbility(SinisterStrike, 45, player.ComboPoints < 5);
        }

        /// <summary>
        /// Determines if the specified target is ready to be interrupted.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>True if the target's mana is greater than 0 and it is either casting or channeling a spell; otherwise, false.</returns>
        bool ReadyToInterrupt(WoWUnit target) => target.Mana > 0 && (target.IsCasting || target.IsChanneling);
    }
}
