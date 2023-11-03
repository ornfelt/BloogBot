﻿using BloogBot.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

/// <summary>
/// This namespace contains classes for handling SQLite database operations.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a repository that uses SQLite as the underlying database.
    /// </summary>
    /// <summary>
    /// Represents a repository for interacting with a SQLite database.
    /// </summary>
    internal class SqliteRepository : SqlRepository, IRepository
    {
        /// <summary>
        /// The connection string used for connecting to a database.
        /// </summary>
        private string connectionString;

        /// <summary>
        /// Initializes the object by creating a SQLite database file and executing a SQL script to create the necessary tables and schema.
        /// </summary>
        public override void Initialize(string _)
        {
            string strExeFilePath = Assembly.GetExecutingAssembly().Location;
            string strWorkPath = Path.GetDirectoryName(strExeFilePath);

            string dbPath = Path.Combine(strWorkPath, "db.db");
            connectionString = $"Data Source={dbPath};Version=3;New=True;Compress=True;";

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                using (var db = this.NewConnection())
                {
                    string script = File.ReadAllText(Path.Combine(strWorkPath, "SqliteSchema.SQL"));
                    var command = this.NewCommand(script, db);
                    db.Open();
                    command.ExecuteNonQuery();
                    db.Close();
                }
            }
        }

        /// <summary>
        /// Creates a new SQLite connection using the specified connection string.
        /// </summary>
        public override dynamic NewConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        /// <summary>
        /// Creates a new SQLiteCommand object with the specified SQL statement and database connection.
        /// </summary>
        public override dynamic NewCommand(string sql, dynamic db)
        {
            return new SQLiteCommand(sql, db);
        }

        /// <summary>
        /// Adds a blacklisted mob with the specified GUID to the database.
        /// </summary>
        public void AddBlacklistedMob(ulong guid)
        {
            string sql = $"INSERT INTO BlacklistedMobs (Guid) VALUES ('{guid}');";

            RunSqlQuery(sql);
        }

        /// <summary>
        /// Adds a new hotspot with the specified description and optional parameters.
        /// </summary>
        /// <param name="description">The description of the hotspot.</param>
        /// <param name="zone">The zone of the hotspot. Default is an empty string.</param>
        /// <param name="faction">The faction of the hotspot. Default is an empty string.</param>
        /// <param name="waypointsJson">The JSON representation of the waypoints. Default is an empty string.</param>
        /// <param name="innkeeper">The innkeeper NPC associated with the hotspot. Default is null.</param>
        /// <param name="repairVendor">The repair vendor NPC associated with the hotspot. Default is null.</param>
        /// <param name="ammoVendor">The ammo vendor NPC associated with the hotspot. Default is null.</param>
        /// <param name="minLevel">The minimum level required for the hotspot. Default is 0.</param>
        /// <param name="travelPath">The travel path associated with the hotspot. Default is null.</param>
        /// <param name="safeForGrinding">Specifies if the hotspot is safe for grinding. Default is false.</param>
        /// <param name="waypoints">The array of positions representing the waypoints. Default is null.</param>
        /// <returns>The newly created hotspot.</returns>
        public Hotspot AddHotspot(string description, string zone = "", string faction = "", string waypointsJson = "", Npc innkeeper = null, Npc repairVendor = null, Npc ammoVendor = null, int minLevel = 0, TravelPath travelPath = null, bool safeForGrinding = false, Position[] waypoints = null)
        {
            string insertSql = $"INSERT INTO Hotspots (Zone, Description, Faction, Waypoints, InnkeeperId, RepairVendorId, AmmoVendorId, MinimumLevel, TravelPathId, SafeForGrinding) VALUES ('{zone}', '{description}', '{faction}', '{waypointsJson}', {innkeeper?.Id.ToString() ?? "NULL"}, {repairVendor?.Id.ToString() ?? "NULL"}, {ammoVendor?.Id.ToString() ?? "NULL"}, {minLevel}, {travelPath?.Id.ToString() ?? "NULL"}, {Convert.ToInt32(safeForGrinding)});";
            string selectSql = $"SELECT * FROM Hotspots WHERE Description = '{description}' LIMIT 1;";
            int id;

            RunSqlQuery(insertSql);

            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(selectSql, db);
                var reader = command.ExecuteReader();
                reader.Read();

                id = Convert.ToInt32(reader["Id"]);

                reader.Close();
                db.Close();
            }

            return new Hotspot(id, zone, description, faction, minLevel, waypoints, innkeeper, repairVendor, ammoVendor, travelPath, safeForGrinding);
        }

        /// <summary>
        /// Adds a new NPC to the database with the specified attributes.
        /// </summary>
        public Npc AddNpc(string name, bool isInnkeeper, bool sellsAmmo, bool repairs, bool quest, bool horde, bool alliance, float positionX, float positionY, float positionZ, string zone)
        {
            string insertSql = $"INSERT INTO Npcs (Name, IsInnKeeper, SellsAmmo, Repairs, Quest, Horde, Alliance, PositionX, PositionY, PositionZ, Zone) VALUES ('{name}', {Convert.ToInt32(isInnkeeper)}, {Convert.ToInt32(sellsAmmo)}, {Convert.ToInt32(repairs)}, {Convert.ToInt32(quest)}, {Convert.ToInt32(horde)}, {Convert.ToInt32(alliance)}, {positionX}, {positionY}, {positionZ}, '{zone}');";
            string selectSql = $"SELECT * FROM Npcs WHERE Name = '{name}' LIMIT 1;";

            RunSqlQuery(insertSql);

            using (var db = NewConnection())
            {
                db.Open();
                var command = NewCommand(selectSql, db);
                var reader = command.ExecuteReader();
                reader.Read();

                var id = Convert.ToInt32(reader["Id"]);
                var npc = new Npc(id,
                    name,
                    isInnkeeper,
                    sellsAmmo,
                    repairs,
                    quest,
                    horde,
                    alliance,
                    new Position(positionX, positionY, positionZ),
                    zone);

                reader.Close();
                db.Close();

                return npc;
            }

        }

        /// <summary>
        /// Adds a report signature for a player with the specified name and command ID.
        /// </summary>
        public void AddReportSignature(string playerName, int commandId)
        {
            string sql = $"INSERT INTO ReportSignatures (Player, CommandId) VALUES ('{playerName}', {commandId})";


            using (var db = NewConnection())
            {
                db.Open();
                var command = NewCommand(sql, db);
                command.ExecuteNonQuery();
                db.Close();
            }

        }

        /// <summary>
        /// Adds a new travel path to the database with the specified name and waypoints.
        /// </summary>
        public TravelPath AddTravelPath(string name, string waypointsJson)
        {

            string insertSql = $"INSERT INTO TravelPaths (Name, Waypoints) VALUES ('{name}', '{waypointsJson}');";
            string selectSql = $"SELECT * FROM TravelPaths WHERE Name = '{name}' LIMIT 1;";

            TravelPath travelPath;

            RunSqlQuery(insertSql);


            Position[] waypoints = JsonConvert.DeserializeObject<Position[]>(waypointsJson);

            using (var db = NewConnection())
            {
                db.Open();
                var command = NewCommand(selectSql, db);
                var reader = command.ExecuteReader();
                reader.Read();

                var id = Convert.ToInt32(reader["Id"]);
                travelPath = new TravelPath(id, name, waypoints);

                reader.Close();
                db.Close();

            }
            return travelPath;
        }

        /// <summary>
        /// Checks if a blacklisted mob with the specified GUID exists.
        /// </summary>
        public bool BlacklistedMobExists(ulong guid)
        {
            string sql = $"SELECT Id FROM BlacklistedMobs WHERE Guid = '{guid}' LIMIT 1;";
            return RowExistsSql(sql);
        }

        /// <summary>
        /// Deletes a command from the database based on the provided ID.
        /// </summary>
        public void DeleteCommand(int id)
        {
            string sql = $"DELETE FROM Commands WHERE Id = {id}";

            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(sql, db);
                command.ExecuteNonQuery();

                db.Close();
            }

        }

        /// <summary>
        /// Deletes all commands for a specific player from the database.
        /// </summary>
        public void DeleteCommandsForPlayer(string player)
        {
            string sql = $"DELETE FROM Commands WHERE Player = '{player}'";

            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(sql, db);
                command.ExecuteNonQuery();

                db.Close();
            }

        }

        /// <summary>
        /// Retrieves a list of command models for a specific player.
        /// </summary>
        public IList<CommandModel> GetCommandsForPlayer(string playerName)
        {
            string sql = $"SELECT * FROM Commands WHERE Player = '{playerName}'";
            IList<CommandModel> commands = new List<CommandModel>();

            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(sql, db);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    commands.Add(new CommandModel(
                        Convert.ToInt32(reader["Id"]),
                        Convert.ToString(reader["Command"]),
                        Convert.ToString(reader["Player"]),
                        Convert.ToString(reader["Args"])));
                }

                reader.Close();
                db.Close();

                return commands;
            }

        }

        /// <summary>
        /// Retrieves the latest report signatures by executing SQL queries on the database.
        /// </summary>
        public ReportSummary GetLatestReportSignatures()
        {
            string sql = $"SELECT Id FROM Commands WHERE Command = '!report' ORDER BY Id DESC LIMIT 1";

            using (var db = NewConnection())
            {
                var reportSignatures = new List<ReportSignature>();

                db.Open();

                var command = NewCommand(sql, db);
                var reader = command.ExecuteReader();

                var commandId = -1;

                if (reader.Read())
                    commandId = Convert.ToInt32(reader["Id"]);

                reader.Close();

                if (commandId != -1)
                {
                    var sql1 = $"SELECT * FROM ReportSignatures s WHERE s.CommandId = {commandId}";
                    var command1 = this.NewCommand(sql1, db);
                    var reader1 = command1.ExecuteReader();

                    while (reader1.Read())
                    {
                        reportSignatures.Add(new ReportSignature(
                            Convert.ToInt32(reader1["Id"]),
                            Convert.ToString(reader1["Player"]),
                            Convert.ToInt32(reader1["CommandId"])));
                    }

                    reader1.Close();
                }

                db.Close();
                return new ReportSummary(commandId, reportSignatures);
            }

        }


        /// <summary>
        /// Retrieves a list of blacklisted mob IDs from the database.
        /// </summary>
        public List<ulong> ListBlacklistedMobs()
        {
            List<ulong> mobIds = new List<ulong>();
            string sql = $"SELECT Guid FROM BlacklistedMobs;";

            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(sql, db);
                var reader = command.ExecuteReader();
                while (reader.Read())
                    mobIds.Add(Convert.ToUInt64(reader["Guid"]));

                reader.Close();
                db.Close();

                return mobIds;
            }
        }

        /// <summary>
        /// Checks if the specified value is not null, not DBNull, and not zero.
        /// </summary>
        bool IsNotNullOrZero(dynamic value) => value != null && value.GetType() != typeof(DBNull) && value != 0;

        /// <summary>
        /// Retrieves a list of hotspots from the database.
        /// </summary>
        public List<Hotspot> ListHotspots()
        {
            string sql = @"
                    SELECT
	                    h.Zone, h.Description, h.Faction, h.Waypoints, h.InnkeeperId, h.RepairVendorId, h.AmmoVendorId, h.MinimumLevel, h.TravelPathId, h.SafeForGrinding, h.Id,
	                    i.Name as Innkeeper_Name, i.IsInnkeeper as Innkeeper_IsInnkeeper, i.SellsAmmo as Innkeeper_SellsAmmo, i.Repairs as Innkeeper_Repairs, i.Quest as Innkeeper_Quest, i.Horde as Innkeeper_Horde, i.Alliance as Innkeeper_Alliance, i.PositionX as Innkeeper_PositionX, i.PositionY as Innkeeper_PositionY, i.PositionZ as Innkeeper_PositionZ, i.Zone as Innkeeper_Zone, i.Id as Innkeeper_Id,
	                    a.Name as AmmoVendor_Name, a.IsInnkeeper as AmmoVendor_IsInnkeeper, a.SellsAmmo as AmmoVendor_SellsAmmo, a.Repairs as AmmoVendor_Repairs, a.Quest as AmmoVendor_Quest, a.Horde as AmmoVendor_Horde, a.Alliance as AmmoVendor_Alliance, a.PositionX as AmmoVendor_PositionX, a.PositionY as AmmoVendor_PositionY, a.PositionZ as AmmoVendor_PositionZ, a.Zone as AmmoVendor_Zone, a.Id as AmmoVendor_Id,
	                    r.Name as RepairVendor_Name, r.IsInnkeeper as RepairVendor_IsInnkeeper, r.SellsAmmo as RepairVendor_SellsAmmo, r.Repairs as RepairVendor_Repairs, r.Quest as RepairVendor_Quest, r.Horde as RepairVendor_Horde, r.Alliance as RepairVendor_Alliance, r.PositionX as RepairVendor_PositionX, r.PositionY as RepairVendor_PositionY, r.PositionZ as RepairVendor_PositionZ, r.Zone as RepairVendor_Zone, r.Id as RepairVendor_Id,
                        t.Name as TravelPath_Name, t.Waypoints as TravelPath_Waypoints, t.Id as TravelPath_Id
                    FROM Hotspots h
	                    LEFT JOIN Npcs i ON h.InnkeeperId = i.Id
	                    LEFT JOIN Npcs a ON h.AmmoVendorId = a.Id
	                    LEFT JOIN Npcs r ON h.RepairVendorId = r.Id
                        LEFT JOIN TravelPaths t ON h.TravelPathId = t.Id";

            List<Hotspot> hotspots = new List<Hotspot>();

            using (var db = this.NewConnection())
            {
                db.Open();

                var command = this.NewCommand(sql, db);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var id = Convert.ToInt32(reader["Id"]);
                    var zone = Convert.ToString(reader["Zone"]);
                    var description = Convert.ToString(reader["Description"]);
                    var faction = Convert.ToString(reader["Faction"]);
                    var minLevel = Convert.ToInt32(reader["MinimumLevel"]);
                    var waypointsJson = Convert.ToString(reader["Waypoints"]);
                    var waypoints = JsonConvert.DeserializeObject<Position[]>(waypointsJson);
                    var safeForGrinding = Convert.ToBoolean(reader["SafeForGrinding"]);

                    Npc innkeeper = null;
                    var innkeeperId = reader["InnkeeperId"];
                    if (IsNotNullOrZero(innkeeperId))
                        innkeeper = ParseNpcFromQueryResult(reader, Convert.ToInt32(innkeeperId), "Innkeeper_");

                    Npc repairVendor = null;
                    var repairVendorId = reader["RepairVendorId"];
                    if (IsNotNullOrZero(repairVendorId))
                        repairVendor = ParseNpcFromQueryResult(reader, Convert.ToInt32(repairVendorId), "RepairVendor_");

                    Npc ammoVendor = null;
                    var ammoVendorId = reader["AmmoVendorId"];
                    if (IsNotNullOrZero(ammoVendorId))
                        ammoVendor = ParseNpcFromQueryResult(reader, Convert.ToInt32(ammoVendorId), "AmmoVendor_");

                    TravelPath travelPath = null;
                    var travelPathId = reader["TravelPathId"];
                    if (IsNotNullOrZero(travelPathId))
                        travelPath = ParseTravelPathFromQueryResult(reader, Convert.ToInt32(travelPathId), "TravelPath_");

                    hotspots.Add(new Hotspot(
                        id,
                        zone,
                        description,
                        faction,
                        minLevel,
                        waypoints,
                        innkeeper,
                        repairVendor,
                        ammoVendor,
                        travelPath,
                        safeForGrinding));
                }

                reader.Close();
                db.Close();
            }

            return hotspots;
        }

        /// <summary>
        /// Retrieves a list of all NPCs from the database.
        /// </summary>
        public List<Npc> ListNPCs()
        {
            List<Npc> npcs = new List<Npc>();
            string sql = $"SELECT * FROM Npcs;";

            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(sql, db);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var positionX = (float)Convert.ToDecimal(reader["PositionX"]);
                    var positionY = (float)Convert.ToDecimal(reader["PositionY"]);
                    var positionZ = (float)Convert.ToDecimal(reader["PositionZ"]);
                    npcs.Add(new Npc(
                        Convert.ToInt32(reader["Id"]),
                        Convert.ToString(reader["Name"]),
                        Convert.ToBoolean(reader["IsInnkeeper"]),
                        Convert.ToBoolean(reader["SellsAmmo"]),
                        Convert.ToBoolean(reader["Repairs"]),
                        Convert.ToBoolean(reader["Quest"]),
                        Convert.ToBoolean(reader["Horde"]),
                        Convert.ToBoolean(reader["Alliance"]),
                        new Position(positionX, positionY, positionZ),
                        Convert.ToString(reader["Zone"])));
                }

                reader.Close();
                db.Close();

                return npcs;
            }

        }

        /// <summary>
        /// Retrieves a list of all travel paths from the database.
        /// </summary>
        public List<TravelPath> ListTravelPaths()
        {
            string sql = $"SELECT * FROM TravelPaths ORDER BY Name";

            var travelPaths = new List<TravelPath>();

            using (var db = this.NewConnection())
            {
                db.Open();

                var command = this.NewCommand(sql, db);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var id = Convert.ToInt32(reader["Id"]);
                    var name = Convert.ToString(reader["Name"]);
                    var waypointsJson = Convert.ToString(reader["Waypoints"]);
                    var waypoints = JsonConvert.DeserializeObject<Position[]>(waypointsJson);
                    travelPaths.Add(new TravelPath(id, name, waypoints));
                }

                reader.Close();
                db.Close();

                return travelPaths;
            }

        }

        /// <summary>
        /// Checks if an NPC with the specified name exists in the database.
        /// </summary>
        public bool NpcExists(string name)
        {
            string sql = $"SELECT Id FROM Npcs WHERE Name = '{name}' LIMIT 1;";
            return RowExistsSql(sql);
        }

        /// <summary>
        /// Removes a blacklisted mob with the specified GUID from the database.
        /// </summary>
        public void RemoveBlacklistedMob(ulong guid)
        {
            string sql = $"DELETE FROM BlacklistedMobs WHERE Guid = '{guid}';";
            RunSqlQuery(sql);
        }

        /// <summary>
        /// Checks if a travel path with the specified name exists.
        /// </summary>
        public bool TravelPathExists(string name)
        {
            string sql = $"SELECT Id FROM TravelPaths WHERE Name = '{name}' LIMIT 1";
            return this.RowExistsSql(sql);
        }

    }
}
