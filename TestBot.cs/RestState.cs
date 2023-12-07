using BloogBot.AI;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the TestBot application.
/// </summary>
namespace TestBot
{
    /// <summary>
    /// Represents a state in which the bot is at rest.
    /// </summary>
    class RestState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;

        /// <summary>
        /// Initializes a new instance of the RestState class.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
        }

        /// <summary>
        /// Updates the bot state by removing the topmost state from the stack.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> Update : Call Update()
        /// Update -> botStates : Pop()
        /// \enduml
        /// </remarks>
        public void Update()
        {
            botStates.Pop();
        }
    }
}
