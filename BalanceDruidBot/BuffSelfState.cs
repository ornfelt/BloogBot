using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Xml.Linq;

/// <summary>
/// This namespace contains classes related to handling the self-buffing behavior of a balance druid bot.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// Represents a state where the bot buffs itself with various abilities.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot buffs itself with various abilities.
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Mark of the Wild".
        /// </summary>
        const string MarkOfTheWild = "Mark of the Wild";
        /// <summary>
        /// Represents a constant string with the value "Thorns".
        /// </summary>
        const string Thorns = "Thorns";
        /// <summary>
        /// Represents the constant string "Omen of Clarity".
        /// </summary>
        const string OmenOfClarity = "Omen of Clarity";
        /// <summary>
        /// Represents the constant string "Moonkin Form".
        /// </summary>
        const string MoonkinForm = "Moonkin Form";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the BuffSelfState class.
        /// </summary>
        public BuffSelfState(Stack<IBotState> botStates)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's buffs and casts spells if necessary.
        /// </summary>
        public void Update()
        {
            if ((player.HasBuff(MarkOfTheWild) || !player.KnowsSpell(MarkOfTheWild)) &&
                (player.HasBuff(Thorns) || !player.KnowsSpell(Thorns)) &&
                (player.HasBuff(OmenOfClarity) || !player.KnowsSpell(OmenOfClarity)))
            {
                botStates.Pop();
                return;
            }

            if (!player.HasBuff(MarkOfTheWild))
            {
                if (player.HasBuff(MoonkinForm))
                {
                    player.LuaCall($"CastSpellByName('{MoonkinForm}')");
                }

                TryCastSpell(MarkOfTheWild);
            }

            TryCastSpell(Thorns);
            TryCastSpell(OmenOfClarity);
        }

        /// <summary>
        /// Tries to cast a spell by the given name if the player does not have the specified buff and the spell is ready.
        /// </summary>
        void TryCastSpell(string name)
        {
            if (!player.HasBuff(name) && player.IsSpellReady(name))
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
