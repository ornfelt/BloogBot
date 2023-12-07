using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains shared states for AI bots, including the MoveToPositionState class which handles moving the bot to a specified position.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state where the bot moves to a specific position.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot moves to a specific position.
    /// </summary>
    public class MoveToPositionState : IBotState
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
        /// Gets or sets a value indicating whether to use 2D pop.
        /// </summary>
        readonly bool use2DPop;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a helper class for handling stuck operations.
        /// </summary>
        readonly StuckHelper stuckHelper;

        /// <summary>
        /// Represents the count of times the program got stuck.
        /// </summary>
        int stuckCount;

        /// <summary>
        /// Initializes a new instance of the MoveToPositionState class.
        /// </summary>
        public MoveToPositionState(Stack<IBotState> botStates, IDependencyContainer container, Position destination, bool use2DPop = false)
        {
            this.botStates = botStates;
            this.container = container;
            this.destination = destination;
            this.use2DPop = use2DPop;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
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
        /// else threat is null
        ///     Update -> StuckHelper: CheckIfStuck
        ///     StuckHelper --> Update: stuckStatus
        ///     alt stuckStatus is true
        ///         Update -> Update: Increment stuckCount
        ///     end
        ///     alt use2DPop is true
        ///         Update -> Player: DistanceTo2D(destination)
        ///         Player --> Update: distance
        ///         alt distance < 5 or stuckCount > 15
        ///             Update -> Player: StopAllMovement
        ///             Update -> BotStates: Pop
        ///         else distance >= 5 and stuckCount <= 15
        ///             alt player.InGhostForm and stuckCount > 3
        ///                 Update -> Player: StopAllMovement
        ///                 Update -> BotStates: Pop
        ///             end
        ///         end
        ///     else use2DPop is false
        ///         Update -> Player: DistanceTo(destination)
        ///         Player --> Update: distance
        ///         alt distance < 5 or stuckCount > 15
        ///             Update -> Player: StopAllMovement
        ///             Update -> BotStates: Pop
        ///         else distance >= 5 and stuckCount <= 15
        ///             alt player.InGhostForm and stuckCount > 3
        ///                 Update -> Player: StopAllMovement
        ///                 Update -> BotStates: Pop
        ///             end
        ///         end
        ///     end
        ///     Update -> Navigation: GetNextWaypoint(ObjectManager.MapId, player.Position, destination, false)
        ///     Navigation --> Update: nextWaypoint
        ///     Update -> Player: MoveToward(nextWaypoint)
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

            if (stuckHelper.CheckIfStuck())
                stuckCount++;

            if (use2DPop)
            {
                if (player.Position.DistanceTo2D(destination) < 5 || stuckCount > 15)
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
                else if (player.InGhostForm && stuckCount > 3)
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
            }
            else
            {
                if (player.Position.DistanceTo(destination) < 5 || stuckCount > 15)
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
                else if (player.InGhostForm && stuckCount > 3)
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, destination, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
