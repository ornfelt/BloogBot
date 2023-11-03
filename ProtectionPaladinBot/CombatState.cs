using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functions related to the combat state of the Protection Paladin Bot.
/// </summary>
namespace ProtectionPaladinBot
{
    /// <summary>
    /// Represents the combat state of the bot and implements the IBotState interface.
    /// </summary>
    /// <summary>
    /// Represents the combat state of the bot and implements the IBotState interface.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The constant string representing "Consecration".
        /// </summary>
        const string Consecration = "Consecration";
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
        /// Represents the constant string "Judgement of Light".
        /// </summary>
        const string JudgementOfLight = "Judgement of Light";
        /// <summary>
        /// Represents the constant string "Judgement of Wisdom".
        /// </summary>
        const string JudgementOfWisdom = "Judgement of Wisdom";
        /// <summary>
        /// Represents the constant string "Judgement of the Crusader".
        /// </summary>
        const string JudgementOfTheCrusader = "Judgement of the Crusader";
        /// <summary>
        /// Represents the constant string "Lay on Hands".
        /// </summary>
        const string LayOnHands = "Lay on Hands";
        /// <summary>
        /// Represents a constant string with the value "Purify".
        /// </summary>
        const string Purify = "Purify";
        /// <summary>
        /// The constant string representing the Retribution Aura.
        /// </summary>
        const string RetributionAura = "Retribution Aura";
        /// <summary>
        /// Represents the constant string "Righteous Fury".
        /// </summary>
        const string RighteousFury = "Righteous Fury";
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
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 4)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the bot's actions based on the current state of the player and target.
        /// </summary>
        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HolyLight))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            TryCastSpell(LayOnHands, player.Mana < player.GetManaCost(HolyLight) && player.HealthPercent < 10, castOnSelf: true);

            TryCastSpell(Purify, player.IsPoisoned || player.IsDiseased, castOnSelf: true);

            TryCastSpell(RighteousFury, !player.HasBuff(RighteousFury));

            TryCastSpell(DevotionAura, !player.HasBuff(DevotionAura) && !player.KnowsSpell(RetributionAura));

            TryCastSpell(RetributionAura, !player.HasBuff(RetributionAura) && player.KnowsSpell(RetributionAura));

            TryCastSpell(Exorcism, 0, 30, target.CreatureType == CreatureType.Undead || target.CreatureType == CreatureType.Demon);

            TryCastSpell(HammerOfJustice, 0, 10, (target.CreatureType != CreatureType.Humanoid || (target.CreatureType == CreatureType.Humanoid && target.HealthPercent < 20)));

            TryCastSpell(Consecration, ObjectManager.Aggressors.Count() > 1);

            // for judgements - in WotLK they reworked Paladins to have "Judgement of Light" and "Judgement of Wisdom" instead of "Judgement".
            // we may want different bot .dlls for each client?
            if (ClientHelper.ClientVersion == ClientVersion.WotLK)
            {
                // do we need to use JudgementOfWisdom? prot pally seems to always be at full mana.

                TryCastSpell(JudgementOfLight, 0, 10, !target.HasDebuff(JudgementOfLight) && player.Buffs.Any(b => b.Name.StartsWith("Seal of")));
            }
            else
            {
                TryCastSpell(Judgement, 0, 10, player.HasBuff(SealOfTheCrusader) || (player.HasBuff(SealOfRighteousness) && (player.ManaPercent >= 95 || target.HealthPercent <= 3)));
            }

            TryCastSpell(SealOfTheCrusader, !player.HasBuff(SealOfTheCrusader) && !target.HasDebuff(JudgementOfTheCrusader));

            TryCastSpell(SealOfRighteousness, !player.HasBuff(SealOfRighteousness) && (target.HasDebuff(JudgementOfTheCrusader) || !player.KnowsSpell(JudgementOfTheCrusader)));

            TryCastSpell(HolyShield, !player.HasBuff(HolyShield) && target.HealthPercent > 50);
        }
    }
}
