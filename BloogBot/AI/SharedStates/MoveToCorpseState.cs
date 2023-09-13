using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

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
        static readonly Random random = new Random();
        
        public MoveToCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
            stuckWalkAround = false;
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
                if (!stuckWalkAround)
                {
                    Console.WriteLine("Stuck in MoveToCorpseState. Trying to find a walkaround...");
                    stuckWalkAround = true;
                }
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

                var hotspot = container.GetCurrentHotspot();
                // TODO? if stuck for a long time, get nearest WP and trace back to CurrWp and then go to corpse
                var nearestWp = container
                    .Hotspots
                    .Where(h => h != null)
                    .SelectMany(h => h.Waypoints)
                    .OrderBy(w => player.Position.DistanceTo(w))
                    .FirstOrDefault();
                    player.MoveToward(nearestWp);

                    if (player.Position.DistanceTo2D(nearestWp) < 5)
                    {
                        stuckWalkAround = false;
                        stuckCount = 0;
                    }

                //while (botStates.Count > 0)
                //    botStates.Pop();

                if (stuckWalkAround)
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
                player.MoveToward(nextWaypoint);
        }
    }
}
