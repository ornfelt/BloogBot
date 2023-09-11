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

        private static int[] blacklistedWPs = {1466, 1438, 1444, 1445, 1364, 1369, 1426, 1100, 993};

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
                var waypointCount = hotspot.Waypoints.Length;
                Console.WriteLine("Waypoint count: " + waypointCount);
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
                    // Log datetime to file to separate new bot sessions
                    LogToFile(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                }
                else
                {
                    // Check if curr waypoint is reached
                    if (player.Position.DistanceTo(waypoint) < 3.0F || player.WpStuckCount > 30)
                    {
                        if (player.WpStuckCount > 30)
                            Console.WriteLine($"WP: {nearestWps.ElementAtOrDefault(0).ID} COULDN'T be reached (should be same WP as Current Waypoint ID: {waypoint.ID}), selecting new WP...");
                        else
                            Console.WriteLine($"WP: {nearestWps.ElementAtOrDefault(0).ID} reached (should be same WP as Current Waypoint ID: {waypoint.ID}), selecting new WP...");

                        // Check if player is higher level than waypoint maxlevel
                        player.HasOverLeveled = player.Level >= waypoint.MaxLevel;
                        if (player.HasOverLeveled)
                        {
                            if (player.ForcedWpPath.Count == 0)
                            {
                                // Use a forced path to a new zone
                                player.ForcedWpPath = ForcedWpPathViaBFS(waypoint.ID);
                                foreach (var wpInPath in player.ForcedWpPath)
                                    Console.Write(wpInPath != player.ForcedWpPath[player.ForcedWpPath.Count-1] ? wpInPath + " -> " : wpInPath + "\n");
                            }
                            // Remove first value since it's the same as the currently reached WP
                            if (player.ForcedWpPath.First() == waypoint.ID)
                                player.ForcedWpPath.Remove(player.ForcedWpPath.First());

                            // Set new WP based on forced path
                            waypoint = hotspot.Waypoints.Where(x => x.ID == player.ForcedWpPath.First()).FirstOrDefault();
                            player.ForcedWpPath.Remove(player.ForcedWpPath.First());
                        }
                        else
                        {
                            if (player.LastWpId != waypoint.ID)
                            {
                                // Reset WP checking values
                                player.DeathsAtWp = 0;
                                player.WpStuckCount = 0;
                                player.LastWpId = waypoint.ID;
                                // Add to visited WPs and log to file
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

                                // Check level requirement
                                if (linkWp.MinLevel <= player.Level && !blacklistedWPs.Contains(linkWp.ID))
                                {
                                    //if (player.LastWpId != linkWp.ID && !player.HasVisitedWp(linkWp.ID))
                                    if (!player.HasVisitedWp(linkWp.ID))
                                    {
                                        waypoint = linkWp;
                                        newWpFound = true;
                                    }
                                    else if (linkSearchCount > 15)
                                    {
                                        // This means that randLink is same as previously visited WP
                                        // Choose it if no other links are suitable
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
                Console.WriteLine("Selected waypoint: " + waypoint.ToStringFull() + ", HasOverleveled: " + player.HasOverLeveled);
                botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
            }
        }

        public List<int> ForcedWpPathViaBFS(int startId)
        {
            var hotspot = container.GetCurrentHotspot();
            var visited = new HashSet<int>();
            var queue = new Queue<List<int>>();
            queue.Enqueue(new List<int> { startId });
            List<int> currentPath = null;

            while (queue.Count > 0)
            {
                currentPath = queue.Dequeue();
                var currentId = currentPath[currentPath.Count - 1]; // Get the last element

                var currentWaypoint = hotspot.Waypoints.Where(x => x.ID == currentId).FirstOrDefault();
                if (currentWaypoint.MaxLevel > player.Level && currentWaypoint.MinLevel <= player.Level)
                {
                    Console.WriteLine("Found new WP matching player level: " + currentWaypoint.ToStringFull() + "\n");
                    return currentPath;
                }
                // Player could be above all WP maxlevels,so make an exception
                // for those players so that they can move through the zones.
                // Hotspot 1-4 are Azeroth WPs, 5 is Outland, 6 is Northrend
                else if ((hotspot.Id < 5 && player.Level >= 60) || (hotspot.Id == 5 && player.Level >= 70) 
                    || (hotspot.Id == 6 && player.Level >= 80))
                {
                    // Try to visit new zones
                    bool currWpIsNewZone = true;
                    foreach(int visitedWpId in player.VisitedWps)
                    {
                        var visitedWp = hotspot.Waypoints.Where(x => x.ID == visitedWpId).FirstOrDefault();
                        if (visitedWp.Zone == currentWaypoint.Zone)
                            currWpIsNewZone = false;
                    }

                    if (currWpIsNewZone)
                    {
                        Console.WriteLine("Found new WP matching player level: " + currentWaypoint.ToStringFull() + "\n");
                        return currentPath;
                    }
                }

                if (!visited.Contains(currentId))
                {
                    visited.Add(currentId);

                    string wpLinks = currentWaypoint.Links.Replace(":0", "");
                    if (wpLinks.EndsWith(" "))
                        wpLinks = wpLinks.Remove(wpLinks.Length - 1);
                    string[] linkSplit = wpLinks.Split(' ');

                    foreach (var linkWp in linkSplit)
                    {
                        int linkId = Int32.Parse(linkWp);
                        if (!visited.Contains(linkId))
                        {
                            var newPath = new List<int>(currentPath);
                            newPath.Add(linkId);
                            queue.Enqueue(newPath);
                        }
                    }
                }
            }
            return currentPath; // Return last currentPath set or null
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
