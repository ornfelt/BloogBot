using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

namespace BloogBot.AI.SharedStates
{
    public class MoveToCorpseState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly StuckHelper stuckHelper;

        bool walkingOnWater;
        int stuckCount;

        bool initialized;

        bool stuckWalkAround;
        bool firstStuckWpReached;
        static readonly Random random = new Random();
        
        public MoveToCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
            stuckWalkAround = false;
            firstStuckWpReached = false;
        }

        public void Update()
        {
            if (!initialized)
            {
                container.DisableTeleportChecker = false;
                initialized = true;
            }

            if (stuckCount == 10)
            {
                //Console.WriteLine("Stuck in movetocorpsestate!");
                stuckWalkAround = true;
                DiscordClientWrapper.SendMessage($"{player.Name} is stuck in the MoveToCorpseState. Stopping.");
                // Might get stuck when moving towards safeWPs
                if (stuckHelper.CheckIfStuck())
                {
            var ran = random.Next(0, 4);
                    if (ran == 0)
                    {
                        player.StartMovement(ControlBits.Front);
                        player.StartMovement(ControlBits.StrafeLeft);
                        player.Jump();
                    }
                    if (ran == 1)
                    {
                        player.StartMovement(ControlBits.Front);
                        player.StartMovement(ControlBits.StrafeRight);
                        player.Jump();
                    }
                    if (ran == 2)
                    {
                        player.StartMovement(ControlBits.Back);
                        player.StartMovement(ControlBits.StrafeLeft);
                        player.Jump();
                    }
                    if (ran == 3)
                    {
                        player.StartMovement(ControlBits.Back);
                        player.StartMovement(ControlBits.StrafeRight);
                        player.Jump();
                    }
                }
                var safeWaypointOne = new Position(2015.57F, 1608.67F, 72.11F); // Deathknell
                var safeWaypointTwo = new Position(2198.62F, 1190.28F, 31.17F); // Deathknell-Brill
                if (!firstStuckWpReached)
                {
                    if (player.Position.DistanceTo2D(safeWaypointOne) < 3)
                    {
                        firstStuckWpReached = true;
                        player.MoveToward(safeWaypointTwo);
                    }
                    else
                        player.MoveToward(safeWaypointOne);
                    return;
                }
                else
                {
                    if (player.Position.DistanceTo2D(safeWaypointTwo) < 3)
                    {
                        stuckWalkAround = false;
                        stuckCount = 0;
                    }
                    else
                        player.MoveToward(safeWaypointTwo);
                }

                //while (botStates.Count > 0)
                //    botStates.Pop();

                if (!stuckWalkAround)
                    return;
            }

            if (stuckHelper.CheckIfStuck())
                stuckCount++;

            if (player.Position.DistanceTo2D(player.CorpsePosition) < 3)
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, player.CorpsePosition, false);

            if (player.Position.Z - nextWaypoint.Z > 5)
                walkingOnWater = true;

            if (walkingOnWater)
            {
                if (!player.IsMoving)
                    player.StartMovement(ControlBits.Front);

                if (player.Position.Z - nextWaypoint.Z < .05)
                {
                    walkingOnWater = false;
                    player.StopMovement(ControlBits.Front);
                }
            }

            else
                if (!stuckWalkAround)
                    player.MoveToward(nextWaypoint);
        }
    }
}
