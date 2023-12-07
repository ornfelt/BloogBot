using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the AI functionality of BloogBot.
/// </summary>
namespace BloogBot.AI
{
    /// <summary>
    /// Represents a dependency container that implements the IDependencyContainer interface.
    /// </summary>
    /// <summary>
    /// Represents a dependency container that implements the IDependencyContainer interface.
    /// </summary>
    public class DependencyContainer : IDependencyContainer
    {
        /// <summary>
        /// Array of names for different types of oozes.
        /// </summary>
        static readonly string[] oozeNames = { "Acidic Swamp Ooze", "Black Slime", "Cloned Ectoplasm", "Cloned Ooze", "Corrosive Sap Beast", "Corrosive Swamp Ooze",
            "Cursed Ooze", "Devouring Ectoplasm", "Evolving Ectoplasm", "Gargantuan Ooze", "Glob of Viscidus", "Glutinous Ooze", "Green Sludge", "Irradiated Slime",
            "Jade Ooze", "Muculent Ooze", "Nightmare Ectoplasm", "Noxious Slime", "Plague Slime", "Primal Ooze", "Rotting Slime", "Sap Beast", "Silty Oozeling",
            "Tainted Ooze", "Vile Slime", "The Rot", "Viscidus", "The Ongar" };

        /// <summary>
        /// Gets or sets the targeting criteria for a World of Warcraft unit.
        /// </summary>
        readonly Func<WoWUnit, bool> targetingCriteria;

        /// <summary>
        /// Gets the dictionary of player trackers.
        /// </summary>
        IDictionary<string, PlayerTracker> PlayerTrackers { get; } = new Dictionary<string, PlayerTracker>();

        /// <summary>
        /// Initializes a new instance of the DependencyContainer class.
        /// </summary>
        public DependencyContainer(
                    Func<WoWUnit, bool> targetingCriteria,
                    Func<Stack<IBotState>, IDependencyContainer, IBotState> createRestState,
                    Func<Stack<IBotState>, IDependencyContainer, WoWUnit, IBotState> createMoveToTargetState,
                    Func<Stack<IBotState>, IDependencyContainer, WoWUnit, WoWPlayer, IBotState> createPowerlevelCombatState,
                    BotSettings botSettings,
                    Probe probe,
                    IEnumerable<Hotspot> hotspots)
        {
            this.targetingCriteria = targetingCriteria;

            CreateRestState = createRestState;
            CreateMoveToTargetState = createMoveToTargetState;
            CreatePowerlevelCombatState = createPowerlevelCombatState;
            BotSettings = botSettings;
            Probe = probe;
            Hotspots = hotspots;
        }

        /// <summary>
        /// Gets or sets the function used to create a new instance of the bot state for REST requests.
        /// </summary>
        public Func<Stack<IBotState>, IDependencyContainer, IBotState> CreateRestState { get; }

        /// <summary>
        /// Gets or sets the function that creates a move to target state.
        /// </summary>
        public Func<Stack<IBotState>, IDependencyContainer, WoWUnit, IBotState> CreateMoveToTargetState { get; }

        /// <summary>
        /// Creates a combat state for power leveling, using the specified stack of bot states, dependency container, WoW unit, and WoW player.
        /// </summary>
        public Func<Stack<IBotState>, IDependencyContainer, WoWUnit, WoWPlayer, IBotState> CreatePowerlevelCombatState { get; }

        /// <summary>
        /// Gets or sets the settings for the bot.
        /// </summary>
        public BotSettings BotSettings { get; }

        /// <summary>
        /// Gets the Probe object.
        /// </summary>
        public Probe Probe { get; }

        /// <summary>
        /// Gets the collection of hotspots.
        /// </summary>
        public IEnumerable<Hotspot> Hotspots { get; }

