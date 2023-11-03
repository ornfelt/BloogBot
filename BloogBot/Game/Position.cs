using Newtonsoft.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to game positions.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a position in 3D space with x, y, and z coordinates.
    /// </summary>
    /// <summary>
    /// Represents a position in 3D space with x, y, and z coordinates.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
        //[JsonConstructor]
        public Position(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Initializes a new instance of the Position class with the specified coordinates, ID, zone, level range, and links.
        /// </summary>
        [JsonConstructor]
        public Position(float x, float y, float z, int id, string zone, int minlevel, int maxlevel, string links)
        {
            X = x;
            Y = y;
            Z = z;
            ID = id;
            Zone = zone;
            MinLevel = minlevel;
            MaxLevel = maxlevel;
            Links = links;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        /// <param name="xyz">The XYZ object containing the X, Y, and Z coordinates.</param>
        public Position(XYZ xyz)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
        }

        /// <summary>
        /// Gets the value of X.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the value of the Y property.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets the value of Z.
        /// </summary>
        public float Z { get; }

        /// <summary>
        /// Gets the ID.
        /// </summary>
        public int ID { get; }
        /// <summary>
        /// Gets the zone.
        /// </summary>
        public string Zone { get; }
        /// <summary>
        /// Gets the minimum level.
        /// </summary>
        public int MinLevel { get; }
        /// <summary>
        /// Gets the maximum level.
        /// </summary>
        public int MaxLevel { get; }
        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        public string Links { get; }

        /// <summary>
        /// Calculates the distance between this position and the specified position.
        /// </summary>
        public float DistanceTo(Position position)
        {
            var deltaX = X - position.X;
            var deltaY = Y - position.Y;
            var deltaZ = Z - position.Z;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        /// <summary>
        /// Calculates the 2D distance between this position and the specified position.
        /// </summary>
        public float DistanceTo2D(Position position)
        {
            var deltaX = X - position.X;
            var deltaY = Y - position.Y;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// Returns a normalized vector of the current position.
        /// </summary>
        public Position GetNormalizedVector()
        {
            var magnitude = Math.Sqrt(X * X + Y * Y + Z * Z);

            return new Position((float)(X / magnitude), (float)(Y / magnitude), (float)(Z / magnitude));
        }

        /// <summary>
        /// Subtracts the coordinates of two positions and returns a new position.
        /// </summary>
        // Update these to match new constructor (only necessary if playing rogue?)
        public static Position operator -(Position a, Position b) =>
            new Position(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>
        /// Adds two positions together and returns a new position with the sum of their coordinates.
        /// </summary>
        public static Position operator +(Position a, Position b) =>
                    new Position(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <summary>
        /// Multiplies a Position object by an integer value.
        /// </summary>
        public static Position operator *(Position a, int n) =>
                    new Position(a.X * n, a.Y * n, a.Z * n);

        /// <summary>
        /// Converts the current object to an instance of XYZ.
        /// </summary>
        public XYZ ToXYZ() => new XYZ(X, Y, Z);

        /// <summary>
        /// Returns a string representation of the object with rounded X, Y, and Z values.
        /// </summary>
        public override string ToString() => $"X: {Math.Round(X, 2)}, Y: {Math.Round(Y, 2)}, Z: {Math.Round(Z, 2)}";
        /// <summary>
        /// Returns a string representation of the object including ID, Zone, MinLevel, MaxLevel, X, Y, Z, and Links.
        /// </summary>
        public string ToStringFull() => $"ID: {ID}, Zone: {GetZoneName(Int32.Parse(Zone))} ({Zone}), MinLevel: {MinLevel}, MaxLevel: {MaxLevel}, X: {Math.Round(X, 2)}, Y: {Math.Round(Y, 2)}, Z: {Math.Round(Z, 2)}, Links: {Links}";

        /// <summary>
        /// Dictionary that maps zone IDs to zone names.
        /// </summary>
        private static readonly Dictionary<int, string> ZoneIdNameDict = new Dictionary<int, string>
        {
            {14, "Durotar"}, {15, "Dustwallow Marsh"}, {16, "Azshara"}, {17, "The Barrens"},
            {141, "Teldrassil"}, {148, "Darkshore"}, {215, "Mulgore"}, {331, "Ashenvale"},
            {357, "Feralas"}, {361, "Felwood"}, {400, "Thousand Needles"}, {405, "Desolace"},
            {406, "Stonetalon Mountains"}, {440, "Tanaris"}, {490, "Un'Goro Crater"}, {493, "Moonglade"},
            {618, "Winterspring"}, {718, "Wailing Caverns"}, {1377, "Silithus"}, {1519, "Stormwind City"},
            {1537, "Ironforge"}, {1581, "The Deadmines"}, {1638, "Thunder Bluff"}, {1657, "Darnassus"},
            {1, "Dun Morogh"}, {3, "Badlands"}, {4, "Blasted Lands"}, {8, "Swamp of Sorrows"},
            {10, "Duskwood"}, {11, "Wetlands"}, {12, "Elwynn Forest"}, {25, "Blackrock Mountain"},
            {28, "Western Plaguelands"}, {33, "Stranglethorn Vale"}, {36, "Alterac Mountains"}, {38, "Loch Modan"},
            {40, "Westfall"}, {41, "Deadwind Pass"}, {44, "Redridge Mountains"}, {45, "Arathi Highlands"},
            {46, "Burning Steppes"}, {47, "The Hinterlands"}, {51, "Searing Gorge"}, {85, "Tirisfal Glades"},
            {130, "Silverpine Forest"}, {139, "Eastern Plaguelands"}, {267, "Hillsbrad Foothills"},
            { 65, "Dragonblight" }, { 66, "Zul'Drak" }, { 67, "The Storm Peaks" }, { 210, "Icecrown" }, { 394, "Grizzly Hills" },
            { 495, "Howling Fjord" }, { 2817, "Crystalsong Forest" }, { 3277, "Warsong Gulch" }, { 3483, "Hellfire Peninsula" },
            { 3518, "Nagrand" }, { 3519, "Terokkar Forest" }, { 3520, "Shadowmoon Valley" }, { 3521, "Zangarmarsh" },
            { 3522, "Blade's Edge Mountains" }, { 3523, "Netherstorm" }, { 3524, "Azuremyst Isle" }, { 3525, "Bloodmyst Isle" },
            { 3526, "Ammen Vale" }, { 3527, "Crash Site" }, { 3528, "Silverline Lake" }, { 3529, "Nestlewood Thicket" },
            { 3530, "Shadow Ridge" }, { 3531, "Skulking Row" }, { 3532, "Dawning Lane" }, { 3533, "Ruins of Silvermoon" },
            { 3534, "Feth's Way" }, { 3535, "Hellfire Citadel" }, { 3536, "Thrallmar" }, { 3537, "Borean Tundra" },
            { 3698, "Nagrand Arena" }, { 3703, "Shattrath City" }, { 3711, "Sholazar Basin" },
            { 2597, "Alterac Valley" }, { 3358, "Arathi Basin" }
        };

        /// <summary>
        /// Gets the name of the zone based on the given zone ID.
        /// If the zone ID is found in the ZoneIdNameDict dictionary, the corresponding name is returned.
        /// Otherwise, "Unknown Zone" is returned.
        /// </summary>
        private static string GetZoneName(int zone)
        {
            return ZoneIdNameDict.ContainsKey(zone) ? ZoneIdNameDict[zone] : "Unknown Zone";
        }

        /// <summary>
        /// Retrieves the name of the zone.
        /// </summary>
        public string GetZoneName()
        {
            return GetZoneName(Int32.Parse(Zone));
        }
    }
}
