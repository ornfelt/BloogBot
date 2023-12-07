using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the classes and functions related to the Retribution Paladin Bot.
/// </summary>
namespace RetributionPaladinBot
{
    /// <summary>
    /// This class defines the combat state for a bot in a game.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// Represents the constant string value for the "Devotion Aura".
        /// </summary>
        const string DevotionAura = "Devotion Aura";
        /// <summary>
        /// Represents the constant string "Exorcism".
        /// </summary>
        const string Exorcism = "Exorcism";
        /// <summary>
        /// Represents the constant string "Hammer of Justice".
        /// </summary>
        const string HammerOfJustice = "Hammer of Justice";
        /// <summary>
        /// Represents the constant string "Holy Light".
        /// </summary>
        const string HolyLight = "Holy Light";
        /// <summary>
        /// Represents the constant string "Holy Shield".
        /// </summary>
        const string HolyShield = "Holy Shield";
        /// <summary>
        /// Represents a constant string value for "Judgement".
        /// </summary>
        const string Judgement = "Judgement";
        /// <summary>
        /// Represents the constant string "Judgement of the Crusader".
        /// </summary>
        const string JudgementOfTheCrusader = "Judgement of the Crusader";
        /// <summary>
        /// Represents a constant string with the value "Purify".
        /// </summary>
        const string Purify = "Purify";
        /// <summary>
        /// The constant string representing the Retribution Aura.
        /// </summary>
        const string RetributionAura = "Retribution Aura";
        /// <summary>
        /// Represents the constant string "Sanctity Aura".
        /// </summary>
        const string SanctityAura = "Sanctity Aura";
        /// <summary>
        /// The constant string representing the "Seal of Command".
        /// </summary>
        const string SealOfCommand = "Seal of Command";
        /// <summary>
        /// The constant string representing the "Seal of Righteousness".
        /// </summary>
        const string SealOfRighteousness = "Seal of Righteousness";
        /// <summary>
        /// The constant string representing the "Seal of the Crusader".
        /// </summary>
        const string SealOfTheCrusader = "Seal of the Crusader";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
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
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the behavior of the player character.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Update -> "player.HealthPercent < 30\n&& target.HealthPercent > 50\n&& player.Mana >= player.GetManaCost(HolyLight)": Check condition
        /// "player.HealthPercent < 30\n&& target.HealthPercent > 50\n&& player.Mana >= player.GetManaCost(HolyLight)" --> Update: If true
        /// Update -> HealSelfState: Push new state
        /// Update -> base.Update: Call base update
        /// base.Update --> Update: If true
        /// Update -> TryCastSpell: Try to cast Purify
        /// Update -> TryCastSpell: Try to cast DevotionAura
        /// Update -> TryCastSpell: Try to cast RetributionAura
        /// Update -> TryCastSpell: Try to cast SanctityAura
        /// Update -> TryCastSpell: Try to cast Exorcism
        /// Update -> TryCastSpell: Try to cast HammerOfJustice
        /// Update -> TryCastSpell: Try to cast SealOfTheCrusader
        /// Update -> TryCastSpell: Try to cast SealOfRighteousness
        /// Update -> TryCastSpell: Try to cast SealOfCommand
        /// Update -> TryCastSpell: Try to cast HolyShield
        /// Update -> TryCastSpell: Try to cast Judgement
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HolyLight))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            TryCastSpell(Purify, player.IsPoisoned || player.IsDiseased, castOnSelf: true);

            TryCastSpell(DevotionAura, !player.HasBuff(DevotionAura) && !player.KnowsSpell(RetributionAura) && !player.KnowsSpell(SanctityAura));

            TryCastSpell(RetributionAura, !player.HasBuff(RetributionAura) && !player.KnowsSpell(SanctityAura));

            TryCastSpell(SanctityAura, !player.HasBuff(SanctityAura));

            TryCastSpell(Exorcism, target.CreatureType == CreatureType.Undead || target.CreatureType == CreatureType.Demon);

            TryCastSpell(HammerOfJustice, (target.CreatureType != CreatureType.Humanoid || (target.CreatureType == CreatureType.Humanoid && target.HealthPercent < 20)));

            TryCastSpell(SealOfTheCrusader, !player.HasBuff(SealOfTheCrusader) && !target.HasDebuff(JudgementOfTheCrusader));

            TryCastSpell(SealOfRighteousness, !player.HasBuff(SealOfRighteousness) && target.HasDebuff(JudgementOfTheCrusader) && !player.KnowsSpell(SealOfCommand));

            TryCastSpell(SealOfCommand, !player.HasBuff(SealOfCommand) && target.HasDebuff(JudgementOfTheCrusader));

            TryCastSpell(HolyShield, !player.HasBuff(HolyShield) && target.HealthPercent > 50);

            TryCastSpell(Judgement, player.HasBuff(SealOfTheCrusader) || ((player.HasBuff(SealOfRighteousness) || player.HasBuff(SealOfCommand)) && (player.ManaPercent >= 95 || target.HealthPercent <= 3)));
        }
    }
}