        /// <summary>
        /// Finds the threat for the player character.
        /// </summary>
        // this is broken up into multiple sub-expressions to improve readability and debuggability
        /// <remarks>
        /// \startuml
        /// ObjectManager -> WoWUnit: Player
        /// ObjectManager -> WoWUnit: BotFriend
        /// ObjectManager -> WoWUnit: Units
        /// WoWUnit -> ObjectManager: Filter Units
        /// ObjectManager -> WoWUnit: PotentialThreats
        /// WoWUnit -> ObjectManager: Check if any PotentialThreats
        /// ObjectManager -> WoWUnit: Return first PotentialThreat
        /// ObjectManager -> WoWUnit: Filter Units for Totems
        /// WoWUnit -> ObjectManager: Check if any Totem Threats
        /// ObjectManager -> WoWUnit: Return first Totem Threat
        /// ObjectManager -> WoWUnit: Filter Units for Stoneclaw Totems
        /// WoWUnit -> ObjectManager: Check if any Stoneclaw Totem Threats
        /// ObjectManager -> WoWUnit: Return first Stoneclaw Totem Threat
        /// \enduml
        /// </remarks>
        public WoWUnit FindThreat()
        {
            var player = ObjectManager.Player;
            var botFriendName = player.BotFriend;
            var botFriend = ObjectManager.Units.Where(u => u.Name == botFriendName).FirstOrDefault();
            var potentialThreats = ObjectManager.Units
                .Where(u =>
                    (botFriend != null && u.Name != botFriendName && u.TargetGuid == botFriend.Guid
                    && u.Guid != botFriend.Guid) || // Required to help npcbots
                    u.TargetGuid == player.Guid ||
                    u.TargetGuid == ObjectManager.Pet?.Guid &&
                    !Probe.BlacklistedMobIds.Contains(u.Guid));

            if (potentialThreats.Any())
                return potentialThreats.First();

            // find totems (these disrupt resting between combat, so kill 'em)
            potentialThreats = ObjectManager.Units
                .Where(u =>
                    u.CreatureType == CreatureType.Totem &&
                    u.Position.DistanceTo(player.Position) <= 20 &&
                    u.UnitReaction == UnitReaction.Hostile);

            if (potentialThreats.Any())
                return potentialThreats.First();

            // find stoneclaw totems? for some reason the above will not find these.
            potentialThreats = ObjectManager.Units
                .Where(u =>
                    u.Position.DistanceTo(player.Position) < 10 &&
                    Convert.ToBoolean(ObjectManager.Units.FirstOrDefault(ou => ou.Guid == u.TargetGuid)?.Name?.Contains("Stoneclaw Totem")) &&
                    u.IsInCombat);

            if (potentialThreats.Any())
                return potentialThreats.First();

            return null;
        }

