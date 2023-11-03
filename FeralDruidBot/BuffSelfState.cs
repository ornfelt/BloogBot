using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the FeralDruidBot.
/// </summary>
namespace FeralDruidBot
{
    /// <summary>
    /// Represents a state where the bot buffs itself.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot buffs itself.
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
        /// Updates the player's buffs by casting Mark of the Wild and Thorns if necessary.
        /// </summary>
        public void Update()
        {
            if ((player.HasBuff(MarkOfTheWild) || !player.KnowsSpell(MarkOfTheWild)) && (player.HasBuff(Thorns) || !player.KnowsSpell(Thorns)))
            {
                botStates.Pop();
                return;
            }

            TryCastSpell(MarkOfTheWild);
            TryCastSpell(Thorns);
        }

        /// <summary>
        /// Tries to cast a spell by the given name if the player does not have the specified buff, knows the spell, and the spell is ready.
        /// </summary>
        void TryCastSpell(string name)
        {
            if (!player.HasBuff(name) && player.KnowsSpell(name) && player.IsSpellReady(name))
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
