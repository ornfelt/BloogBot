using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// Represents a state in which the bot moves towards a target.
/// </summary>
namespace TestBot
{
    /// <summary>
    /// Updates the bot state to move towards the target.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;

        /// <summary>
        /// Initializes a new instance of the MoveToTargetState class.
        /// </summary>
        internal MoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
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
