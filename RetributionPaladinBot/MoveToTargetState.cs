using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the state of the bot where it moves towards the target.
/// </summary>
namespace RetributionPaladinBot
{
    /// <summary>
    /// Represents a class that handles moving to a target state in a bot.
    /// </summary>
    class MoveToTargetState : IBotState
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
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a helper class for handling stuck operations.
        /// </summary>
        readonly StuckHelper stuckHelper;

        /// <summary>
        /// Initializes a new instance of the MoveToTargetState class.
        /// </summary>
        internal MoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        /// <summary>
        /// Updates the current state of the bot. If the target is tapped by another player or if there are aggressors targeting the target and none of them have the same GUID as the target, the bot stops all movement and pops the current state from the stack. Otherwise, it checks if the bot is stuck. If the player is within 3 units of the target's position or if the player is in combat, the bot stops all movement, pops the current state from the stack, and pushes a new CombatState onto the stack. Finally, it calculates the next waypoint using the Navigation class and moves the player towards it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> target: Check TappedByOther or Aggressors
        /// target --> Update: Return status
        /// Update -> player: StopAllMovement
        /// Update -> botStates: Pop
        /// Update -> stuckHelper: CheckIfStuck
        /// Update -> player: Check Position.DistanceTo or IsInCombat
        /// player --> Update: Return status
        /// Update -> player: StopAllMovement
        /// Update -> botStates: Pop
        /// Update -> botStates: Push(new CombatState)
        /// Update -> Navigation: GetNextWaypoint
        /// Navigation --> Update: Return nextWaypoint
        /// Update -> player: MoveToward(nextWaypoint)
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (target.TappedByOther || (ObjectManager.Aggressors.Count() > 0 && !ObjectManager.Aggressors.Any(a => a.Guid == target.Guid)))
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            if (player.Position.DistanceTo(target.Position) < 3 || player.IsInCombat)
            {
                player.StopAllMovement();
                botStates.Pop();
                botStates.Push(new CombatState(botStates, container, target));
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
