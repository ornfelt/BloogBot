using BloogBot.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

/// <summary>
/// This namespace contains classes for handling SQL repositories.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a repository for accessing and manipulating data in a SQL database using T-SQL.
    /// </summary>
    /// <summary>
    /// Represents a repository for accessing and manipulating data in a SQL database using T-SQL.
    /// </summary>
    internal class TSqlRepository : SqlRepository, IRepository
    {
        /// <summary>
        /// The connection string used to establish a connection to a database.
        /// </summary>
        string connectionString;

        /// <summary>
        /// Initializes the object with the provided connection string and runs the TSqlSchema.SQL script to check if tables exist before creating them.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Initialize Method" as Initialize
        /// participant "NewConnection Method" as NewConnection
        /// participant "Database" as DB
        /// participant "NewCommand Method" as NewCommand
        /// 
        /// Initialize -> NewConnection: Create new connection
        /// NewConnection --> Initialize: Return connection
        /// Initialize -> NewCommand: Create new command with script
        /// NewCommand --> Initialize: Return command
        /// Initialize -> DB: Open connection
        /// Initialize -> DB: Execute command
        /// DB --> Initialize: Command executed
        /// Initialize -> DB: Close connection
        /// \enduml
        /// </remarks>
        public override void Initialize(string connectionString)
        {
            this.connectionString = connectionString;

            string strExeFilePath = Assembly.GetExecutingAssembly().Location;
            string strWorkPath = Path.GetDirectoryName(strExeFilePath);

            // Run TSqlSchema.SQL regardless - The SQL checks if tables exists before creating the

            using (var db = this.NewConnection())
            {
                string script = File.ReadAllText(Path.Combine(strWorkPath, "TSqlSchema.SQL"));
                var command = this.NewCommand(script, db);
                db.Open();
                command.ExecuteNonQuery();
                db.Close();
            }
        }

        /// <summary>
        /// Creates a new SqlConnection object using the specified connection string.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "NewConnection Method" as M
        /// C -> M: Call NewConnection()
        /// activate M
        /// M -> "SqlConnection" : new(connectionString)
        /// "SqlConnection" --> M : Return new connection
        /// deactivate M
        /// M --> C : Return new connection
        /// \enduml
        /// </remarks>
        public override dynamic NewConnection()
        {
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Creates a new instance of the SqlCommand class with the specified SQL statement and database connection.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "NewCommand Method" as N
        /// participant "SqlCommand" as S
        /// C -> N: NewCommand(sql, db)
        /// N -> S: new SqlCommand(sql, db)
        /// S --> N: Return SqlCommand
        /// N --> C: Return SqlCommand
        /// \enduml
        /// </remarks>
        public override dynamic NewCommand(string sql, dynamic db)
        {
            return new SqlCommand(sql, db);
        }

        /// <summary>
        /// Adds a blacklisted mob with the specified GUID to the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddBlacklistedMob -> RunSqlQuery: RunSqlQuery(sql)
        /// \enduml
        /// </remarks>
        public void AddBlacklistedMob(ulong guid)
        {
            string sql = $"INSERT INTO BlacklistedMobs VALUES ('{guid}');";

            RunSqlQuery(sql);
        }

        /// <summary>
        /// Adds a hotspot with the specified description, zone, faction, waypoints JSON, innkeeper, repair vendor, ammo vendor, minimum level, travel path, safe for grinding, and waypoints.
        /// </summary>
        /// <param name="description">The description of the hotspot.</param>
        /// <param name="zone">The zone of the hotspot.</param>
        /// <param name="faction">The faction of the hotspot.</param>
        /// <param name="waypointsJson">The JSON representation of the waypoints.</param>
        /// <param name="innkeeper">The innkeeper NPC associated with the hotspot.</param>
        /// <param name="repairVendor">The repair vendor NPC associated with the hotspot.</param>
        /// <param name="ammoVendor">The ammo vendor NPC associated with the hotspot.</param>
        /// <param name="minLevel">The minimum level required for the hotspot.</param>
        /// <param name="travelPath">The travel path associated with the hotspot.</param>
        /// <param name="safeForGrinding">A flag indicating if the hotspot is safe for grinding.</param>
        /// <param name="waypoints">The array of positions representing the waypoints.</param>
        /// <returns>The newly created hotspot.</returns>
        /// <remarks>
        /// \startuml
        /// AddHotspot -> "INSERT INTO Hotspots": insertSql
        /// AddHotspot -> "SELECT TOP 1 * FROM Hotspots": selectSql
        /// "SELECT TOP 1 * FROM Hotspots" --> AddHotspot: id
        /// AddHotspot -> Hotspot: new Hotspot(id, zone, description, faction, minLevel, waypoints, innkeeper, repairVendor, ammoVendor, travelPath, safeForGrinding)
        /// \enduml
        /// </remarks>
        public Hotspot AddHotspot(string description, string zone = "", string faction = "", string waypointsJson = "", Npc innkeeper = null, Npc repairVendor = null, Npc ammoVendor = null, int minLevel = 0, TravelPath travelPath = null, bool safeForGrinding = false, Position[] waypoints = null)
        {
            string insertSql = $"INSERT INTO Hotspots VALUES ('{zone}', '{description}', '{faction}', '{waypointsJson}', {innkeeper?.Id.ToString() ?? "NULL"}, {repairVendor?.Id.ToString() ?? "NULL"}, {ammoVendor?.Id.ToString() ?? "NULL"}, {minLevel}, {travelPath?.Id.ToString() ?? "NULL"}, {Convert.ToInt32(safeForGrinding)});";
            string selectSql = $"SELECT TOP 1 * FROM Hotspots WHERE Description = '{description}';";
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
        /// Adds a new NPC to the database with the specified attributes and returns the created NPC object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// AddNpc -> RunSqlQuery: insertSql
        /// AddNpc -> NewConnection: db
        /// db -> AddNpc: Open
        /// AddNpc -> NewCommand: selectSql, db
        /// NewCommand -> AddNpc: command
        /// command -> ExecuteReader: reader
        /// reader -> AddNpc: Read
        /// AddNpc -> Npc: new Npc
        /// Npc -> AddNpc: npc
        /// reader -> AddNpc: Close
        /// db -> AddNpc: Close
        /// AddNpc --> : return npc
        /// \enduml
        /// </remarks>
        public Npc AddNpc(string name, bool isInnkeeper, bool sellsAmmo, bool repairs, bool quest, bool horde, bool alliance, float positionX, float positionY, float positionZ, string zone)
        {
            string insertSql = $"INSERT INTO Npcs VALUES ('{name}', {Convert.ToInt32(isInnkeeper)}, {Convert.ToInt32(sellsAmmo)}, {Convert.ToInt32(repairs)}, {Convert.ToInt32(quest)}, {Convert.ToInt32(horde)}, {Convert.ToInt32(alliance)}, {positionX}, {positionY}, {positionZ}, '{zone}');";
            string selectSql = $"SELECT TOP 1 * FROM Npcs WHERE Name = '{name}';";

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
        /// <remarks>
        /// \startuml
        /// AddReportSignature -> NewConnection : Create new connection
        /// NewConnection --> AddReportSignature : Return new connection
        /// AddReportSignature -> NewConnection : Open connection
        /// AddReportSignature -> NewCommand : Create new command
        /// NewCommand --> AddReportSignature : Return new command
        /// AddReportSignature -> command : ExecuteNonQuery
        /// AddReportSignature -> NewConnection : Close connection
        /// \enduml
        /// </remarks>
        public void AddReportSignature(string playerName, int commandId)
        {
            string sql = $"INSERT INTO ReportSignatures VALUES ('{playerName}', {commandId})";

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
        /// <remarks>
        /// \startuml
        /// AddTravelPath -> RunSqlQuery: insertSql
        /// AddTravelPath -> JsonConvert: DeserializeObject<Position[]>(waypointsJson)
        /// AddTravelPath -> NewConnection: db
        /// db -> AddTravelPath: Open
        /// AddTravelPath -> NewCommand: selectSql, db
        /// NewCommand -> AddTravelPath: command
        /// command -> AddTravelPath: ExecuteReader
        /// ExecuteReader -> AddTravelPath: reader
        /// reader -> AddTravelPath: Read
        /// AddTravelPath -> Convert: ToInt32(reader["Id"])
        /// Convert -> AddTravelPath: id
        /// AddTravelPath -> TravelPath: new TravelPath(id, name, waypoints)
        /// TravelPath -> AddTravelPath: travelPath
        /// reader -> AddTravelPath: Close
        /// db -> AddTravelPath: Close
        /// \enduml
        /// </remarks>
        public TravelPath AddTravelPath(string name, string waypointsJson)
        {

            string insertSql = $"INSERT INTO TravelPaths VALUES ('{name}', '{waypointsJson}');";
            string selectSql = $"SELECT TOP 1 * FROM TravelPaths WHERE Name = '{name}';";

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
        /// <remarks>
        /// \startuml
        /// BlacklistedMobExists -> RowExistsSql: sql
        /// RowExistsSql --> BlacklistedMobExists: bool
        /// \enduml
        /// </remarks>
        public bool BlacklistedMobExists(ulong guid)
        {
            string sql = $"SELECT TOP 1 Id FROM BlacklistedMobs WHERE Guid = '{guid}';";
            return RowExistsSql(sql);
        }

        /// <summary>
        /// Deletes a command from the database based on the provided ID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// DeleteCommand -> Database: NewConnection()
        /// Database --> DeleteCommand: db
        /// DeleteCommand -> Database: Open()
        /// DeleteCommand -> Database: NewCommand(sql, db)
        /// Database --> DeleteCommand: command
        /// DeleteCommand -> Database: ExecuteNonQuery()
        /// DeleteCommand -> Database: Close()
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// DeleteCommandsForPlayer -> NewConnection : Create new connection
        /// NewConnection --> DeleteCommandsForPlayer : Return db connection
        /// DeleteCommandsForPlayer -> db : Open connection
        /// DeleteCommandsForPlayer -> NewCommand : Create new command with sql and db
        /// NewCommand --> DeleteCommandsForPlayer : Return command
        /// DeleteCommandsForPlayer -> command : ExecuteNonQuery
        /// DeleteCommandsForPlayer -> db : Close connection
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// GetCommandsForPlayer -> NewConnection : Create new database connection
        /// NewConnection --> GetCommandsForPlayer : Return database connection
        /// GetCommandsForPlayer -> NewConnection : Open database connection
        /// GetCommandsForPlayer -> NewCommand : Create new command with SQL and database connection
        /// NewCommand --> GetCommandsForPlayer : Return command
        /// GetCommandsForPlayer -> Command : Execute reader
        /// Command --> GetCommandsForPlayer : Return reader
        /// GetCommandsForPlayer -> CommandModel : Create new command model and add to list
        /// GetCommandsForPlayer -> Reader : Close reader
        /// GetCommandsForPlayer -> NewConnection : Close database connection
        /// GetCommandsForPlayer --> : Return list of command models
        /// \enduml
        /// </remarks>
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
        /// Retrieves the latest report signatures by executing a SQL query to retrieve the latest command ID for the "!report" command, 
        /// and then retrieves the corresponding report signatures from the ReportSignatures table using the retrieved command ID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetLatestReportSignatures()" as A
        /// database "Database" as B
        /// A -> B: Execute SQL to get latest command Id
        /// B --> A: Return command Id
        /// A -> A: Check if command Id is not -1
        /// alt command Id is not -1
        ///     A -> B: Execute SQL to get report signatures
        ///     B --> A: Return report signatures
        ///     A -> A: Add report signatures to list
        /// end
        /// A -> A: Return ReportSummary
        /// \enduml
        /// </remarks>
        public ReportSummary GetLatestReportSignatures()
        {
            string sql = $"SELECT TOP 1 Id FROM Commands WHERE Command = '!report' ORDER BY Id DESC";

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
        /// <remarks>
        /// \startuml
        /// ListBlacklistedMobs -> Database: NewConnection()
        /// Database --> ListBlacklistedMobs: db
        /// ListBlacklistedMobs -> db: Open()
        /// ListBlacklistedMobs -> Database: NewCommand(sql, db)
        /// Database --> ListBlacklistedMobs: command
        /// ListBlacklistedMobs -> command: ExecuteReader()
        /// command --> ListBlacklistedMobs: reader
        /// ListBlacklistedMobs -> reader: Read()
        /// activate ListBlacklistedMobs
        /// loop until reader has no more rows
        ///     ListBlacklistedMobs -> reader: Get Guid
        ///     reader --> ListBlacklistedMobs: Guid
        ///     ListBlacklistedMobs -> mobIds: Add(Guid)
        /// end
        /// deactivate ListBlacklistedMobs
        /// ListBlacklistedMobs -> reader: Close()
        /// ListBlacklistedMobs -> db: Close()
        /// ListBlacklistedMobs --> ListBlacklistedMobs: return mobIds
        /// \enduml
        /// </remarks>
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
        /// Retrieves a list of hotspots from the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ListHotspots -> Database: SQL Query
        /// Database --> ListHotspots: Return Query Result
        /// ListHotspots -> JsonConvert: DeserializeObject<Position[]>
        /// JsonConvert --> ListHotspots: Return Deserialized Object
        /// ListHotspots -> ParseNpcFromQueryResult: Parse Innkeeper
        /// ParseNpcFromQueryResult --> ListHotspots: Return Innkeeper
        /// ListHotspots -> ParseNpcFromQueryResult: Parse RepairVendor
        /// ParseNpcFromQueryResult --> ListHotspots: Return RepairVendor
        /// ListHotspots -> ParseNpcFromQueryResult: Parse AmmoVendor
        /// ParseNpcFromQueryResult --> ListHotspots: Return AmmoVendor
        /// ListHotspots -> ParseTravelPathFromQueryResult: Parse TravelPath
        /// ParseTravelPathFromQueryResult --> ListHotspots: Return TravelPath
        /// ListHotspots -> Hotspot: Create new Hotspot
        /// Hotspot --> ListHotspots: Return new Hotspot
        /// ListHotspots -> Database: Close Connection
        /// \enduml
        /// </remarks>
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
                    if (reader["InnkeeperId"].GetType() != typeof(DBNull))
                        innkeeper = ParseNpcFromQueryResult(reader, Convert.ToInt32(reader["InnkeeperId"]), "Innkeeper_");

                    Npc repairVendor = null;
                    if (reader["RepairVendorId"].GetType() != typeof(DBNull))
                        repairVendor = ParseNpcFromQueryResult(reader, Convert.ToInt32(reader["RepairVendorId"]), "RepairVendor_");

                    Npc ammoVendor = null;
                    if (reader["AmmoVendorId"].GetType() != typeof(DBNull))
                        ammoVendor = ParseNpcFromQueryResult(reader, Convert.ToInt32(reader["AmmoVendorId"]), "AmmoVendor_");

                    TravelPath travelPath = null;
                    if (reader["TravelPathId"].GetType() != typeof(DBNull))
                        travelPath = ParseTravelPathFromQueryResult(reader, Convert.ToInt32(reader["TravelPathId"]), "TravelPath_");

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
        /// <remarks>
        /// \startuml
        /// ListNPCs -> Database: NewConnection
        /// Database --> ListNPCs: db
        /// ListNPCs -> db: Open
        /// ListNPCs -> Database: NewCommand(sql, db)
        /// Database --> ListNPCs: command
        /// ListNPCs -> command: ExecuteReader
        /// command --> ListNPCs: reader
        /// activate ListNPCs
        /// loop while reader.Read()
        ///     ListNPCs -> reader: Read
        ///     ListNPCs -> Npc: new Npc
        ///     Npc --> ListNPCs: npc
        ///     ListNPCs -> npcs: Add(npc)
        /// end
        /// deactivate ListNPCs
        /// ListNPCs -> reader: Close
        /// ListNPCs -> db: Close
        /// ListNPCs --> ListNPCs: return npcs
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// ListTravelPaths -> NewConnection: Create new database connection
        /// NewConnection -> Open: Open the database connection
        /// Open -> NewCommand: Create new SQL command
        /// NewCommand -> ExecuteReader: Execute the SQL command
        /// loop
        ///     ExecuteReader -> Read: Read the next row from the result set
        ///     Read -> Convert: Convert the values from the row
        ///     Convert -> DeserializeObject: Deserialize the waypoints JSON
        ///     DeserializeObject -> CreateTravelPath: Create a new TravelPath object
        ///     CreateTravelPath -> AddToList: Add the TravelPath to the list
        /// end
        /// Read --> ExecuteReader: Continue reading until all rows are processed
        /// ExecuteReader --> Close: Close the reader
        /// Close --> NewConnection: Close the database connection
        /// NewConnection --> ListTravelPaths: Return the list of TravelPaths
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// NpcExists -> RowExistsSql: sql
        /// RowExistsSql --> NpcExists: bool
        /// \enduml
        /// </remarks>
        public bool NpcExists(string name)
        {
            string sql = $"SELECT TOP 1 Id FROM Npcs WHERE Name = '{name}';";
            return RowExistsSql(sql);
        }

        /// <summary>
        /// Removes a blacklisted mob with the specified GUID from the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// RemoveBlacklistedMob -> RunSqlQuery: sql
        /// \enduml
        /// </remarks>
        public void RemoveBlacklistedMob(ulong guid)
        {
            string sql = $"DELETE FROM BlacklistedMobs WHERE Guid = '{guid}';";
            RunSqlQuery(sql);
        }

        /// <summary>
        /// Checks if a travel path with the specified name exists in the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> TravelPathExists: name
        /// TravelPathExists -> RowExistsSql: sql
        /// RowExistsSql --> TravelPathExists: bool
        /// TravelPathExists --> User: bool
        /// \enduml
        /// </remarks>
        public bool TravelPathExists(string name)
        {
            string sql = $"SELECT TOP 1 Id FROM TravelPaths WHERE Name = '{name}'";
            return this.RowExistsSql(sql);
        }
    }
}
