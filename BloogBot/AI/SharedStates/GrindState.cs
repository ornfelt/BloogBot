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
        bool isInBg;
        int playerLevel;

        public GrindState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            var enemyTarget = container.FindClosestTarget();

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
            // Initialize variables
            var hotspot = container.GetCurrentHotspot();
            player = ObjectManager.Player;
            isInBg = IsHotspotBg(hotspot.Id);
            playerLevel = player.Level;
            player.StuckInStateOrPosCount = 0;

            EnsurePlayerHasZone(hotspot);

            var waypoints = hotspot.Waypoints;
            var nearestWps = waypoints.OrderBy(w => player.Position.DistanceTo(w));
            var waypoint = player.CurrWpId == 0 ? nearestWps.FirstOrDefault() : waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();

            if (player.CurrWpId == 0)
                waypoint = SelectNewWaypoint(nearestWps);
            else
                waypoint = SelectNewWaypointFromExisting(waypoints, waypoint, hotspot);

            TryToMount();

            // Update current WP
            player.CurrWpId = waypoint.ID;
            Console.WriteLine("Selected waypoint: " + waypoint.ToStringFull() + ", HasOverleveled: " + player.HasOverLeveled);
            botStates.Push(new MoveToHotspotWaypointState(botStates, container, waypoint));
        }

        private void EnsurePlayerHasZone(Hotspot hotspot)
        {
            if (player.CurrZone == "0")
            {
                var nearestWp = hotspot.Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault();
                player.CurrZone = nearestWp.Zone;
                Console.WriteLine("No zone currently set. Setting zone based on nearest WP: " + player.CurrZone);
            }
        }

        //private void SelectNewWaypoint(ref Position waypoint, IOrderedEnumerable<Position> nearestWps)
        private Position SelectNewWaypoint(IOrderedEnumerable<Position> nearestWps)
        {
            Console.WriteLine("No CurrWpId... Selecting new one");
            bool newWpFound = false;
            var waypoint = nearestWps.FirstOrDefault();
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

            return waypoint;
        }

        private Position SelectNewWaypointFromExisting(IEnumerable<Position> waypoints, Position waypoint, Hotspot hotspot)
        {
            if (player.Position.DistanceTo(waypoint) < 3.0F || player.WpStuckCount > 10)
            {
                Console.WriteLine($"WP: {waypoint.ID} " + (player.WpStuckCount > 10 ? "couldn't be reached" : "reached") + ", selecting new WP...");
                // Check if player is higher level than waypoint maxlevel
                player.HasOverLeveled = !isInBg && (playerLevel >= waypoint.MaxLevel || playerLevel < waypoint.MinLevel);

                if (player.Position.DistanceTo(waypoint) < 3.0F)
                    HandleWaypointReachedActions(waypoint, hotspot.Id);

                // Set new WP based on forced path if player has overleveled
                if (player.HasOverLeveled)
                    waypoint = SelectOverleveledWaypoint(waypoints, waypoint);
                else
                {
                    // If stuck
                    if (player.WpStuckCount > 10 && player.HasBeenStuckAtWp)
                    {
                        Console.WriteLine($"Forcing teleport to WP: {waypoint.ID} due to being stuck in new path.");
                        player.LuaCall($"SendChatMessage('.npcb wp go {waypoint.ID}')");
                    }
                    else
                        waypoint = SelectNewLinkedWaypoint(waypoints, waypoint);
                }
                // Reset WP checking values
                player.DeathsAtWp = 0;
                player.WpStuckCount = 0;
            }
            else
                Console.WriteLine($"CurrWP not reached yet. Distance: {player.Position.DistanceTo(waypoint)}, wpStuckCount: {player.WpStuckCount}");
            return waypoint;
        }

        private Position SelectNewLinkedWaypoint(IEnumerable<Position> waypoints, Position waypoint)
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
                var linkWp = waypoints.Where(x => x.ID == Int32.Parse(linkSplit[randLink])).FirstOrDefault();

                if (isInBg)
                {
                    if (!player.BlackListedWps.Contains(linkWp.ID) && !player.HasVisitedWp(linkWp.ID))
                    {
                        waypoint = linkWp;
                        newWpFound = true;
                    }
                }
                else
                {
                    // Check level requirement
                    if (linkWp.MinLevel <= playerLevel && !player.BlackListedWps.Contains(linkWp.ID))
                    {
                        //if (!player.HasVisitedWp(linkWp.ID) && linkWp.MinLevel <= player.Level && linkWp.MaxLevel > player.Level)
                        if (linkWp.MinLevel <= playerLevel && linkWp.MaxLevel > playerLevel)
                        {
                            waypoint = linkWp;
                            newWpFound = true;
                        }
                    }
                }
                if (linkSearchCount > 15)
                {
                    // Choose current random WP if no other links are suitable
                    waypoint = linkWp;
                    newWpFound = true;
                }
            }
            return waypoint;
        }

        private void HandleWaypointReachedActions(Position waypoint, int hotspotId)
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
            if (!isInBg && random.Next(99) + 1 < (player.HasOverLeveled ? 0 : 10) && playerLevel >= 10 && !player.IsInCombat)
                botStates.Push(new BattlegroundQueueState(botStates, container));
            else if (!isInBg && random.Next(99) + 1 < (player.HasOverLeveled ? 0 : 5) && playerLevel >= 20 && !player.IsInCombat)
                botStates.Push(new ArenaSkirmishQueueState(botStates, container));
            if (!isInBg && playerLevel != 80)
            {
                // Check if bot needs to teleport to outland / northrend.
                // This should only occur if bot somehow doesn't manage to teleport back after arena skirmish
                if (playerLevel >= 70 && hotspotId != 7 && hotspotId != 8)
                {
                    player.LuaCall($"SendChatMessage('.npcb wp go 2706')"); // Teleport to Northrend
                    player.HasEnteredNewMap = true;
                }
                else if (playerLevel >= 60 && playerLevel < 70 && hotspotId != 5 && hotspotId != 6)
                {
                    player.LuaCall($"SendChatMessage('.npcb wp go 2583')"); // Teleport to Outland
                    player.HasEnteredNewMap = true;
                }
            }
        }

        private Position SelectOverleveledWaypoint(IEnumerable<Position> waypoints, Position waypoint)
        {
            var stayOnWp = false;
            if (player.ForcedWpPath == null || player.ForcedWpPath?.Count == 0 || player.WpStuckCount > 10)
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
                waypoint = waypoints.Where(x => x.ID == player.ForcedWpPath.First()).FirstOrDefault();
                player.ForcedWpPath.Remove(player.ForcedWpPath.First());
            }
            return waypoint;
        }

        private void TryToMount()
        {
            string MountName = "white polar bear";
            if (!isInBg && playerLevel >= 40 && !player.IsMounted || isInBg && !player.IsMounted)
            //if ((!isInBg && playerLevel >= 40 && !player.IsMounted) || (isInBg && !player.IsMounted))
            {
                player.LuaCall($"CastSpellByName('{MountName}')");
            }
        }

        /// <summary>
        /// Determines if the given hotspot ID is a battleground hotspot.
        /// </summary>
        private bool IsHotspotBg(int hotspotId)
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

                if (currentWaypoint.MaxLevel > playerLevel && currentWaypoint.MinLevel <= playerLevel
                    && (ignoreBlacklistedWps || (!player.HasVisitedWp(currentId))))
                {
                    Console.WriteLine($"Found new WP matching player level (ignoreBlacklistedWps: {ignoreBlacklistedWps}): " + currentWaypoint.ToStringFull() + "\n");
                    return currentPath;
                }
                // Player could be above all WP maxlevels, so make an exception
                // for those players so that they can move through the zones.
                // Hotspot 1-4 are Azeroth WPs, 5,6 Outland, and 7,8 Northrend
                else if (currentWaypoint.Zone != player.CurrZone && ((hotspot.Id < 5 && playerLevel >= 60)
                    || ((hotspot.Id == 5 || hotspot.Id == 6) && playerLevel >= 70)
                    || ((hotspot.Id == 7 || hotspot.Id == 8) && playerLevel >= 80)))
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
                        Console.WriteLine($"Found new WP matching player level (> hotspot maxlevel, ignoreBlacklistedWps: {ignoreBlacklistedWps}): " + currentWaypoint.ToStringFull() + "\n");
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
            return null;
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
