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

                var waypointCount = hotspot.Waypoints.Length;
                //var waypoint = hotspot.Waypoints[random.Next(0, waypointCount)];

                var zoneWaypoints = hotspot.Waypoints.Where(x => x.Zone == "14");
                var waypoint = zoneWaypoints.ElementAtOrDefault(random.Next() % zoneWaypoints.Count());
                var nearestWps = zoneWaypoints.OrderBy(w => player.Position.DistanceTo(w));
                if (player.Position.DistanceTo(nearestWps.ElementAtOrDefault(0)) < 3.0F)
                {
                    //TODO: select next wp based on links!
                    // IF new zone, check minlevel. If minlevel not reached -> select link where zone is same as curr
                    // Don't choose same as last wp when selecting link...
                    waypoint = nearestWps.ElementAtOrDefault(1);
                    Console.WriteLine("WP reached, selecting second closest...");
                }
                else
                {
                    waypoint = nearestWps.ElementAtOrDefault(0);
                    Console.WriteLine("Probably no waypoint set...");
                }

                Console.WriteLine("Waypoint count: " + waypointCount);
                Console.WriteLine("Selected waypoint: " + waypoint.ToStringFull());
                botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
            }
        }
    }
}
