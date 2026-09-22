// Ported from BloogBot/BloogBot/AI/SharedStates/MoveToPositionState.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

namespace BloogBot.AI.SharedStates
{
    public class MoveToPositionState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly Position destination;
        readonly bool use2DPop;
        readonly bool ignoreThreats;
        readonly LocalPlayer player;
        readonly StuckHelper stuckHelper;
        readonly int deadline;
        readonly Action onDeadline;

        int stuckCount;

        public MoveToPositionState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            Position destination,
            bool use2DPop = false,
            bool ignoreThreats = false,
            int deadline = -1,
            Action onDeadline = null)
        {
            this.botStates = botStates;
            this.container = container;
            this.destination = destination;
            this.use2DPop = use2DPop;
            this.ignoreThreats = ignoreThreats;
            this.deadline = deadline;
            this.onDeadline = onDeadline;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        public void Update()
        {
            var threat = container.FindThreat();

            if (threat != null && !ignoreThreats)
            {
                player.StopAllMovement();
                botStates.Push(container.CreateMoveToTargetState(botStates, container, threat));
                return;
            }

            if (stuckHelper.CheckIfStuck())
                stuckCount++;

            if (use2DPop)
            {
#if USE_CUSTOM_CHANGES
                if (player.Position.DistanceTo2D(destination) < 5 || stuckCount > 15)
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
                else if (player.InGhostForm && stuckCount > 3)
                {
#else
                if (player.Position.DistanceTo2D(destination) < 3 || stuckCount > 20)
                {
#endif
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
            }
            else
            {
#if USE_CUSTOM_CHANGES
                if (player.Position.DistanceTo(destination) < 5 || stuckCount > 15)
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
                else if (player.InGhostForm && stuckCount > 3)
                {
#else
                if (player.Position.DistanceTo(destination) < 3 || stuckCount > 20)
                {
#endif
                    player.StopAllMovement();
                    botStates.Pop();
                    return;
                }
            }

            if (deadline > 0 && Environment.TickCount > deadline)
            {
                player.StopAllMovement();
                botStates.Pop();
                onDeadline?.Invoke();
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, destination, false);
            player.MoveToward(nextWaypoint);
        }
    }
}
