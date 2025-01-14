﻿using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BloogBot.AI.SharedStates
{
    public class MoveToCorpseState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly StuckHelper stuckHelper;

        bool walkingOnWater;
        int stuckCount;

        bool initialized;
        private static bool s_HasReachedWpCloseToCorpse;

        static readonly Random random = new Random();
        
        public MoveToCorpseState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
            s_HasReachedWpCloseToCorpse = false;
            stuckCount = 0;
        }

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

        // Try to move to corpse with a path based on WPs
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
