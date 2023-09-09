using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using System.IO;
using System.Reflection;
using BloogBot.UI;

namespace BloogBot.AI.SharedStates
{
    public class GrindState : IBotState
    {
        static readonly Random random = new Random();

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        private static int[] blacklistedWPs = {1466, 1438, 1444, 1445, 1364, 1369, 1426, 1100};

        public GrindState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            var enemyTarget = container.FindClosestTarget();

            // 4 scenarios:
            // 1: MoveToTargetState
            // 2: No CurrWP set -> pick new one
            // 3: CurrWP set and not reached
            // 4: CurrWP set and reached
            if (enemyTarget != null && Math.Abs(enemyTarget.Position.Z - player.Position.Z) < 16.0F)
            {
                player.SetTarget(enemyTarget.Guid);
                botStates.Push(container.CreateMoveToTargetState(botStates, container, enemyTarget));
            }
            else
            {
                var hotspot = container.GetCurrentHotspot();
                //var waypointCount = hotspot.Waypoints.Length;
                //Console.WriteLine("Waypoint count: " + waypointCount);
                //var waypoint = hotspot.Waypoints[random.Next(0, waypointCount)]; // Old 
                //var waypoint = zoneWaypoints.ElementAtOrDefault(random.Next() % zoneWaypoints.Count());

                // Check if no zone is set
                if (player.CurrZone == "0")
                {
                    var nearestWp = hotspot.Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault();
                    player.CurrZone = nearestWp.Zone;
                    Console.WriteLine("No zone currently set. Setting zone based on nearest WP: " + player.CurrZone);
                }
                var zoneWaypoints = hotspot.Waypoints.Where(x => x.Zone == player.CurrZone);
                var nearestWps = zoneWaypoints.OrderBy(w => player.Position.DistanceTo(w));
                var waypoint = player.CurrWpId == 0 ? nearestWps.FirstOrDefault() : zoneWaypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();
                //float currWpDist = player.CurrWp == null ? 100.0F : player.Position.DistanceTo(waypoint);

                if (player.CurrWpId == 0)
                {
                    // No current WP. Pick nearest WP
                    bool newWpFound = false;
                    waypoint = nearestWps.ElementAtOrDefault(0);
                    int wpCounter = 0;
                    while (!newWpFound)
                    {
                        wpCounter++;
                        if (!blacklistedWPs.Contains(waypoint.ID))
                            newWpFound = true;
                        else
                            waypoint = nearestWps.ElementAtOrDefault(wpCounter);

                        if (wpCounter > 100) newWpFound = true;
                    }
                    Console.WriteLine("No CurrWpId... Selecting new one");
                }
                else
                {
                    // Check if curr waypoint is reached
                    if (player.Position.DistanceTo(waypoint) < 3.0F || player.WpStuckCount > 40)
                    {
                        Console.WriteLine($"WP: {nearestWps.ElementAtOrDefault(0).ID} reached (should be same as CurrWpId: {waypoint.ID}), selecting new WP...");
                        if (player.LastWpId != waypoint.ID)
                        {
                            // Reset WP checking values
                            player.DeathsAtWp = 0;
                            player.WpStuckCount = 0;
                            player.LastWpId = waypoint.ID;
                            player.AddWpToVisitedList(waypoint.ID);
                            LogToFile(waypoint.ID + ",");
                        }

                        // Select new waypoint based on links
                        string wpLinks = waypoint.Links.Replace(":0", "");
                        if (wpLinks.EndsWith(" "))
                            wpLinks = wpLinks.Remove(wpLinks.Length - 1);
                        string[] linkSplit = wpLinks.Split(' ');
                        //foreach (string link in linkSplit)
                        //    Console.WriteLine("Found link: " + link);

                        bool newWpFound = false;
                        int linkSearchCount = 0;
                        while (!newWpFound)
                        {
                            linkSearchCount++;
                            int randLink = random.Next() % linkSplit.Length;
                            //Console.WriteLine("randLink: " + randLink);
                            var linkWp = hotspot.Waypoints.Where(x => x.ID == Int32.Parse(linkSplit[randLink])).FirstOrDefault();
                            // TODO:
                            // * Add maxlevels to each zone. If maxlevel reached,
                            // find path to new zone and traverse the links...
                            // Add function (startNode, destZone) and output link of WPs to reach new zone
                            // Maybe use .npcb wp go XXX when leveled up / died too many times at current WP

                            // Check level requirement
                            if (linkWp.MinLevel <= player.Level && !blacklistedWPs.Contains(linkWp.ID))
                            {
                                //if (player.LastWpId != linkWp.ID && !player.HasVisitedWp(linkWp.ID))
                                if (!player.HasVisitedWp(linkWp.ID))
                                {
                                    waypoint = linkWp;
                                    newWpFound = true;
                                }
                                else
                                {
                                    // This means that randLink is same as previously visited WP
                                    // Choose it if no other links are suitable
                                    if (linkSearchCount > 15)
                                    {
                                        waypoint = linkWp;
                                        newWpFound = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"CurrWP not reached yet. Distance: {player.Position.DistanceTo(waypoint)}, wpStuckCount: {player.WpStuckCount}");
                        //player.wpStuckCount++; // This is increased in StuckHelper
                    }
                }

                player.CurrWpId = waypoint.ID;

                if (player.CurrZone != waypoint.Zone)
                {
                    player.CurrZone = waypoint.Zone; // Update current zone
                    Console.WriteLine("Bot walking towards new zone!");
                }
                Console.WriteLine("Selected waypoint: " + waypoint.ToStringFull());
                botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
            }
        }

        void LogToFile(string text)
        {
            var dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(MainViewModel)).CodeBase);
            var path = new UriBuilder(dir).Path;
            var file = Path.Combine(path, "VisitedWanderNodes.txt");
            string altFileName = "C:\\local\\VisitedWanderNodes.txt";
            if (File.Exists(file))
            {
                using (var sw = File.AppendText(file))
                {
                    sw.WriteLine(text);
                }
            }
            else if (File.Exists(altFileName))
            {
                using (var sw = File.AppendText(altFileName))
                {
                    sw.WriteLine(text);
                }
            }
        }
    }
}
