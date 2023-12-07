using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes and methods related to handling stuck behavior in the AI system.
/// </summary>
namespace BloogBot.AI
{
    /// <summary>
    /// Represents a helper class for handling stuck situations.
    /// </summary>
    /// <summary>
    /// Represents a helper class for handling stuck situations.
    /// </summary>
    public class StuckHelper
    {
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
        /// Represents the last position.
        /// </summary>
        Position lastPosition;
        /// <summary>
        /// Represents the last recorded tick time.
        /// </summary>
        int lastTickTime;
        /// <summary>
        /// Represents the duration of being stuck.
        /// </summary>
        int stuckDuration;

        /// <summary>
        /// Initializes a new instance of the StuckHelper class.
        /// </summary>
        public StuckHelper(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Checks if the player is stuck by comparing the distance between the current position and the last position.
        /// If the player has been stuck for more than 1000 milliseconds, increments the WpStuckCount and pushes a new StuckState to the botStates stack.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CheckIfStuck -> player: Get last position
        /// player -> player.Position: Get position
        /// CheckIfStuck -> lastPosition: Check if last position is not null
        /// lastPosition --> CheckIfStuck: Last position is not null
        /// CheckIfStuck -> player.Position: Calculate distance to last position
        /// player.Position --> CheckIfStuck: Distance calculated
        /// CheckIfStuck -> stuckDuration: Check if distance is less than or equal to 0.05
        /// stuckDuration --> CheckIfStuck: Distance is less than or equal to 0.05
        /// CheckIfStuck -> Environment: Get current tick count
        /// Environment --> CheckIfStuck: Current tick count
        /// CheckIfStuck -> lastTickTime: Get last tick time
        /// lastTickTime --> CheckIfStuck: Last tick time
        /// CheckIfStuck -> stuckDuration: Add tick count difference to stuck duration
        /// stuckDuration --> CheckIfStuck: Stuck duration updated
        /// CheckIfStuck -> stuckDuration: Check if stuck duration is greater than or equal to 1000
        /// stuckDuration --> CheckIfStuck: Stuck duration is greater than or equal to 1000
        /// CheckIfStuck -> player: Increment WpStuckCount
        /// player --> CheckIfStuck: WpStuckCount incremented
        /// CheckIfStuck -> Console: Print WpStuckCount
        /// Console --> CheckIfStuck: WpStuckCount printed
        /// CheckIfStuck -> stuckDuration: Reset stuck duration
        /// stuckDuration --> CheckIfStuck: Stuck duration reset
        /// CheckIfStuck -> lastPosition: Reset last position
        /// lastPosition --> CheckIfStuck: Last position reset
        /// CheckIfStuck -> botStates: Push new StuckState
        /// botStates --> CheckIfStuck: StuckState pushed
        /// CheckIfStuck -> return: Return true
        /// \enduml
        /// </remarks>
        public bool CheckIfStuck()
        {
            if (lastPosition != null && player.Position.DistanceTo(lastPosition) <= 0.05)
                stuckDuration += Environment.TickCount - lastTickTime;
            if (stuckDuration >= 1000)
            {
                player.WpStuckCount++;
                Console.WriteLine($"WpStuckCount: {player.WpStuckCount}");
                stuckDuration = 0;
                lastPosition = null;
                botStates.Push(new StuckState(botStates, container));
                return true;
            }

            lastPosition = player.Position;
            lastTickTime = Environment.TickCount;

            return false;
        }
    }
}
