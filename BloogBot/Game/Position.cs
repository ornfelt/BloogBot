﻿using Newtonsoft.Json;
using System;

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
        public Position(float x, float y, float z, int id, string zone, int minlevel, string links)
        {
            X = x;
            Y = y;
            Z = z;
            ID = id;
            Zone = zone;
            MinLevel = minlevel;
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
        public string ToStringFull() => $"X: {Math.Round(X, 2)}, Y: {Math.Round(Y, 2)}, Z: {Math.Round(Z, 2)}, ID: {ID}, Zone: {Zone}, MinLevel: {MinLevel}, Links: {Links}";
    }
}
