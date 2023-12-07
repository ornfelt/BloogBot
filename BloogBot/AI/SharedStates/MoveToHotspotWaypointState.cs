using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a state in which the bot moves towards a designated hotspot waypoint.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot moves towards a hotspot waypoint.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot moves towards a hotspot waypoint.
    /// </summary>
    public class MoveToHotspotWaypointState : IBotState
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
        /// Gets the destination position.
        /// </summary>
        readonly Position destination;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a helper class for handling stuck operations.
        /// </summary>
        readonly StuckHelper stuckHelper;

        /// <summary>
        /// Initializes a new instance of the MoveToHotspotWaypointState class.
        /// </summary>
        public MoveToHotspotWaypointState(Stack<IBotState> botStates, IDependencyContainer container, Position destination)
        {
            this.botStates = botStates;
            this.container = container;
            this.destination = destination;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        /// <summary>
        /// Updates the player's movement and checks if the player is stuck or has reached the destination.
        /// If the player is stuck, stops all movement and pops the current bot state.
        /// If the player has reached the destination or is stuck for too long, stops all movement and pops the current bot state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: IsCasting
        /// alt Player is casting
        ///     Update -> Update: return
        /// else Player is not casting
        ///     Update -> StuckHelper: CheckIfStuck
        ///     Update -> Container: FindClosestTarget
        ///     alt Closest target found and within range or Player close to destination or Player stuck count > 10
        ///         Update -> Player: StopAllMovement
        ///         Update -> BotStates: Pop
        ///         Update -> Update: return
        ///     else Continue movement
        ///         Update -> Navigation: GetNextWaypoint
        ///         Update -> Player: MoveToward(nextWaypoint)
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.IsCasting)
                return;
            stuckHelper.CheckIfStuck();

            if ((container.FindClosestTarget() != null &&
                Math.Abs(container.FindClosestTarget().Position.Z - player.Position.Z) < 16.0F)
                || player.Position.DistanceTo(destination) < 3
                || player.WpStuckCount > 10)
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, destination, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
