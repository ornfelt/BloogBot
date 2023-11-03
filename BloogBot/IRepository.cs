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
        void AddBlacklistedMob(ulong guid);
        /// <summary>
        /// Adds a hotspot with the specified description and optional parameters.
        /// </summary>
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
        Npc AddNpc(string name, bool isInnkeeper, bool sellsAmmo, bool repairs, bool quest, bool horde, bool alliance, float positionX, float positionY, float positionZ, string zone);
        /// <summary>
        /// Adds a report signature for the specified player with the given command ID.
        /// </summary>
        void AddReportSignature(string playerName, int commandId);
        /// <summary>
        /// Adds a travel path with the specified name and waypoints JSON.
        /// </summary>
        TravelPath AddTravelPath(string name, string waypointsJson);
        /// <summary>
        /// Checks if a blacklisted mob with the specified GUID exists.
        /// </summary>
        bool BlacklistedMobExists(ulong guid);
        /// <summary>
        /// Deletes a command with the specified ID.
        /// </summary>
        void DeleteCommand(int id);
        /// <summary>
        /// Deletes all commands for a specific player.
        /// </summary>
        void DeleteCommandsForPlayer(string player);
        /// <summary>
        /// Retrieves a list of command models for a specific player.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <returns>A list of command models associated with the player.</returns>
        IList<CommandModel> GetCommandsForPlayer(string playerName);
        /// <summary>
        /// Retrieves the latest report signatures.
        /// </summary>
        ReportSummary GetLatestReportSignatures();
        /// <summary>
        /// Initializes the object with the specified connection string.
        /// </summary>
        void Initialize(string connectionString);
        /// <summary>
        /// Retrieves a list of blacklisted mobs.
        /// </summary>
        List<ulong> ListBlacklistedMobs();
        /// <summary>
        /// Retrieves a list of hotspots.
        /// </summary>
        List<Hotspot> ListHotspots();
        /// <summary>
        /// Retrieves a list of all non-player characters (NPCs).
        /// </summary>
        List<Npc> ListNPCs();
        /// <summary>
        /// Retrieves a list of travel paths.
        /// </summary>
        List<TravelPath> ListTravelPaths();
        /// <summary>
        /// Checks if an NPC with the specified name exists.
        /// </summary>
        bool NpcExists(string name);
        /// <summary>
        /// Removes a blacklisted mob with the specified GUID.
        /// </summary>
        void RemoveBlacklistedMob(ulong guid);
        /// <summary>
        /// Checks if a row exists in the database based on the provided SQL query.
        /// </summary>
        bool RowExistsSql(string sql);
        /// <summary>
        /// Checks if a travel path with the specified name exists.
        /// </summary>
        bool TravelPathExists(string name);
    }
}