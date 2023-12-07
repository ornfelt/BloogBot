using BloogBot.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
/// <summary>
/// This namespace contains the Repository class which handles database operations.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Initializes the Repository class with the specified database type and connection string.
    /// </summary>
    /// <summary>
    /// Initializes the Repository class with the specified database type and connection string.
    /// </summary>
    static public class Repository
    {
        /// <summary>
        /// Represents a static instance of the IRepository interface used for database operations.
        /// </summary>
        static IRepository databaseWrapper;
        /// <summary>
        /// Initializes the database connection based on the specified database type and connection string.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Initialize Method" as A
        /// participant "SqliteRepository" as B
        /// participant "TSqlRepository" as C
        /// participant "DatabaseWrapper" as D
        /// 
        /// A -> A: databaseType.ToLower()
        /// alt sqlite
        ///     A -> B: new SqliteRepository()
        ///     B --> A: databaseWrapper
        /// else mssql
        ///     A -> C: new TSqlRepository()
        ///     C --> A: databaseWrapper
        /// else
        ///     A -> A: throw new NotImplementedException()
        /// end
        /// 
        /// A -> D: Initialize(parConnectionString)
        /// A -> A: CultureInfo.CurrentCulture = CultureInfo.InvariantCulture
        /// \enduml
        /// </remarks>
        static internal void Initialize(string databaseType, string parConnectionString)
        {
            switch (databaseType.ToLower())
            {
                case "sqlite":
                    databaseWrapper = new SqliteRepository();
                    break;
                case "mssql":
                    databaseWrapper = new TSqlRepository();
                    break;
                default:
                    throw new NotImplementedException();

            }

            databaseWrapper.Initialize(parConnectionString);

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Adds a new NPC to the database with the specified attributes.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddNpc -> Encode: name
        /// Encode --> AddNpc: encodedName
        /// AddNpc -> Encode: zone
        /// Encode --> AddNpc: encodedZone
        /// AddNpc -> databaseWrapper: AddNpc(encodedName, isInnkeeper, sellsAmmo, repairs, quest, horde, alliance, positionX, positionY, positionZ, encodedZone)
        /// databaseWrapper --> AddNpc: Npc
        /// \enduml
        /// </remarks>
        static public Npc AddNpc(
                            string name,
                            bool isInnkeeper,
                            bool sellsAmmo,
                            bool repairs,
                            bool quest,
                            bool horde,
                            bool alliance,
                            float positionX,
                            float positionY,
                            float positionZ,
                            string zone)
        {
            var encodedName = Encode(name);
            var encodedZone = Encode(zone);

            return databaseWrapper.AddNpc(encodedName,
             isInnkeeper,
             sellsAmmo,
             repairs,
             quest,
             horde,
             alliance,
             positionX,
             positionY,
             positionZ,
             encodedZone);
        }

        /// <summary>
        /// Checks if an NPC with the specified name exists in the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "NpcExists Method" as N
        /// participant "DatabaseWrapper" as D
        /// N -> N: Encode(name)
        /// N -> D: NpcExists(encodedName)
        /// D --> N: return
        /// \enduml
        /// </remarks>
        static public bool NpcExists(string name)
        {
            var encodedName = Encode(name);
            return databaseWrapper.NpcExists(encodedName);
        }

        /// <summary>
        /// Checks if a blacklisted mob with the specified GUID exists in the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "BlacklistedMobExists Method" as BME
        /// participant "DatabaseWrapper" as DW
        /// BME -> DW: BlacklistedMobExists(guid)
        /// DW --> BME: return
        /// \enduml
        /// </remarks>
        static public bool BlacklistedMobExists(ulong guid)
        {
            return databaseWrapper.BlacklistedMobExists(guid);
        }

        /// <summary>
        /// Adds a travel path with the specified name and waypoints to the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "AddTravelPath Method" as A
        /// participant "DatabaseWrapper" as B
        /// A -> A: Encode(name)
        /// A -> A: JsonConvert.SerializeObject(waypoints)
        /// A -> B: AddTravelPath(encodedName, waypointsJson)
        /// B --> A: return TravelPath
        /// \enduml
        /// </remarks>
        static public TravelPath AddTravelPath(string name, Position[] waypoints)
        {
            var encodedName = Encode(name);

            var waypointsJson = JsonConvert.SerializeObject(waypoints);

            return databaseWrapper.AddTravelPath(encodedName, waypointsJson);

        }

        /// <summary>
        /// Returns a collection of TravelPath objects.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ListTravelPaths -> databaseWrapper: ListTravelPaths()
        /// databaseWrapper --> ListTravelPaths: return IEnumerable<TravelPath>
        /// \enduml
        /// </remarks>
        static public IEnumerable<TravelPath> ListTravelPaths()
        {
            return databaseWrapper.ListTravelPaths();
        }

        /// <summary>
        /// Checks if a travel path with the specified name exists in the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TravelPathExists Method" as TPE
        /// participant "DatabaseWrapper" as DW
        /// TPE -> DW: TravelPathExists(name)
        /// DW --> TPE: return
        /// \enduml
        /// </remarks>
        static public bool TravelPathExists(string name)
        {
            return databaseWrapper.TravelPathExists(name);
        }

        /// <summary>
        /// Adds a hotspot to the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddHotspot -> Encode: zone
        /// Encode --> AddHotspot: encodedZone
        /// AddHotspot -> Encode: description
        /// Encode --> AddHotspot: encodedDescription
        /// AddHotspot -> Encode: faction
        /// Encode --> AddHotspot: encodedFaction
        /// AddHotspot -> JsonConvert: SerializeObject(waypoints)
        /// JsonConvert --> AddHotspot: waypointsJson
        /// AddHotspot -> databaseWrapper: AddHotspot(description, zone, faction, waypointsJson, innkeeper, repairVendor, ammoVendor, minLevel, travelPath, safeForGrinding, waypoints)
        /// databaseWrapper --> AddHotspot: Hotspot
        /// \enduml
        /// </remarks>
        static public Hotspot AddHotspot(
                            string zone,
                            string description,
                            string faction,
                            Position[] waypoints,
                            Npc innkeeper,
                            Npc repairVendor,
                            Npc ammoVendor,
                            int minLevel,
                            TravelPath travelPath,
                            bool safeForGrinding)
        {
            var encodedZone = Encode(zone);
            var encodedDescription = Encode(description);
            var encodedFaction = Encode(faction);

            var waypointsJson = JsonConvert.SerializeObject(waypoints);
            return databaseWrapper.AddHotspot(description, zone, faction, waypointsJson, innkeeper, repairVendor, ammoVendor, minLevel, travelPath, safeForGrinding, waypoints);
        }

        /// <summary>
        /// Retrieves a list of hotspots from the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ListHotspots -> databaseWrapper: ListHotspots()
        /// databaseWrapper --> ListHotspots: Return hotspots
        /// \enduml
        /// </remarks>
        static public IEnumerable<Hotspot> ListHotspots()
        {
            var hotspots = new List<Hotspot>();
            hotspots = databaseWrapper.ListHotspots();
            return hotspots;
        }



        /// <summary>
        /// Adds a blacklisted mob with the specified GUID to the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddBlacklistedMob -> databaseWrapper: AddBlacklistedMob(guid)
        /// \enduml
        /// </remarks>
        static public void AddBlacklistedMob(ulong guid)
        {
            databaseWrapper.AddBlacklistedMob(guid);
        }

        /// <summary>
        /// Returns a list of blacklisted mob IDs.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "DatabaseWrapper" as D
        /// C -> D: ListBlacklistedMobs()
        /// D --> C: return List of Blacklisted Mob Ids
        /// \enduml
        /// </remarks>
        static public IList<ulong> ListBlacklistedMobIds()
        {
            return databaseWrapper.ListBlacklistedMobs();
        }

        /// <summary>
        /// Removes a blacklisted mob from the database based on its GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// RemoveBlacklistedMob -> databaseWrapper: RemoveBlacklistedMob(guid)
        /// \enduml
        /// </remarks>
        static public void RemoveBlacklistedMob(ulong guid)
        {
            databaseWrapper.RemoveBlacklistedMob(guid);
        }


        /// <summary>
        /// Returns a list of Npcs.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ListNpcs -> databaseWrapper: ListNPCs()
        /// databaseWrapper --> ListNpcs: return IList<Npc>
        /// \enduml
        /// </remarks>
        static public IList<Npc> ListNpcs()
        {
            return databaseWrapper.ListNPCs();
        }

        /// <summary>
        /// Retrieves a list of command models for a specific player.
        /// </summary>
        //HERE

        /// <remarks>
        /// \startuml
        /// participant "GetCommandsForPlayer Method" as A
        /// participant "DatabaseWrapper" as B
        /// A -> B: GetCommandsForPlayer(playerName)
        /// B --> A: Return list of CommandModel
        /// \enduml
        /// </remarks>
        static public IList<CommandModel> GetCommandsForPlayer(string playerName)
        {
            return databaseWrapper.GetCommandsForPlayer(playerName);
        }

        /// <summary>
        /// Deletes a command with the specified ID from the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "DeleteCommand Function" as A
        /// participant "DatabaseWrapper" as B
        /// A -> B: DeleteCommand(id)
        /// \enduml
        /// </remarks>
        static public void DeleteCommand(int id)
        {
            databaseWrapper.DeleteCommand(id);
        }

        /// <summary>
        /// Deletes all commands for a specific player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> DeleteCommandsForPlayer: player
        /// DeleteCommandsForPlayer -> databaseWrapper: DeleteCommandsForPlayer(player)
        /// \enduml
        /// </remarks>
        static public void DeleteCommandsForPlayer(string player)
        {
            databaseWrapper.DeleteCommandsForPlayer(player);
        }

        /// <summary>
        /// Retrieves the latest report signatures from the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetLatestReportSignatures Method" as A
        /// participant "DatabaseWrapper" as B
        /// A -> B: GetLatestReportSignatures()
        /// B --> A: ReportSummary
        /// \enduml
        /// </remarks>
        static public ReportSummary GetLatestReportSignatures()
        {
            return databaseWrapper.GetLatestReportSignatures();
        }

        /// <summary>
        /// Adds a report signature for a player with the specified name and command ID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddReportSignature -> databaseWrapper: AddReportSignature(playerName, commandId)
        /// \enduml
        /// </remarks>
        static public void AddReportSignature(string playerName, int commandId)
        {
            databaseWrapper.AddReportSignature(playerName, commandId);
        }

        /// <summary>
        /// Encodes a string by replacing single quotes with double single quotes.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> Encode: value
        /// Encode --> :User: Encoded value
        /// \enduml
        /// </remarks>
        static string Encode(string value) => value.Replace("'", "''");
    }
}
