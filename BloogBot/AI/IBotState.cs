/// <summary>
/// Represents the interface for a bot state.
/// </summary>
namespace BloogBot.AI
{
    /// <summary>
    /// Represents the state of a bot.
    /// </summary>
    public interface IBotState
    {
        /// <summary>
        /// Updates the data.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> Update: Call Update method
        /// \enduml
        /// </remarks>
        void Update();
    }
}