        /// <summary>
        /// Finds the closest target for the player.
        /// </summary>
        // this is broken up into multiple sub-expressions to improve readability and debuggability
        /// <remarks>
        /// \startuml
        /// ObjectManager -> WoWUnit: Player
        /// ObjectManager -> WoWUnit: FindThreat()
        /// alt threat is not null
        ///   WoWUnit --> ObjectManager: return threat
        /// else threat is null
        ///   ObjectManager -> WoWUnit: MapId
        ///   ObjectManager -> BotSettings: TargetingExcludedNames
        ///   ObjectManager -> ObjectManager: Units
        ///   ObjectManager -> WoWUnit: Health
        ///   ObjectManager -> WoWUnit: IsPet
        ///   ObjectManager -> Probe: BlacklistedMobIds
        ///   ObjectManager -> WoWUnit: CreatureRank
        ///   ObjectManager -> BotSettings: TargetingIncludedNames
        ///   ObjectManager -> BotSettings: TargetingExcludedNames
        ///   ObjectManager -> BotSettings: UnitReactions
        ///   ObjectManager -> WoWUnit: UnitReaction
        ///   ObjectManager -> BotSettings: CreatureTypes
        ///   ObjectManager -> WoWUnit: CreatureType
        ///   ObjectManager -> WoWUnit: Level
        ///   ObjectManager -> BotSettings: LevelRangeMax
        ///   ObjectManager -> BotSettings: LevelRangeMin
        ///   ObjectManager -> WoWUnit: FactionId
        ///   ObjectManager -> WoWUnit: UnitFlags
        ///   ObjectManager -> WoWUnit: targetingCriteria()
        ///   ObjectManager -> WoWUnit: OrderBy DistanceTo Player
        ///   ObjectManager -> WoWUnit: CanAttackTarget()
        ///   WoWUnit --> ObjectManager: return potentialTargets
        /// end
        /// \enduml
        /// </remarks>
        public WoWUnit FindClosestTarget()
        {
            var player = ObjectManager.Player;
            var threat = FindThreat();
            if (threat != null)
                return threat;

            var mapId = ObjectManager.MapId;
            if (!BotSettings.TargetingExcludedNames.Contains(player.BotFriend))
                BotSettings.TargetingExcludedNames += ("|" + player.BotFriend);
            var potentialTargetsList = ObjectManager.Units
                // only consider units that are not null, and whose name and position are not null
                .Where(u => u != null && u.Name != null && u.Position != null)
                // only consider living units whose health is > 0
                .Where(u => u.Health > 0)
                // exclude units that are pets of another unit
                .Where(u => !u.IsPet)
                // only consider units that have not been blacklisted
                .Where(u => !Probe?.BlacklistedMobIds?.Contains(u.Guid) ?? true)
                // exclude elites, unless their names have been explicitly included in the targeting settings
                .Where(u => u.CreatureRank == CreatureRank.Normal || BotSettings.TargetingIncludedNames.Any(n => u.Name != null && u.Name.Contains(n)))
                // if included targets are specified, only consider units part of that list
                .Where(u => string.IsNullOrWhiteSpace(BotSettings.TargetingIncludedNames) || BotSettings.TargetingIncludedNames.Split('|').Any(m => u.Name != null && u.Name.Contains(m)))
                // if excluded targets are specified, do not consider units part of that list
                .Where(u => string.IsNullOrWhiteSpace(BotSettings.TargetingExcludedNames) || !BotSettings.TargetingExcludedNames.Split('|').Any(m => u.Name != null && u.Name.Contains(m)))
                // filter units by unit reactions as specified in targeting settings
                .Where(u => BotSettings.UnitReactions.Count == 0 || BotSettings.UnitReactions.Contains(u.UnitReaction.ToString()))
                // Skip Neutral enemies in BG
                //.Where(u => !((mapId == 30 || mapId == 489 || mapId == 529 || mapId == 559) && u.UnitReaction == UnitReaction.Neutral))
                // Always skip neutral (when above level 12)
                .Where(u => !(player?.Level > 12 && u.UnitReaction == UnitReaction.Neutral))
                // filter units by creature type as specified in targeting settings. also include things like totems and slimes.
                .Where(u => BotSettings.CreatureTypes.Count == 0 || u.CreatureType == CreatureType.Mechanical || (u.CreatureType == CreatureType.Totem && u.Position.DistanceTo(player?.Position) <= 20) || BotSettings.CreatureTypes.Contains(u.CreatureType.ToString()) || oozeNames.Contains(u.Name))
                // filter by the level range specified in targeting settings
                .Where(u => u.Level <= player?.Level + BotSettings.LevelRangeMax && u.Level >= player?.Level - BotSettings.LevelRangeMin)
                // exclude certain factions known to cause targeting issues (like neutral, non attackable NPCs in town)
                .Where(u => u.FactionId != 71 && u.FactionId != 85 && u.FactionId != 474 && u.FactionId != 475 && u.FactionId != 1475)
                // exclude units with the UNIT_FLAG_NON_ATTACKABLE flag
                .Where(u => u.UnitFlags != UnitFlags.UNIT_FLAG_NON_ATTACKABLE)
                // apply bot profile specific targeting criteria
                .Where(u => targetingCriteria(u))
                .ToList();

            var potentialTargets = potentialTargetsList
                .OrderBy(u => u.Position.DistanceTo(player?.Position));

            return potentialTargets.FirstOrDefault(x => CanAttackTarget(x.Guid));
            //return potentialTargets.FirstOrDefault();
        }

