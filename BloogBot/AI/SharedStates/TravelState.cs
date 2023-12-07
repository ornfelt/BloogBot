using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains shared states for AI travel functionality.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state for handling travel-related functionality in a bot.
    /// </summary>
    /// <summary>
    /// Represents a state for handling travel-related functionality in a bot.
    /// </summary>
    public class TravelState : IBotState
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
        /// Gets or sets the array of waypoints representing the travel path.
        /// </summary>
        readonly Position[] travelPathWaypoints;
        /// <summary>
        /// Gets or sets the callback action that is executed when the event occurs.
        /// </summary>
        readonly Action callback;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Represents the index of the travel path.
        /// </summary>
        int travelPathIndex;

        /// <summary>
        /// Initializes a new instance of the TravelState class.
        /// </summary>
        public TravelState(Stack<IBotState> botStates, IDependencyContainer container, Position[] travelPathWaypoints, int startingIndex, Action callback = null)
        {
            this.botStates = botStates;
            this.container = container;
            this.travelPathWaypoints = travelPathWaypoints;
            this.callback = callback;
            player = ObjectManager.Player;
            travelPathIndex = startingIndex;
        }

        /// <summary>
        /// Updates the behavior of the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Container: FindThreat
        /// Container --> Update: threat
        /// alt threat is not null
        ///     Update -> Player: StopAllMovement
        ///     Update -> Container: CreateMoveToTargetState(botStates, container, threat)
        ///     Container --> Update: MoveToTargetState
        ///     Update -> BotStates: Push(MoveToTargetState)
        /// else threat is null and player is close to waypoint
        ///     Update -> Player: Position.DistanceTo(travelPathWaypoints[travelPathIndex])
        ///     Player --> Update: distance
        ///     alt distance < 3
        ///         Update -> Update: travelPathIndex++
        ///     else travelPathIndex equals travelPathWaypoints.Length
        ///         Update -> Player: StopAllMovement
        ///         Update -> BotStates: Pop
        ///         Update -> Callback: Invoke
        ///     else player moves toward waypoint
        ///         Update -> Player: MoveToward(travelPathWaypoints[travelPathIndex])
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            var threat = container.FindThreat();

            if (threat != null)
            {
                player.StopAllMovement();
                botStates.Push(container.CreateMoveToTargetState(botStates, container, threat));
                return;
            }

            if (player.Position.DistanceTo(travelPathWaypoints[travelPathIndex]) < 3)
                travelPathIndex++;

            if (travelPathIndex == travelPathWaypoints.Length)
            {
                player.StopAllMovement();
                botStates.Pop();
                callback?.Invoke();
                return;
            }

            player.MoveToward(travelPathWaypoints[travelPathIndex]);
        }
    }
}
