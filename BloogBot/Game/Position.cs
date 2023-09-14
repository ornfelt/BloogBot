using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BloogBot.Game
{
    public class Position
    {
        //[JsonConstructor]
        public Position(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

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

        public Position(XYZ xyz)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public int ID { get; }
        public string Zone { get; }
        public int MinLevel { get; }
        public int MaxLevel { get; }
        public string Links { get; }
        
        public float DistanceTo(Position position)
        {
            var deltaX = X - position.X;
            var deltaY = Y - position.Y;
            var deltaZ = Z - position.Z;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public float DistanceTo2D(Position position)
        {
            var deltaX = X - position.X;
            var deltaY = Y - position.Y;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public Position GetNormalizedVector()
        {
            var magnitude = Math.Sqrt(X * X + Y * Y + Z * Z);

            return new Position((float)(X / magnitude), (float)(Y / magnitude), (float)(Z / magnitude));
        }

        // Update these to match new constructor (only necessary if playing rogue?)
        public static Position operator -(Position a, Position b) =>
            new Position(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Position operator +(Position a, Position b) =>
            new Position(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Position operator *(Position a, int n) =>
            new Position(a.X * n, a.Y * n, a.Z * n);

        public XYZ ToXYZ() => new XYZ(X, Y, Z);
        
        public override string ToString() => $"X: {Math.Round(X, 2)}, Y: {Math.Round(Y, 2)}, Z: {Math.Round(Z, 2)}";
        public string ToStringFull() => $"ID: {ID}, Zone: {GetZoneName(Int32.Parse(Zone))} ({Zone}), MinLevel: {MinLevel}, MaxLevel: {MaxLevel}, X: {Math.Round(X, 2)}, Y: {Math.Round(Y, 2)}, Z: {Math.Round(Z, 2)}, Links: {Links}";

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
            {130, "Silverpine Forest"}, {139, "Eastern Plaguelands"}, {267, "Hillsbrad Foothills"}
        };
        private static string GetZoneName(int zone)
        {
            return ZoneIdNameDict.ContainsKey(zone) ? ZoneIdNameDict[zone] : "Unknown Zone";
        }
    }
}