        /// <summary>
        /// Determines whether the player can attack the specified target.
        /// </summary>
        /// <param name="targetGuid">The unique identifier of the target.</param>
        /// <returns>True if the player can attack the target; otherwise, false.</returns>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> Player: Get Player
        /// Player -> BlackListedTargets: Check if targetGuid is in BlackListedTargets
        /// alt targetGuid is in BlackListedTargets
        ///   BlackListedTargets --> CanAttackTarget: return false
        /// else targetGuid is not in BlackListedTargets
        ///   BlackListedTargets --> CanAttackTarget: return true
        /// end
        /// \enduml
        /// </remarks>
        private bool CanAttackTarget(ulong targetGuid)
        {
            var player = ObjectManager.Player;
            if (player.BlackListedTargets.Contains(targetGuid))
                return false;
            return true; // Below seems to make client crash when run too often?
            //player.SetTarget(targetGuid);
            //var result = player.LuaCallWithResults($"{{0}} = UnitCanAttack('player', 'target')");

            //if (result.Length > 0 && result[0] == "1")
            //    return true;
            //else
            //{
            //    player.BlackListedNeutralTargets.Add(targetGuid);
            //    return false;
            //}
        }

        /// <summary>
        /// Retrieves a hotspot by its ID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetHotspotById Function" as A
        /// database "Hotspots" as B
        /// A -> B : Request Hotspot with id
        /// B --> A : Return Hotspot
        /// \enduml
        /// </remarks>
        private Hotspot GetHotspotById(int id)
        {
            return Hotspots.FirstOrDefault(h => h != null && h.Id == id);
        }

        /// <summary>
        /// Retrieves the current hotspot based on the logic of the current map.
        /// </summary>
        //public Hotspot GetCurrentHotspot() => BotSettings.GrindingHotspot;
        /// <remarks>
        /// \startuml
        /// ObjectManager -> GetCurrentHotspot: MapId
        /// GetCurrentHotspot -> GetHotspotById: player.IsAlly ? 2 : 1
        /// GetCurrentHotspot -> GetHotspotById: player.IsAlly ? 4 : 3
        /// GetCurrentHotspot -> GetHotspotById: player.IsAlly ? 6 : 5
        /// GetCurrentHotspot -> GetHotspotById: player.IsAlly ? 8 : 7
        /// GetCurrentHotspot -> GetHotspotById: 9
        /// GetCurrentHotspot -> GetHotspotById: 10
        /// GetCurrentHotspot -> GetHotspotById: 11
        /// GetCurrentHotspot -> GetHotspotById: 12
        /// GetCurrentHotspot -> BotSettings: GrindingHotspot
        /// \enduml
        /// </remarks>
        public Hotspot GetCurrentHotspot()
        {
            var player = ObjectManager.Player;
            // Hard-coded logic based on Hotspot Id, default is hotspot from settings
            switch (ObjectManager.MapId)
            {
                case 1:
                    return GetHotspotById(player.IsAlly ? 2 : 1); // Kalimdor
                case 0:
                    return GetHotspotById(player.IsAlly ? 4 : 3); // Eastern Kingdoms
                case 530:
                    return GetHotspotById(player.IsAlly ? 6 : 5); // Outland
                case 571:
                    return GetHotspotById(player.IsAlly ? 8 : 7); // Northrend
                case 489:
                    return GetHotspotById(9); // WSG
                case 529:
                    return GetHotspotById(10); // AB
                case 30:
                    return GetHotspotById(11); // AV
                case 559:
                    return GetHotspotById(12); // Nagrand Arena
                default:
                    return BotSettings.GrindingHotspot; // Default
            }
        }

