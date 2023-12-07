using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using BloogBot.UI;

/// <summary>
/// Represents the state of the bot when it is in the grinding mode.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot is grinding.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot is grinding.
    /// </summary>
    public class GrindState : IBotState
    {
        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a local player.
        /// </summary>
        LocalPlayer player;
        /// <summary>
        /// Represents a boolean value indicating whether the code is running in the background.
        /// </summary>
        bool isInBg;
        /// <summary>
        /// Represents the level of the player.
        /// </summary>
        int playerLevel;

        /// <summary>
        /// Initializes a new instance of the GrindState class.
        /// </summary>
        public GrindState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's target and bot states based on the closest enemy target and the player's position.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Container: FindClosestTarget()
        /// Container --> Update: enemyTarget
        /// Update -> Math: Abs(enemyTarget.Position.Z - player.Position.Z)
        /// Math --> Update: distance
        /// Update -> Player: SetTarget(enemyTarget.Guid)
        /// Update -> Container: CreateMoveToTargetState(botStates, container, enemyTarget)
        /// Container --> Update: moveToTargetState
        /// Update -> BotStates: Push(moveToTargetState)
        /// alt enemyTarget is null or distance >= 16.0F
        /// Update -> Update: HandleWpSelection()
        /// end
        /// \enduml
        /// </remarks>
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

        /// <summary>
        /// Handles the selection of a waypoint for the bot to move to.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// HandleWpSelection -> container: GetCurrentHotspot()
        /// HandleWpSelection -> ObjectManager: Player
        /// HandleWpSelection -> HandleWpSelection: IsHotspotBg(hotspot.Id)
        /// HandleWpSelection -> player: Level
        /// HandleWpSelection -> player: StuckInStateOrPosCount = 0
        /// HandleWpSelection -> HandleWpSelection: EnsurePlayerHasZone(hotspot)
        /// HandleWpSelection -> hotspot: Waypoints
        /// HandleWpSelection -> player: Position.DistanceTo(w)
        /// HandleWpSelection -> player: CurrWpId == 0 ? nearestWps.FirstOrDefault() : waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault()
        /// alt player.CurrWpId == 0
        ///   HandleWpSelection -> HandleWpSelection: SelectNewWaypoint(nearestWps)
        /// else
        ///   HandleWpSelection -> HandleWpSelection: SelectNewWaypointFromExisting(waypoints, waypoint, hotspot)
        /// end
        /// HandleWpSelection -> HandleWpSelection: TryToMount()
        /// HandleWpSelection -> player: CurrWpId = waypoint.ID
        /// HandleWpSelection -> Console: WriteLine("Selected waypoint: " + waypoint.ToStringFull() + ", HasOverleveled: " + player.HasOverLeveled)
        /// HandleWpSelection -> botStates: Push(new MoveToHotspotWaypointState(botStates, container, waypoint))
        /// \enduml
        /// </remarks>
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

        /// <summary>
        /// Ensures that the player has a zone assigned based on the nearest waypoint.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// EnsurePlayerHasZone -> player: Check CurrZone
        /// alt CurrZone is "0"
        ///   EnsurePlayerHasZone -> hotspot: Get Waypoints
        ///   hotspot --> EnsurePlayerHasZone: Return Waypoints
        ///   EnsurePlayerHasZone -> player: Calculate DistanceTo each Waypoint
        ///   player --> EnsurePlayerHasZone: Return Distances
        ///   EnsurePlayerHasZone -> player: Set CurrZone to nearest Waypoint's Zone
        ///   EnsurePlayerHasZone -> Console: Write "No zone currently set. Setting zone based on nearest WP: " + player.CurrZone
        /// end
        /// \enduml
        /// </remarks>
        private void EnsurePlayerHasZone(Hotspot hotspot)
        {
            if (player.CurrZone == "0")
            {
                var nearestWp = hotspot.Waypoints.OrderBy(w => player.Position.DistanceTo(w)).FirstOrDefault();
                player.CurrZone = nearestWp.Zone;
                Console.WriteLine("No zone currently set. Setting zone based on nearest WP: " + player.CurrZone);
            }
        }

        /// <summary>
        /// Selects a new waypoint from the given list of nearest waypoints.
        /// </summary>
        //private void SelectNewWaypoint(ref Position waypoint, IOrderedEnumerable<Position> nearestWps)
        /// <remarks>
        /// \startuml
        /// participant "SelectNewWaypoint()" as A
        /// participant "Console" as B
        /// participant "nearestWps" as C
        /// participant "player" as D
        /// 
        /// A -> B: WriteLine("No CurrWpId... Selecting new one")
        /// A -> C: FirstOrDefault()
        /// A -> D: BlackListedWps.Contains(waypoint.ID)
        /// loop until newWpFound
        ///     A -> C: ElementAtOrDefault(wpCounter)
        ///     A -> D: BlackListedWps.Contains(waypoint.ID)
        ///     note over A: If wpCounter > 100, set newWpFound to true
        /// end
        /// A --> A: return waypoint
        /// \enduml
        /// </remarks>
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

        /// <summary>
        /// Selects a new waypoint from the existing list of waypoints based on certain conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Position -> Player: DistanceTo(waypoint)
        /// Player -> Position: Return distance
        /// Position -> Player: WpStuckCount
        /// Player -> Position: Return WpStuckCount
        /// Position -> Console: WriteLine
        /// Position -> Player: HasOverLeveled
        /// Player -> Position: Return HasOverLeveled
        /// Position -> Player: DistanceTo(waypoint)
        /// Player -> Position: Return distance
        /// Position -> Position: HandleWaypointReachedActions(waypoint, hotspot.Id)
        /// Position -> Player: HasOverLeveled
        /// Player -> Position: Return HasOverLeveled
        /// Position -> Position: SelectOverleveledWaypoint(waypoints, waypoint)
        /// Position -> Player: WpStuckCount
        /// Player -> Position: Return WpStuckCount
        /// Position -> Player: HasBeenStuckAtWp
        /// Player -> Position: Return HasBeenStuckAtWp
        /// Position -> Console: WriteLine
        /// Position -> Player: LuaCall
        /// Position -> Position: SelectNewLinkedWaypoint(waypoints, waypoint)
        /// Position -> Player: DeathsAtWp
        /// Player -> Position: Return DeathsAtWp
        /// Position -> Player: WpStuckCount
        /// Player -> Position: Return WpStuckCount
        /// Position -> Console: WriteLine
        /// Position -> Position: Return waypoint
        /// \enduml
        /// </remarks>
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

        /// <summary>
        /// Selects a new linked waypoint based on the given list of waypoints and current waypoint.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "SelectNewLinkedWaypoint()" as S
        /// participant "Player" as P
        /// participant "Waypoint" as W
        /// S -> P: player.WpStuckCount
        /// S -> P: player.HasBeenStuckAtWp
        /// S -> W: waypoint.Links
        /// S -> W: waypoint.ID
        /// S -> P: player.BlackListedWps
        /// S -> P: player.HasVisitedWp
        /// S -> W: linkWp.MinLevel
        /// S -> W: linkWp.MaxLevel
        /// S -> P: playerLevel
        /// S -> W: waypoint
        /// S --> S: Return waypoint
        /// \enduml
        /// </remarks>
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
                    if (linkWp.MinLevel <= playerLevel && linkWp.MaxLevel > playerLevel
                        && !player.BlackListedWps.Contains(linkWp.ID))
                    {
                        waypoint = linkWp;
                        newWpFound = true;
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

        /// <summary>
        /// Handles the actions when a waypoint is reached.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// HandleWaypointReachedActions -> player : VisitedWps.Add(waypoint.ID)
        /// HandleWaypointReachedActions -> : LogToFile(waypoint.ID + "  (" + waypoint.GetZoneName() + ")")
        /// HandleWaypointReachedActions -> ObjectManager : MapId != 559
        /// ObjectManager --> HandleWaypointReachedActions : MapId
        /// HandleWaypointReachedActions -> player : LastWpId = waypoint.ID
        /// HandleWaypointReachedActions -> player : CurrZone != waypoint.Zone
        /// player --> HandleWaypointReachedActions : CurrZone
        /// HandleWaypointReachedActions -> Console : WriteLine("Bot arrived at new zone!")
        /// HandleWaypointReachedActions -> player : CurrZone = waypoint.Zone
        /// HandleWaypointReachedActions -> player : HasBeenStuckAtWp = false
        /// HandleWaypointReachedActions -> : random.Next(99) + 1 < (player.HasOverLeveled ? 0 : 10)
        /// HandleWaypointReachedActions -> botStates : Push(new BattlegroundQueueState(botStates, container))
        /// HandleWaypointReachedActions -> : random.Next(99) + 1 < (player.HasOverLeveled ? 0 : 5)
        /// HandleWaypointReachedActions -> botStates : Push(new ArenaSkirmishQueueState(botStates, container))
        /// HandleWaypointReachedActions -> player : playerLevel >= 70 && hotspotId != 7 && hotspotId != 8
        /// HandleWaypointReachedActions -> player : LuaCall($"SendChatMessage('.npcb wp go 2706')")
        /// HandleWaypointReachedActions -> player : HasEnteredNewMap = true
        /// HandleWaypointReachedActions -> player : playerLevel >= 60 && playerLevel < 70 && hotspotId != 5 && hotspotId != 6
        /// HandleWaypointReachedActions -> player : LuaCall($"SendChatMessage('.npcb wp go 2583')")
        /// HandleWaypointReachedActions -> player : HasEnteredNewMap = true
        /// \enduml
        /// </remarks>
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

        /// <summary>
        /// Selects an overleveled waypoint from a collection of waypoints based on the current waypoint.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Player -> SelectOverleveledWaypoint: waypoints, waypoint
        /// SelectOverleveledWaypoint -> Player: ForcedWpPath, WpStuckCount
        /// alt WpStuckCount > 10
        ///     SelectOverleveledWaypoint -> Player: LastWpId
        ///     alt waypoint.ID == LastWpId
        ///         SelectOverleveledWaypoint -> Console: WriteLine
        ///         SelectOverleveledWaypoint -> Player: LuaCall
        ///         SelectOverleveledWaypoint -> Player: ForcedWpPathViaBFS
        ///     else
        ///         SelectOverleveledWaypoint -> Player: BlackListedWps.Add
        ///         SelectOverleveledWaypoint -> Player: ForcedWpPathViaBFS
        ///         SelectOverleveledWaypoint -> Player: BlackListedWps.Remove
        ///         SelectOverleveledWaypoint -> Player: LuaCall
        ///     end
        /// else
        ///     SelectOverleveledWaypoint -> Player: ForcedWpPathViaBFS
        ///     SelectOverleveledWaypoint -> Player: ForcedWpPath.Remove
        /// end
        /// SelectOverleveledWaypoint -> Console: WriteLine
        /// SelectOverleveledWaypoint -> Player: ForcedWpPath.First
        /// SelectOverleveledWaypoint -> Player: ForcedWpPath.Remove
        /// SelectOverleveledWaypoint --> Player: waypoint
        /// \enduml
        /// </remarks>
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

        /// <summary>
        /// Tries to mount the player character.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// TryToMount -> Player: Check if player is mounted
        /// alt not in BG and player level >= 40 or in BG
        /// TryToMount -> Player: CastSpellByName('white polar bear')
        /// end
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// participant "Caller Method" as Caller
        /// participant "IsHotspotBg Method" as IsHotspotBg
        /// Caller -> IsHotspotBg: hotspotId
        /// IsHotspotBg --> Caller: return (hotspotId > 8 && hotspotId < 13)
        /// \enduml
        /// </remarks>
        private bool IsHotspotBg(int hotspotId)
        {
            return (hotspotId > 8 && hotspotId < 13);
        }

        /// <summary>
        /// Performs a breadth-first search to find a forced waypoint path.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ForcedWpPathViaBFS -> GetCurrentHotspot: Get current hotspot
        /// ForcedWpPathViaBFS -> Queue: Initialize queue with startId
        /// loop while queue.Count > 0
        ///     ForcedWpPathViaBFS -> Queue: Dequeue currentPath
        ///     ForcedWpPathViaBFS -> Waypoints: Get current waypoint
        ///     alt if currentWaypoint is null
        ///         ForcedWpPathViaBFS -> Console: Write error message
        ///         ForcedWpPathViaBFS --> ForcedWpPathViaBFS: Return null
        ///     else if currentWaypoint matches player level
        ///         ForcedWpPathViaBFS -> Console: Write success message
        ///         ForcedWpPathViaBFS --> ForcedWpPathViaBFS: Return currentPath
        ///     else if currentWaypoint is in new zone
        ///         loop for each visited waypoint
        ///             ForcedWpPathViaBFS -> Waypoints: Get visited waypoint
        ///         end
        ///         alt if current waypoint is in new zone
        ///             ForcedWpPathViaBFS -> Console: Write success message
        ///             ForcedWpPathViaBFS --> ForcedWpPathViaBFS: Return currentPath
        ///         end
        ///     else if currentId is not visited
        ///         ForcedWpPathViaBFS -> HashSet: Add currentId to visited
        ///         ForcedWpPathViaBFS -> Waypoints: Get linked waypoints
        ///         loop for each linked waypoint
        ///             ForcedWpPathViaBFS -> Queue: Enqueue newPath
        ///         end
        ///     end
        /// end
        /// ForcedWpPathViaBFS --> ForcedWpPathViaBFS: Return currentPath or null
        /// \enduml
        /// </remarks>
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
            return ignoreBlacklistedWps ? currentPath : null; // Return current path or null
        }

        /// <summary>
        /// Logs the specified text to a file.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// LogToFile -> Path.GetDirectoryName: Get directory of MainViewModel assembly
        /// Path.GetDirectoryName --> LogToFile: Directory path
        /// LogToFile -> UriBuilder: Create UriBuilder with directory path
        /// UriBuilder --> LogToFile: UriBuilder object
        /// LogToFile -> Path.Combine: Combine directory path and "VisitedWanderNodes.txt"
        /// Path.Combine --> LogToFile: File path
        /// LogToFile -> File.Exists: Check if file exists at file path
        /// File.Exists --> LogToFile: Boolean result
        /// LogToFile -> File.AppendText: Open file for appending
        /// File.AppendText --> LogToFile: StreamWriter object
        /// LogToFile -> StreamWriter.WriteLine: Write text to file
        /// StreamWriter.WriteLine --> LogToFile: 
        /// LogToFile -> File.Exists: Check if alternative file exists at altFileName
        /// File.Exists --> LogToFile: Boolean result
        /// LogToFile -> File.AppendText: Open alternative file for appending
        /// File.AppendText --> LogToFile: StreamWriter object
        /// LogToFile -> StreamWriter.WriteLine: Write text to alternative file
        /// StreamWriter.WriteLine --> LogToFile: 
        /// \enduml
        /// </remarks>
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
