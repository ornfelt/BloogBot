using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
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
        LocalPlayer player;

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
                HandleWpSelection();
            }
        }

        private void HandleWpSelection()
        {
            var hotspot = container.GetCurrentHotspot();
            player = ObjectManager.Player;
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
            //var zoneWaypoints = hotspot.Waypoints.Where(x => x.Zone == player.CurrZone);
            var waypoints = hotspot.Waypoints;
            var nearestWps = waypoints.OrderBy(w => player.Position.DistanceTo(w));
            var waypoint = player.CurrWpId == 0 ? nearestWps.FirstOrDefault() : waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();

            if (player.CurrWpId == 0)
            {
                // No current WP -> pick nearest WP
                Console.WriteLine("No CurrWpId... Selecting new one");
                bool newWpFound = false;
                waypoint = nearestWps.FirstOrDefault();
                int wpCounter = 0;
                while (!newWpFound)
                {
                    wpCounter++;
                    if (!player.BlackListedWps.Contains(waypoint.ID))
                        newWpFound = true;
                    else
                        waypoint = nearestWps.ElementAtOrDefault(wpCounter) == null ? waypoint : nearestWps.ElementAtOrDefault(wpCounter);

                    if (wpCounter > 100) newWpFound = true;
                }
            }
            else
            {
                // Check if curr waypoint is reached
                if (player.Position.DistanceTo(waypoint) < 3.0F || player.WpStuckCount > 10)
                {
                    Console.WriteLine($"WP: {waypoint.ID} " + (player.WpStuckCount > 10 ? "couldn't be reached" : "reached") + ", selecting new WP...");
                    // Check if player is higher level than waypoint maxlevel
                    player.HasOverLeveled = !HotspotIsBg(hotspot.Id) && player.Level > waypoint.MaxLevel;

                    if (player.Position.DistanceTo(waypoint) < 3.0F)
                    {
                        player.VisitedWps.Add(waypoint.ID);
                        LogToFile(waypoint.ID + "  (" + waypoint.GetZoneName() + ")");
                        if (ObjectManager.MapId != 559)
                            player.LastWpId = waypoint.ID;

                        if (player.CurrZone != waypoint.Zone)
                            Console.WriteLine("Bot arrived at new zone!");
                        player.CurrZone = waypoint.Zone; // Update current zone
                        player.HasBeenStuckAtWp = false;

                        // Random chance to queue for BG or arena (if not in one already)
                        if (!HotspotIsBg(hotspot.Id) && random.Next(99)+1 < (player.HasOverLeveled ? 0 : 10) && player.Level >= 10 && !player.IsInCombat)
                            botStates.Push(new BattlegroundQueueState(botStates, container));
                        else if (!HotspotIsBg(hotspot.Id) && random.Next(99)+1 < (player.HasOverLeveled ? 0 : 2) && player.Level >= 10 && !player.IsInCombat)
                            botStates.Push(new ArenaSkirmishQueueState(botStates, container));
                    }

                    // Set new WP based on forced path if player has overleveled
                    if (player.HasOverLeveled)
                    {
                        var stayOnWp = false;
                        if (player.ForcedWpPath.Count == 0 || player.WpStuckCount > 10)
                        {
                            // If stuck on forcedwppath get new forcedwppath to new zone but make sure it's a new path
                            if (player.WpStuckCount > 10)
                            {
                                // Check if waypoint ID is the same as lastwpid
                                // which means that the first WP of the new path can't be reached either
                                // This effectively means that we've arrived at wpStuckCount twice without
                                // progress. Therefore we force teleport to CurrWp.
                                // Also force teleport if stuck at WP 2141 (Thousand needles -> Tanaris).
                                if (waypoint.ID == player.LastWpId)
                                {
                                    Console.WriteLine($"Forcing teleport to WP: {waypoint.ID} due to being stuck in new path.");
                                    player.LuaCall($"SendChatMessage('.npcb wp go {waypoint.ID}')");
                                    player.ForcedWpPath = ForcedWpPathViaBFS(waypoint.ID, false);
                                    if (player.ForcedWpPath == null)
                                        player.ForcedWpPath = ForcedWpPathViaBFS(waypoint.ID, true);
                                }
                                else
                                {
                                    // Try to find a new path to the same new zone
                                    player.BlackListedWps.Add(waypoint.ID);
                                    var lastNewZone = player.ForcedWpPath.Count > 0 ? waypoints.Where(x => x.ID == player.ForcedWpPath[player.ForcedWpPath.Count - 1]).FirstOrDefault().Zone : "0";
                                    var prevForcedWpPath = player.ForcedWpPath;
                                    // Find new path to zone
                                    player.ForcedWpPath = ForcedWpPathViaBFS(player.LastWpId, false);
                                    if (player.ForcedWpPath == null)
                                        player.ForcedWpPath = ForcedWpPathViaBFS(player.LastWpId, true);
                                    var newZoneWp = waypoints.Where(x => x.ID == player.ForcedWpPath[player.ForcedWpPath.Count - 1]).FirstOrDefault();
                                    // Check if new wp path leads to the same new zone as previously, otherwise force teleport
                                    if (lastNewZone != newZoneWp.Zone)
                                    {
                                        Console.WriteLine($"Forcing teleport to WP: {waypoint.ID} due to being stuck in new path and couldn't find another path to the same new zone.");
                                        player.BlackListedWps.Remove(waypoint.ID);
                                        player.LuaCall($"SendChatMessage('.npcb wp go {waypoint.ID}')");
                                        player.ForcedWpPath = prevForcedWpPath;
                                        stayOnWp = true;
                                    }
                                }
                            }
                            else
                            {
                                // If ForcedWpPath is empty
                                player.ForcedWpPath = ForcedWpPathViaBFS(waypoint.ID, false);
                                if (player.ForcedWpPath == null)
                                    player.ForcedWpPath = ForcedWpPathViaBFS(waypoint.ID, true);

                                // Remove first value since it's the same as the currently reached WP
                                if (player.ForcedWpPath.First() == waypoint.ID) player.ForcedWpPath.Remove(player.ForcedWpPath.First());
                            }
                            Console.WriteLine("New WP path:");
                            foreach (var wpInPath in player.ForcedWpPath)
                                Console.Write(wpInPath != player.ForcedWpPath[player.ForcedWpPath.Count - 1] ? wpInPath + " -> " : wpInPath + "\n\n");
                        }
                        // Set new WP
                        if (!stayOnWp)
                        {
                            waypoint = hotspot.Waypoints.Where(x => x.ID == player.ForcedWpPath.First()).FirstOrDefault();
                            player.ForcedWpPath.Remove(player.ForcedWpPath.First());
                        }
                    }
                    else
                    {
                        if (player.WpStuckCount > 10 && player.HasBeenStuckAtWp)
                        {
                            Console.WriteLine($"Forcing teleport to WP: {waypoint.ID} due to being stuck in new path.");
                            player.LuaCall($"SendChatMessage('.npcb wp go {waypoint.ID}')");
                        }
                        else
                        {
                            if (player.WpStuckCount > 10)
                                player.HasBeenStuckAtWp = true;
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

                                if (HotspotIsBg(hotspot.Id))
                                {
                                    if (!player.BlackListedWps.Contains(linkWp.ID) && !player.HasVisitedWp(linkWp.ID))
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
                                else
                                {
                                    // Check level requirement
                                    if (linkWp.MinLevel <= player.Level && !player.BlackListedWps.Contains(linkWp.ID))
                                    {
                                        //if (!player.HasVisitedWp(linkWp.ID) && linkWp.MinLevel <= player.Level && linkWp.MaxLevel > player.Level)
                                        if (linkWp.MinLevel <= player.Level && linkWp.MaxLevel > player.Level)
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
                    }
                    // Reset WP checking values
                    player.DeathsAtWp = 0;
                    player.WpStuckCount = 0;
                }
                else
                {
                    Console.WriteLine($"CurrWP not reached yet. Distance: {player.Position.DistanceTo(waypoint)}, wpStuckCount: {player.WpStuckCount}");
                    //player.wpStuckCount++; // This is increased in StuckHelper
                }
            }
            // Try to mount
            string MountName = "white polar bear";
            if (!HotspotIsBg(hotspot.Id) && player.Level >= 40 && !player.IsMounted)
                player.LuaCall($"CastSpellByName('{MountName}')");
            else if (HotspotIsBg(hotspot.Id) && !player.IsMounted)
                player.LuaCall($"CastSpellByName('{MountName}')");

            player.CurrWpId = waypoint.ID;
            Console.WriteLine("Selected waypoint: " + waypoint.ToStringFull() + ", HasOverleveled: " + player.HasOverLeveled);
            botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
        }

        private bool HotspotIsBg(int hotspotId)
        {
            return (hotspotId > 8 && hotspotId < 13);
        }

        public List<int> ForcedWpPathViaBFS(int startId, bool ignoreBlacklistedWps)
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
                if (currentWaypoint == null)
                {
                    Console.WriteLine("Current WP is null (ForcedWpPathViaBFS), ID: " + currentId + ", startId: " + startId);
                    return null;
                }

                if (currentWaypoint.MaxLevel > player.Level && currentWaypoint.MinLevel <= player.Level
                    && !player.HasVisitedWp(currentId))
                {
                    Console.WriteLine("Found new WP matching player level: " + currentWaypoint.ToStringFull() + "\n");
                    return currentPath;
                }
                // Player could be above all WP maxlevels, so make an exception
                // for those players so that they can move through the zones.
                // Hotspot 1-4 are Azeroth WPs, 5,6 Outland, and 7,8 Northrend
                else if (currentWaypoint.Zone != player.CurrZone && ((hotspot.Id < 5 && player.Level >= 60)
                    || ((hotspot.Id == 5 || hotspot.Id == 6) && player.Level >= 70) 
                    || ((hotspot.Id == 7 || hotspot.Id == 8) && player.Level >= 80)))
                {
                    // Try to visit new zones
                    bool currWpIsNewZone = true;
                    foreach (int visitedWpId in player.VisitedWps)
                    {
                        var visitedWp = hotspot.Waypoints.Where(x => x.ID == visitedWpId).FirstOrDefault();
                        if (visitedWp.Zone == currentWaypoint.Zone)
                            currWpIsNewZone = false;
                    }

                    if (currWpIsNewZone)
                    {
                        Console.WriteLine("Found new WP matching player level (> hotspot maxlevel): " + currentWaypoint.ToStringFull() + "\n");
                        return currentPath;
                    }
                }

                // Search and enqueue links
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
                        if (!visited.Contains(linkId) && (ignoreBlacklistedWps || !player.BlackListedWps.Contains(linkId)))
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
