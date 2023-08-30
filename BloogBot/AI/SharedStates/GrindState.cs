using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BloogBot.AI.SharedStates
{
    public class GrindState : IBotState
    {
        static readonly Random random = new Random();

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        public GrindState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            var enemyTarget = container.FindClosestTarget();

            if (enemyTarget != null)
            {
                player.SetTarget(enemyTarget.Guid);
                botStates.Push(container.CreateMoveToTargetState(botStates, container, enemyTarget));
            }
            else
            {
                var hotspot = container.GetCurrentHotspot();

                if (hotspot == null)
                {
                    var nearestHotspot = container
                        .Hotspots
                        .Where(h => h != null)
                        //.SelectMany(h => h.Waypoints)
                        //.OrderBy(w => player.Position.DistanceTo(w))
                        .FirstOrDefault();
                    if (nearestHotspot != null)
                        hotspot = nearestHotspot;
                    else
                    {
                        Console.WriteLine("No hotspot nearby!");
                        return;
                    }
                }

                var waypointCount = hotspot.Waypoints.Length;
                var waypoint = hotspot.Waypoints[random.Next(0, waypointCount)];
                botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
            }
        }
    }
}
