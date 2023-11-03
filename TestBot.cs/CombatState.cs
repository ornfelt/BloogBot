using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// Represents a state in which the bot is engaged in combat.
/// </summary>
namespace TestBot
{
    /// <summary>
    /// This class represents the combat state of a bot in a game.
    /// </summary>
    class CombatState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;

        /// <summary>
        /// Initializes a new instance of the CombatState class.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
        }

        /// <summary>
        /// Updates the bot state by removing the topmost state from the stack.
        /// </summary>
        public void Update()
        {
            botStates.Pop();
        }
    }
}
