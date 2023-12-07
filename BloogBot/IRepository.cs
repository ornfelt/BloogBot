using BloogBot.Game;
using System.Collections.Generic;

/// <summary>
/// This namespace contains interfaces and classes for managing repositories.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a repository for managing data related to hotspots and blacklisted mobs.
    /// </summary>
    internal interface IRepository
    {
        /// <summary>
        /// Adds a blacklisted mob with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "AddBlacklistedMob" as A
        /// C -> A: AddBlacklistedMob(guid)
        /// \enduml
        /// </remarks>
        void AddBlacklistedMob(ulong guid);
        /// <summary>
        /// Adds a hotspot with the specified description and optional parameters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Application -> Hotspot: AddHotspot(description, zone, faction, waypointsJson, innkeeper, repairVendor, ammoVendor, minLevel, travelPath, safeForGrinding, waypoints)
        /// Hotspot --> Application: Hotspot
        /// \enduml
        /// </remarks>
        Hotspot AddHotspot(string description, string zone = "", string faction = "", string waypointsJson = "", Npc innkeeper = null, Npc repairVendor = null, Npc ammoVendor = null, int minLevel = 0, TravelPath travelPath = null, bool safeForGrinding = false, Position[] waypoints = null);
        /// <summary>
        /// Adds a new NPC with the specified parameters.
        /// </summary>
        /// <param name="name">The name of the NPC.</param>
        /// <param name="isInnkeeper">Specifies if the NPC is an innkeeper.</param>
        /// <param name="sellsAmmo">Specifies if the NPC sells ammo.</param>
        /// <param name="repairs">Specifies if the NPC can repair items.</param>
        /// <param name="quest">Specifies if the NPC offers quests.</param>
        /// <param name="horde">Specifies if the NPC is affiliated with the Horde faction.</param>
        /// <param name="alliance">Specifies if the NPC is affiliated with the Alliance faction.</param>
        /// <param name="positionX">The X-coordinate of the NPC's position.</param>
        /// <param name="positionY">The Y-coordinate of the NPC's position.</param>
        /// <param name="positionZ">The Z-coordinate of the NPC's position.</param>
        /// <param name="zone">The zone where the NPC is located.</param>
        /// <returns>The newly added NPC.</returns>
        /// <remarks>
        /// \startuml
        /// participant "AddNpc Method" as A
        /// participant "Npc" as B
        /// A -> B: Create Npc
        /// note right: Parameters:\nname, isInnkeeper, sellsAmmo, repairs, quest, horde, alliance, positionX, positionY, positionZ, zone
        /// B --> A: Return Npc
        /// \enduml
        /// </remarks>
        Npc AddNpc(string name, bool isInnkeeper, bool sellsAmmo, bool repairs, bool quest, bool horde, bool alliance, float positionX, float positionY, float positionZ, string zone);
        /// <summary>
        /// Adds a report signature for the specified player with the given command ID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> AddReportSignature: playerName, commandId
        /// \enduml
        /// </remarks>
        void AddReportSignature(string playerName, int commandId);
        /// <summary>
        /// Adds a travel path with the specified name and waypoints JSON.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> TravelPath: AddTravelPath(name, waypointsJson)
        /// \enduml
        /// </remarks>
        TravelPath AddTravelPath(string name, string waypointsJson);
        /// <summary>
        /// Checks if a blacklisted mob with the specified GUID exists.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "BlacklistedMobExists Function" as F
        /// C -> F: BlacklistedMobExists(guid)
        /// F --> C: bool
        /// \enduml
        /// </remarks>
        bool BlacklistedMobExists(ulong guid);
        /// <summary>
        /// Deletes a command with the specified ID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> :System: : DeleteCommand(id)
        /// \enduml
        /// </remarks>
        void DeleteCommand(int id);
        /// <summary>
        /// Deletes all commands for a specific player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> System: DeleteCommandsForPlayer(player)
        /// System --> Database: Delete player commands
        /// Database --> System: Acknowledge deletion
        /// \enduml
        /// </remarks>
        void DeleteCommandsForPlayer(string player);
        /// <summary>
        /// Retrieves a list of command models for a specific player.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <returns>A list of command models associated with the player.</returns>
        /// <remarks>
        /// \startuml
        /// participant "Client" as C
        /// participant "System" as S
        /// C -> S: GetCommandsForPlayer(playerName)
        /// S --> C: Return IList<CommandModel>
        /// \enduml
        /// </remarks>
        IList<CommandModel> GetCommandsForPlayer(string playerName);
        /// <summary>
        /// Retrieves the latest report signatures.
        /// </summary>
        /// <remarks>
        /// \startuml
        ///  :User: -> ReportSummary: GetLatestReportSignatures()
        /// \enduml
        /// </remarks>
        ReportSummary GetLatestReportSignatures();
        /// <summary>
        /// Initializes the object with the specified connection string.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> :System: : Initialize(connectionString)
        /// \enduml
        /// </remarks>
        void Initialize(string connectionString);
        /// <summary>
        /// Retrieves a list of blacklisted mobs.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ListBlacklistedMobs -> ulong : List<ulong>
        /// \enduml
        /// </remarks>
        List<ulong> ListBlacklistedMobs();
        /// <summary>
        /// Retrieves a list of hotspots.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> ListHotspots: Call Method
        /// ListHotspots --> :User: : Return List<Hotspot>
        /// \enduml
        /// </remarks>
        List<Hotspot> ListHotspots();
        /// <summary>
        /// Retrieves a list of all non-player characters (NPCs).
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> :System: : ListNPCs()
        /// :System: --> :User: : Return List<Npc>
        /// \enduml
        /// </remarks>
        List<Npc> ListNPCs();
        /// <summary>
        /// Retrieves a list of travel paths.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> ListTravelPaths: Call method
        /// ListTravelPaths --> :User:: Return List<TravelPath>
        /// \enduml
        /// </remarks>
        List<TravelPath> ListTravelPaths();
        /// <summary>
        /// Checks if an NPC with the specified name exists.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Client" as C
        /// participant "Server" as S
        /// C -> S: NpcExists(name)
        /// S --> C: bool
        /// \enduml
        /// </remarks>
        bool NpcExists(string name);
        /// <summary>
        /// Removes a blacklisted mob with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "RemoveBlacklistedMob" as R
        /// C -> R: RemoveBlacklistedMob(guid)
        /// \enduml
        /// </remarks>
        void RemoveBlacklistedMob(ulong guid);
        /// <summary>
        /// Checks if a row exists in the database based on the provided SQL query.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> System: Call RowExistsSql(sql)
        /// System --> Database: Execute sql
        /// Database --> System: Return result
        /// System --> User: Return result
        /// \enduml
        /// </remarks>
        bool RowExistsSql(string sql);
        /// <summary>
        /// Checks if a travel path with the specified name exists.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> System: TravelPathExists(name)
        /// System --> User: bool
        /// \enduml
        /// </remarks>
        bool TravelPathExists(string name);
    }
}