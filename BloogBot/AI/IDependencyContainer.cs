using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains interfaces and classes related to the AI functionality of BloogBot.
/// </summary>
namespace BloogBot.AI
{
    /// <summary>
    /// Represents a dependency container for managing dependencies in the application.
    /// </summary>
    public interface IDependencyContainer
    {
        /// <summary>
        /// Creates a new instance of the RestState class.
        /// </summary>
        Func<Stack<IBotState>, IDependencyContainer, IBotState> CreateRestState { get; }

        /// <summary>
        /// Creates a new instance of the MoveToTargetState function.
        /// </summary>
        Func<Stack<IBotState>, IDependencyContainer, WoWUnit, IBotState> CreateMoveToTargetState { get; }

        /// <summary>
        /// Creates a combat state for power leveling, using the specified stack of bot states, dependency container, WoW unit, and WoW player.
        /// </summary>
        Func<Stack<IBotState>, IDependencyContainer, WoWUnit, WoWPlayer, IBotState> CreatePowerlevelCombatState { get; }

        /// <summary>
        /// Gets the BotSettings.
        /// </summary>
        BotSettings BotSettings { get; }

        /// <summary>
        /// Gets the Probe object.
        /// </summary>
        Probe Probe { get; }

        /// <summary>
        /// Gets the collection of hotspots.
        /// </summary>
        IEnumerable<Hotspot> Hotspots { get; }

        /// <summary>
        /// Finds the closest target for the WoWUnit.
        /// </summary>
        WoWUnit FindClosestTarget();

        /// <summary>
        /// Finds the threat for the WoWUnit.
        /// </summary>
        WoWUnit FindThreat();

        /// <summary>
        /// Gets the current hotspot.
        /// </summary>
        Hotspot GetCurrentHotspot();

        /// <summary>
        /// Checks for a travel path based on the given bot states, with the option to reverse the path and the ability to specify if the bot needs to rest.
        /// </summary>
        void CheckForTravelPath(Stack<IBotState> botStates, bool reverse, bool needsToRest = true);

        /// <summary>
        /// Gets or sets a value indicating whether the person is running errands.
        /// </summary>
        bool RunningErrands { get; set; }

        /// <summary>
        /// Updates the player trackers.
        /// </summary>
        bool UpdatePlayerTrackers();

        /// <summary>
        /// Gets or sets a value indicating whether the teleport checker is disabled.
        /// </summary>
        bool DisableTeleportChecker { get; set; }
    }
}
