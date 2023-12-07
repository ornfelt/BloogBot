using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the state of the bot when it is moving towards a corpse.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state where the bot moves to a corpse.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot moves to the corpse location.
    /// </summary>
    public class MoveToCorpseState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a helper class for handling stuck operations.
        /// </summary>
        readonly StuckHelper stuckHelper;

        /// <summary>
        /// Represents a boolean value indicating whether the entity is capable of walking on water.
        /// </summary>
        bool walkingOnWater;
        /// <summary>
        /// Represents the count of times the program got stuck.
        /// </summary>
        int stuckCount;

        /// <summary>
        /// Represents a boolean value indicating whether the object has been initialized.
        /// </summary>
        bool initialized;
        /// <summary>
        /// Indicates whether the specified waypoint is close to a corpse.
        /// </summary>
        private static bool s_HasReachedWpCloseToCorpse;

        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Initializes a new instance of the MoveToCorpseState class.
        /// </summary>
        public MoveToCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
            s_HasReachedWpCloseToCorpse = false;
            stuckCount = 0;
        }

        /// <summary>
        /// Updates the state of the object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Update: Check if initialized
        /// Update -> Container: DisableTeleportChecker
        /// Update -> StuckHelper: CheckIfStuck
        /// StuckHelper --> Update: Return result
        /// Update -> Console: Write stuckCount
        /// Update -> Player: Check Position DistanceTo2D
        /// Player --> Update: Return result
        /// Update -> Player: ForcedWpPath
        /// Update -> Player: StopAllMovement
        /// Update -> BotStates: Pop
        /// Update -> Update: Check HasReachedWpCloseToCorpse
        /// Update -> Update: Set HasReachedWpCloseToCorpse
        /// Update -> Console: Write HasReachedWpCloseToCorpse
        /// Update -> Navigation: GetNextWaypoint
        /// Navigation --> Update: Return nextWaypoint
        /// Update -> Update: Check if walkingOnWater
        /// Update -> Player: StartMovement
        /// Update -> Update: Check if stop walkingOnWater
        /// Update -> Player: StopMovement
        /// Update -> Player: MoveToward
        /// Update -> Player: LuaCall
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (!initialized)
            {
                container.DisableTeleportChecker = false;
                initialized = true;
            }

            if (stuckHelper.CheckIfStuck())
                Console.WriteLine("stuckCount in MovetoCorpseState: " + stuckCount++);

            if (player.Position.DistanceTo2D(player.CorpsePosition) < 3 || !player.InGhostForm)
            {
                player.ForcedWpPath = new List<int>();
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            if (!s_HasReachedWpCloseToCorpse)
            {
                s_HasReachedWpCloseToCorpse = HasReachedWpCloseToCorpse();
                if (s_HasReachedWpCloseToCorpse)
                    Console.WriteLine("HasReachedWpCloseToCorpse!");
            }

            if (s_HasReachedWpCloseToCorpse)
            {
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

            // Force teleport to corpse pos
            if (stuckCount > 40)
                player.LuaCall($"SendChatMessage('.go xyz {player.CorpsePosition.X.ToString().Replace(',', '.')}" +
                    $" {player.CorpsePosition.Y.ToString().Replace(',', '.')}" +
                    $" {player.CorpsePosition.Z.ToString().Replace(',', '.')}', 'GUILD', nil)");
        }

        /// <summary>
        /// Try to move to corpse with a path based on waypoints.
        /// </summary>
        // Try to move to corpse with a path based on WPs
        /// <remarks>
        /// \startuml
        /// container -> hotspot: GetCurrentHotspot()
        /// hotspot -> player: OrderBy(w => player.Position.DistanceTo(w))
        /// hotspot -> player: OrderBy(w => player.CorpsePosition.DistanceTo(w)).FirstOrDefault()
        /// player -> hotspot: Where(x => x.ID == player.CurrWpId).FirstOrDefault()
        /// player -> player: Position.DistanceTo(wpCloseToCorpse) < 20 || stuckCount > 20 || Position.DistanceTo2D(player.CorpsePosition) < 20
        /// player -> player: ForcedWpPath.Count == 0 && currWp != wpCloseToCorpse
        /// player -> Console: WriteLine("New WP path (to corpse):")
        /// player -> player: Position.DistanceTo(currWp) < 3
        /// hotspot -> player: Where(x => x.ID == player.ForcedWpPath.First()).FirstOrDefault()
        /// player -> player: ForcedWpPath.Remove(player.ForcedWpPath.First())
        /// player -> player: CurrWpId = currWp.ID
        /// player -> player: MoveToward(currWp)
        /// \enduml
        /// </remarks>
        public bool HasReachedWpCloseToCorpse()
        {
            var hotspot = container.GetCurrentHotspot();
            var nearestWps = hotspot.Waypoints.OrderBy(w => player.Position.DistanceTo(w));
            var wpCloseToCorpse = hotspot.Waypoints.OrderBy(w => player.CorpsePosition.DistanceTo(w)).FirstOrDefault();
            var waypoint = nearestWps.FirstOrDefault();
            var currWp = player.CurrWpId == 0 ? waypoint : hotspot.Waypoints.Where(x => x.ID == player.CurrWpId).FirstOrDefault();

            // This should return true if last WP in forcedwppath is reached (WP close to corpse), or stuckCount > 20, or close to corpse
            if (player.Position.DistanceTo(wpCloseToCorpse) < 20 || stuckCount > 20
                || player.Position.DistanceTo2D(player.CorpsePosition) < 20)
                return true;

            if (player.ForcedWpPath.Count == 0 && currWp != wpCloseToCorpse)
            {
                player.ForcedWpPath = ForcedWpPathToCorpse(waypoint.ID, wpCloseToCorpse.ID);
                Console.WriteLine("New WP path (to corpse):");
                foreach (var wpInPath in player.ForcedWpPath)
                    Console.Write(wpInPath != player.ForcedWpPath[player.ForcedWpPath.Count - 1] ? wpInPath + " -> " : wpInPath + "\n\n");
            }
            else if (player.Position.DistanceTo(currWp) < 3)
            {
                // Set new WP
                currWp = hotspot.Waypoints.Where(x => x.ID == player.ForcedWpPath.First()).FirstOrDefault();
                player.ForcedWpPath.Remove(player.ForcedWpPath.First());
            }

            player.CurrWpId = currWp.ID;
            player.MoveToward(currWp);
            return false;
        }

        /// <summary>
        /// Finds a path from a starting waypoint ID to an ending waypoint ID using a breadth-first search algorithm.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "ForcedWpPathToCorpse()" as F
        /// participant "Hotspot" as H
        /// participant "Queue" as Q
        /// participant "HashSet" as HS
        /// participant "Waypoint" as W
        /// 
        /// F -> H: GetCurrentHotspot()
        /// activate H
        /// H --> F: Return current hotspot
        /// deactivate H
        /// 
        /// F -> Q: Enqueue(startId)
        /// activate Q
        /// Q --> F: Enqueue successful
        /// deactivate Q
        /// 
        /// loop while queue.Count > 0
        ///     F -> Q: Dequeue()
        ///     activate Q
        ///     Q --> F: Return currentPath
        ///     deactivate Q
        /// 
        ///     F -> H: Waypoints.Where(x => x.ID == currentId).FirstOrDefault()
        ///     activate H
        ///     H --> F: Return currentWaypoint
        ///     deactivate H
        /// 
        ///     alt if currentId == endId
        ///         F --> F: return currentPath
        ///     else if !visited.Contains(currentId)
        ///         F -> HS: Add(currentId)
        ///         activate HS
        ///         HS --> F: Add successful
        ///         deactivate HS
        /// 
        ///         F -> W: Parse and split links
        ///         activate W
        ///         W --> F: Return linkSplit
        ///         deactivate W
        /// 
        ///         loop for each linkWp in linkSplit
        ///             alt if !visited.Contains(linkId)
        ///                 F -> Q: Enqueue(newPath)
        ///                 activate Q
        ///                 Q --> F: Enqueue successful
        ///                 deactivate Q
        ///             end
        ///         end
        ///     end
        /// end
        /// F --> F: return currentPath
        /// \enduml
        /// </remarks>
        private List<int> ForcedWpPathToCorpse(int startId, int endId)
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

                // CurrWP should be close enough to corpse - return WP-based path to it
                if (currentId == endId)
                    return currentPath;

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
    }
}