        /// <summary>
        /// Checks for a travel path and creates travel and move to position states if a path exists.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// CheckForTravelPath -> BotSettings: Get GrindingHotspot
        /// BotSettings --> CheckForTravelPath: Return currentHotspot
        /// CheckForTravelPath -> currentHotspot: Get TravelPath
        /// currentHotspot --> CheckForTravelPath: Return travelPath
        /// CheckForTravelPath -> travelPath: Get Waypoints
        /// travelPath --> CheckForTravelPath: Return waypoints
        /// CheckForTravelPath -> ObjectManager: Get Player Position
        /// ObjectManager --> CheckForTravelPath: Return player position
        /// CheckForTravelPath -> waypoints: OrderBy DistanceTo player position
        /// waypoints --> CheckForTravelPath: Return closestTravelPathWaypoint
        /// CheckForTravelPath -> waypoints: Get Last waypoint
        /// waypoints --> CheckForTravelPath: Return destination
        /// CheckForTravelPath -> closestTravelPathWaypoint: DistanceTo player position
        /// closestTravelPathWaypoint --> CheckForTravelPath: Return distance
        /// CheckForTravelPath -> destination: DistanceTo player position
        /// destination --> CheckForTravelPath: Return distance
        /// CheckForTravelPath -> botStates: Push TravelState
        /// CheckForTravelPath -> botStates: Push MoveToPositionState
        /// CheckForTravelPath -> CheckForTravelPath: CreateRestState
        /// CheckForTravelPath --> botStates: Push RestState
        /// \enduml
        /// </remarks>
        public void CheckForTravelPath(Stack<IBotState> botStates, bool reverse, bool needsToRest = true)
        {
            var currentHotspot = BotSettings.GrindingHotspot;
            var travelPath = currentHotspot?.TravelPath;
            if (travelPath != null)
            {
                Position[] waypoints;
                if (reverse)
                    waypoints = travelPath.Waypoints.Reverse().ToArray();
                else
                    waypoints = travelPath.Waypoints;

                var closestTravelPathWaypoint = waypoints.OrderBy(l => l.DistanceTo(ObjectManager.Player.Position)).First();

                Position destination;
                if (reverse)
                    destination = waypoints.Last();
                else
                    destination = currentHotspot.Waypoints.OrderBy(l => l.DistanceTo(ObjectManager.Player.Position)).First();

                if (closestTravelPathWaypoint.DistanceTo(ObjectManager.Player.Position) < destination.DistanceTo(ObjectManager.Player.Position))
                {
                    var startingIndex = waypoints.ToList().IndexOf(closestTravelPathWaypoint);
                    botStates.Push(new TravelState(botStates, this, waypoints, startingIndex));
                    botStates.Push(new MoveToPositionState(botStates, this, closestTravelPathWaypoint));

                    if (reverse && needsToRest)
                        botStates.Push(CreateRestState(botStates, this));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the person is running errands.
        /// </summary>
        public bool RunningErrands { get; set; }

        /// <summary>
        /// Represents the timer for beeping.
        /// </summary>
        int beepTimer;

        /// <summary>
        /// Updates the player trackers by removing old players and tracking new players.
        /// Checks if the bot is catching heat and triggers warnings or stops the bot accordingly.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> UpdatePlayerTrackers: Get Player
        /// ObjectManager -> UpdatePlayerTrackers: Get Other Players
        /// UpdatePlayerTrackers -> PlayerTrackers: Remove Old Players
        /// UpdatePlayerTrackers -> PlayerTrackers: Start Tracking New Players
        /// UpdatePlayerTrackers -> PlayerTrackers: Check Targeting Status
        /// UpdatePlayerTrackers -> PlayerTrackers: Check Proximity Status
        /// UpdatePlayerTrackers -> DiscordClientWrapper: Send Target Warning Message
        /// UpdatePlayerTrackers -> DiscordClientWrapper: Send Proximity Warning Message
        /// UpdatePlayerTrackers -> Console: Beep
        /// UpdatePlayerTrackers -> DiscordClientWrapper: Send Target Stop Message
        /// UpdatePlayerTrackers -> DiscordClientWrapper: Send Proximity Stop Message
        /// UpdatePlayerTrackers -> Console: Beep
        /// UpdatePlayerTrackers -> Console: Beep (if warning)
        /// \enduml
        /// </remarks>
        public bool UpdatePlayerTrackers()
        {
            var stopBot = false;

            try
            {
                var me = ObjectManager.Player;
                var otherPlayers = ObjectManager.Players.Where(p => p.Name != me.Name && p.Position.DistanceTo(me.Position) < 80);

                // remove old players
                foreach (var player in PlayerTrackers)
                {
                    if (!otherPlayers.Any(p => p.Name == player.Key))
                        PlayerTrackers.Remove(player);
                }

                // start tracking new players
                foreach (var player in otherPlayers)
                {
                    if (!PlayerTrackers.ContainsKey(player.Name))
                        PlayerTrackers.Add(player.Name, new PlayerTracker(Environment.TickCount));

                    if (PlayerTrackers.ContainsKey(player.Name))
                    {
                        if (player.TargetGuid == me.Guid)
                        {
                            if (!PlayerTrackers[player.Name].TargetingMe)
                            {
                                PlayerTrackers[player.Name].FirstTargetedMe = Environment.TickCount;
                                PlayerTrackers[player.Name].TargetingMe = true;
                            }
                        }
                        else
                        {
                            PlayerTrackers[player.Name].TargetingMe = false;
                        }
                    }
                }

                // check if we're catching heat
                foreach (var player in PlayerTrackers)
                {
                    var targetWarningTriggered = Environment.TickCount - player.Value.FirstTargetedMe > BotSettings.TargetingWarningTimer && player.Value.TargetingMe && !player.Value.TargetWarning && BotSettings.UsePlayerTargetingKillswitch;
                    if (targetWarningTriggered)
                    {
                        player.Value.TargetWarning = true;
                        DiscordClientWrapper.SendMessage($"{player.Key} has been targeting {ObjectManager.Player.Name} for over {BotSettings.TargetingWarningTimer}ms.");
                    }

                    var proximityWarningTriggered = Environment.TickCount - player.Value.FirstSeen > BotSettings.ProximityWarningTimer && !player.Value.ProximityWarning && BotSettings.UsePlayerProximityKillswitch;
                    if (proximityWarningTriggered)
                    {
                        player.Value.ProximityWarning = true;
                        DiscordClientWrapper.SendMessage($"{player.Key} has been in range of {ObjectManager.Player.Name} for over {BotSettings.ProximityWarningTimer}ms.");
                    }

                    if (Environment.TickCount - player.Value.FirstTargetedMe > BotSettings.TargetingStopTimer && player.Value.TargetingMe && BotSettings.UsePlayerTargetingKillswitch)
                    {
                        DiscordClientWrapper.SendMessage($"{player.Key} has been targeting {ObjectManager.Player.Name} for over {BotSettings.TargetingStopTimer}ms. Stopping.");
                        Console.Beep(1000, 250);
                        stopBot = true;
                    }

                    if (Environment.TickCount - player.Value.FirstSeen > BotSettings.ProximityStopTimer && BotSettings.UsePlayerProximityKillswitch)
                    {
                        DiscordClientWrapper.SendMessage($"{player.Key} has been in range of {ObjectManager.Player.Name} for over {BotSettings.ProximityStopTimer}ms. Stopping.");
                        Console.Beep(1000, 250);
                        stopBot = true;
                    }

                    if ((player.Value.TargetWarning || player.Value.ProximityWarning) && Environment.TickCount - beepTimer > 2000)
                    {
                        Console.Beep(1000, 250);
                        beepTimer = Environment.TickCount;
                    }
                }
            }
            catch (Exception) { /* swallow it */ }

            return stopBot;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the teleport checker is disabled.
        /// </summary>
        public bool DisableTeleportChecker { get; set; }
    }
}
