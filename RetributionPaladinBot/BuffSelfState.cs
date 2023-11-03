using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Retribution Paladin Bot.
/// </summary>
namespace RetributionPaladinBot
{
    /// <summary>
    /// Represents a state where the bot buffs itself with various blessings.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot buffs itself with various blessings.
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Blessing of Kings".
        /// </summary>
        const string BlessingOfKings = "Blessing of Kings";
        /// <summary>
        /// Represents the constant string "Blessing of Might".
        /// </summary>
        const string BlessingOfMight = "Blessing of Might";
        /// <summary>
        /// Represents the constant string "Blessing of Sanctuary".
        /// </summary>
        const string BlessingOfSanctuary = "Blessing of Sanctuary";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuffSelfState"/> class.
        /// </summary>
        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's buffs by casting appropriate spells.
        /// </summary>
        public void Update()
        {
            if (!player.KnowsSpell(BlessingOfMight) || player.HasBuff(BlessingOfMight) || player.HasBuff(BlessingOfKings) || player.HasBuff(BlessingOfSanctuary))
            {
                botStates.Pop();
                return;
            }

            if (player.KnowsSpell(BlessingOfMight) && !player.KnowsSpell(BlessingOfKings) && !player.KnowsSpell(BlessingOfSanctuary))
                TryCastSpell(BlessingOfMight);

            if (player.KnowsSpell(BlessingOfKings) && !player.KnowsSpell(BlessingOfSanctuary))
                TryCastSpell(BlessingOfKings);

            if (player.KnowsSpell(BlessingOfSanctuary))
                TryCastSpell(BlessingOfSanctuary);
        }

        /// <summary>
        /// Tries to cast a spell by the given name if the player does not have the specified buff, the spell is ready, and the player has enough mana.
        /// </summary>
        void TryCastSpell(string name)
        {
            if (!player.HasBuff(name) && player.IsSpellReady(name) && player.Mana > player.GetManaCost(name))
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
