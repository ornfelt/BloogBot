using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;

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
                //var waypoint = hotspot.Waypoints[random.Next(0, waypointCount)]; // Old 

                // Check if no zone is set
                if (player.CurrZone == "0")
                {
                    var nearestWp = hotspot.Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault();
                    player.CurrZone = nearestWp.Zone;
                    Console.WriteLine("No zone currently set. Setting zone based on nearest WP: " + player.CurrZone);
                }
                var zoneWaypoints = hotspot.Waypoints.Where(x => x.Zone == player.CurrZone);
                var waypoint = zoneWaypoints.ElementAtOrDefault(random.Next() % zoneWaypoints.Count());
                var nearestWps = zoneWaypoints.OrderBy(w => player.Position.DistanceTo(w));
                // Check if curr waypoint is reached
                if (player.Position.DistanceTo(nearestWps.ElementAtOrDefault(0)) < 3.0F)
                {
                    Console.WriteLine($"WP: {nearestWps.ElementAtOrDefault(0).ID} reached, selecting new WP...");
                    string wpLinks = nearestWps.ElementAtOrDefault(0).Links.Replace(":0", "");
                    if (wpLinks.EndsWith(" "))
                        wpLinks = wpLinks.Remove(wpLinks.Length - 1);
                    string[] linkSplit = wpLinks.Split(' ');
                    foreach (string link in linkSplit)
                        Console.WriteLine("Found link: " + link);

                    bool newWpFound = false;
                    int linkSearchCount = 0;
                    while (!newWpFound)
                    {
                        linkSearchCount++;
                        int randLink = random.Next() % linkSplit.Length;
                        Console.WriteLine("randLink: " + randLink);
                        var linkWp = hotspot.Waypoints.Where(x => x.ID == Int32.Parse(linkSplit[randLink])).FirstOrDefault();
                        // No need to check new zone...
                        if (linkWp.MinLevel <= player.Level)
                        {
                            if (player.LastWpId != linkWp.ID)
                            {
                                waypoint = linkWp;
                                newWpFound = true;
                            }
                            else
                            {
                                // This means that randLink is same as last visited WP
                                // Choose it if no other links are suitable
                                if (linkSearchCount > 15)
                                {
                                    waypoint = linkWp;
                                    newWpFound = true;
                                }
                            }
                        }
                    }
                    player.LastWpId = nearestWps.ElementAtOrDefault(0).ID;
                }
                else
                {
                    waypoint = nearestWps.ElementAtOrDefault(0);
                    Console.WriteLine("No current waypoint set...");
                }

                if (player.CurrZone != waypoint.Zone)
                {
                    player.CurrZone = waypoint.Zone; // Update current zone
                    Console.WriteLine("Bot walking towards new zone!");
                }
                Console.WriteLine("Waypoint count: " + waypointCount);
                Console.WriteLine("Selected waypoint: " + waypoint.ToStringFull());
                botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
            }
        }
    }
}
