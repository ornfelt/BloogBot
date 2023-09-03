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

        private static int[] blacklistedWPs = {1466, 1438};

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
                //if (player.Position.DistanceTo(nearestWps.ElementAtOrDefault(0)) < 3.0F)
                if (player.CurrWpId != 0)
                {
                    Console.WriteLine($"WP: {nearestWps.ElementAtOrDefault(0).ID} reached (should be same as CurrWpId: {player.CurrWpId}), selecting new WP...");
                    string wpLinks = nearestWps.ElementAtOrDefault(0).Links.Replace(":0", "");
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
                    if (player.LastWpId != nearestWps.ElementAtOrDefault(0).ID)
                    {
                        player.LastWpId = nearestWps.ElementAtOrDefault(0).ID;
                        player.AddWpToVisitedList(nearestWps.ElementAtOrDefault(0).ID);
                        LogToFile(player.LastWpId + ",");
                    }
                }
                else
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

                player.CurrWpId = waypoint.ID;
                player.DeathsAtWp = 0; // Reset

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
