using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BloogBot.Game;
using Newtonsoft.Json;

/// <summary>
/// This namespace contains classes for interacting with a SQL database.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// This class represents a SQL repository and provides methods for creating a new connection and initializing the object with a connection string.
    /// </summary>
    /// <summary>
    /// This class represents a SQL repository and provides methods for creating a new connection and initializing the object with a connection string.
    /// </summary>
    public abstract class SqlRepository
    {
        /// <summary>
        /// Creates a new connection.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> :System: : NewConnection()
        /// \enduml
        /// </remarks>
        public abstract dynamic NewConnection();

        /// <summary>
        /// Creates a new command object for executing the specified SQL statement using the provided database connection.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "NewCommand Method" as M
        /// C -> M: NewCommand(sql, db)
        /// M --> C: Return dynamic
        /// \enduml
        /// </remarks>
        public abstract dynamic NewCommand(string sql, dynamic db);

        /// <summary>
        /// Initializes the object with the specified connection string.
        /// </summary>
        /// <remarks>
        /// \startuml
        ///  participant "Caller" as A
        ///  participant "Database" as B
        ///  A -> B : Initialize(connectionString)
        /// \enduml
        /// </remarks>
        public abstract void Initialize(string connectionString);

        /// <summary>
        /// Runs a SQL query and executes it against the database.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// RunSqlQuery -> NewConnection : Create new connection
        /// NewConnection --> RunSqlQuery : Return db
        /// RunSqlQuery -> db : Open
        /// RunSqlQuery -> NewCommand : Create new command
        /// NewCommand --> RunSqlQuery : Return command
        /// RunSqlQuery -> command : Prepare
        /// RunSqlQuery -> command : ExecuteNonQuery
        /// RunSqlQuery -> db : Close
        /// \enduml
        /// </remarks>
        public void RunSqlQuery(string sql)
        {
            using (var db = NewConnection())
            {
                db.Open();

                var command = NewCommand(sql, db);
                command.Prepare();
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        /// <summary>
        /// Checks if a row exists in the database based on the provided SQL query.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "RowExistsSql Method" as Method
        /// participant "Database Connection" as DB
        /// participant "SQL Command" as Command
        /// 
        /// Method -> DB: Open Connection
        /// Method -> Command: Create Command with SQL
        /// Command -> DB: Execute Reader
        /// DB --> Command: Return if has rows
        /// Method -> DB: Close Connection
        /// Method --> : Return if rows exist
        /// \enduml
        /// </remarks>
        public bool RowExistsSql(string sql)
        {
            using (var db = this.NewConnection())
            {
                db.Open();

                var command = this.NewCommand(sql, db);
                var exists = command.ExecuteReader().HasRows;

                db.Close();

                return exists;
            }
        }
        /// <summary>
        /// Parses an NPC from a query result.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "ParseNpcFromQueryResult()" as P
        /// participant "Position" as Pos
        /// participant "Npc" as N
        /// 
        /// P -> Pos: Create new Position
        /// activate Pos
        /// Pos --> P: Return Position
        /// deactivate Pos
        /// 
        /// P -> N: Create new Npc
        /// activate N
        /// N --> P: Return Npc
        /// deactivate N
        /// \enduml
        /// </remarks>
        public Npc ParseNpcFromQueryResult(dynamic reader, int id, string prefix)
        {
            var positionX = (float)Convert.ToDecimal(reader[$"{prefix}PositionX"]);
            var positionY = (float)Convert.ToDecimal(reader[$"{prefix}PositionY"]);
            var positionZ = (float)Convert.ToDecimal(reader[$"{prefix}PositionZ"]);
            var position = new Position(positionX, positionY, positionZ);
            return new Npc(
                id,
                Convert.ToString(reader[$"{prefix}Name"]),
                Convert.ToBoolean(reader[$"{prefix}IsInnkeeper"]),
                Convert.ToBoolean(reader[$"{prefix}SellsAmmo"]),
                Convert.ToBoolean(reader[$"{prefix}Repairs"]),
                Convert.ToBoolean(reader[$"{prefix}Quest"]),
                Convert.ToBoolean(reader[$"{prefix}Horde"]),
                Convert.ToBoolean(reader[$"{prefix}Alliance"]),
                position,
                Convert.ToString(reader[$"{prefix}Zone"]));
        }

        /// <summary>
        /// Builds and returns an Npc object based on the provided prefix and reader.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// BuildStartNpc -> Convert : Convert.ToDecimal for positionX
        /// BuildStartNpc -> Convert : Convert.ToDecimal for positionY
        /// BuildStartNpc -> Convert : Convert.ToDecimal for positionZ
        /// BuildStartNpc -> Position : new Position
        /// BuildStartNpc -> Convert : Convert.ToInt32 for NpcId
        /// BuildStartNpc -> Convert : Convert.ToString for NpcName
        /// BuildStartNpc -> Convert : Convert.ToBoolean for NpcIsInnkeeper
        /// BuildStartNpc -> Convert : Convert.ToBoolean for NpcSellsAmmo
        /// BuildStartNpc -> Convert : Convert.ToBoolean for NpcRepairs
        /// BuildStartNpc -> Convert : Convert.ToBoolean for NpcQuest
        /// BuildStartNpc -> Convert : Convert.ToBoolean for NpcHorde
        /// BuildStartNpc -> Convert : Convert.ToBoolean for NpcAlliance
        /// BuildStartNpc -> Convert : Convert.ToString for NpcZone
        /// BuildStartNpc -> Npc : new Npc
        /// \enduml
        /// </remarks>
        public Npc BuildStartNpc(string prefix, dynamic reader)
        {
            var positionX = (float)Convert.ToDecimal(reader[$"{prefix}NpcPositionX"]);
            var positionY = (float)Convert.ToDecimal(reader[$"{prefix}NpcPositionY"]);
            var positionZ = (float)Convert.ToDecimal(reader[$"{prefix}NpcPositionZ"]);
            var position = new Position(positionX, positionY, positionZ);
            return new Npc(
                Convert.ToInt32(reader[$"{prefix}NpcId"]),
                Convert.ToString(reader[$"{prefix}NpcName"]),
                Convert.ToBoolean(reader[$"{prefix}NpcIsInnkeeper"]),
                Convert.ToBoolean(reader[$"{prefix}NpcSellsAmmo"]),
                Convert.ToBoolean(reader[$"{prefix}NpcRepairs"]),
                Convert.ToBoolean(reader[$"{prefix}NpcQuest"]),
                Convert.ToBoolean(reader[$"{prefix}NpcHorde"]),
                Convert.ToBoolean(reader[$"{prefix}NpcAlliance"]),
                position,
                Convert.ToString(reader[$"{prefix}NpcZone"]));
        }

        /// <summary>
        /// Parses a travel path from a query result.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TravelPathParser" as T
        /// participant "JsonConvert" as J
        /// T -> T: ParseTravelPathFromQueryResult(reader, id, prefix)
        /// T -> T: Convert.ToString(reader[prefix + "Name"])
        /// T -> T: Convert.ToString(reader[prefix + "Waypoints"])
        /// T -> J: DeserializeObject<Position[]>(waypointsJson)
        /// T -> T: new TravelPath(id, name, waypoints)
        /// \enduml
        /// </remarks>
        public TravelPath ParseTravelPathFromQueryResult(dynamic reader, int id, string prefix)
        {
            var name = Convert.ToString(reader[$"{prefix}Name"]);
            var waypointsJson = Convert.ToString(reader[$"{prefix}Waypoints"]);
            var waypoints = JsonConvert.DeserializeObject<Position[]>(waypointsJson);

            return new TravelPath(id, name, waypoints);
        }

    }
}
