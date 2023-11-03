using BloogBot.AI;
using BloogBot.Game;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace contains classes and methods for handling navigation and pathfinding in the BloogBot application.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a class for handling navigation functionality.
    /// </summary>
    /// <summary>
    /// Represents a class for handling navigation functionality.
    /// </summary>
    public unsafe class Navigation
    {
        /// <summary>
        /// Represents a static random number generator.
        /// </summary>
        private static Random rand = new Random();
        /// <summary>
        /// Loads the specified dynamic-link library (DLL) into the address space of the calling process.
        /// </summary>
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        /// Calculates the path between two points on a map.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate XYZ* CalculatePathDelegate(
                    uint mapId,
                    XYZ start,
                    XYZ end,
                    bool straightPath,
                    out int length);

        /// <summary>
        /// Delegate for freeing memory allocated for an array of XYZ structures.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void FreePathArr(XYZ* pathArr);

        /// <summary>
        /// Delegate used to calculate a path.
        /// </summary>
        static CalculatePathDelegate calculatePath;
        /// <summary>
        /// Represents a static instance of the FreePathArr class.
        /// </summary>
        static FreePathArr freePathArr;

        /// <summary>
        /// Initializes the Navigation class by loading the Navigation.dll library and obtaining function pointers for the CalculatePath and FreePathArr methods.
        /// </summary>
        static Navigation()
        {
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mapsPath = $"{currentFolder}\\Navigation.dll";

            var navProcPtr = LoadLibrary(mapsPath);

            var calculatePathPtr = GetProcAddress(navProcPtr, "CalculatePath");
            calculatePath = Marshal.GetDelegateForFunctionPointer<CalculatePathDelegate>(calculatePathPtr);

            var freePathPtr = GetProcAddress(navProcPtr, "FreePathArr");
            freePathArr = Marshal.GetDelegateForFunctionPointer<FreePathArr>(freePathPtr);
        }

        /// <summary>
        /// Calculates the distance between two positions on a map using a specified path.
        /// </summary>
        static public float DistanceViaPath(uint mapId, Position start, Position end)
        {
            var distance = 0f;
            var path = CalculatePath(mapId, start, end, false);
            for (var i = 0; i < path.Length - 1; i++)
                distance += path[i].DistanceTo(path[i + 1]);
            return distance;
        }

        /// <summary>
        /// Calculates a path between two positions on a map.
        /// </summary>
        static public Position[] CalculatePath(uint mapId, Position start, Position end, bool straightPath)
        {
            var ret = calculatePath(mapId, start.ToXYZ(), end.ToXYZ(), straightPath, out int length);
            var list = new Position[length];
            for (var i = 0; i < length; i++)
            {
                list[i] = new Position(ret[i]);
            }
            freePathArr(ret);
            return list;
        }

        /// <summary>
        /// Retrieves the next waypoint position based on the given map ID, start position, end position, and straight path flag.
        /// </summary>
        static public Position GetNextWaypoint(uint mapId, Position start, Position end, bool straightPath)
        {
            var path = CalculatePath(mapId, start, end, straightPath);
            if (path.Length <= 1)
            {
                //if (!ObjectManager.Player.IsSwimming && !ObjectManager.Player.IsFalling && rand.Next(100) == 1)
                //    Logger.Log($"Problem building path for mapId \"{mapId}\". Returning destination as next waypoint...");
                return end;
            }

            return path[1];
        }

        /// <summary>
        /// Calculates whether point p2 is leftOf, on, or rightOf the line formed by points p0 and p1.
        /// </summary>
        // if p0 and p1 make a line, this method calculates whether point p2 is leftOf, on, or rightOf that line
        static PointComparisonResult IsLeft(Position p0, Position p1, Position p2)
        {
            var result = (p1.X - p0.Y) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);

            if (result < 0)
                return PointComparisonResult.RightOfLine;
            else if (result > 0)
                return PointComparisonResult.LeftOfLine;
            else
                return PointComparisonResult.OnLine;
        }

        /// <summary>
        /// Determines whether a given point is inside a polygon.
        /// </summary>
        static public bool IsPositionInsidePolygon(Position point, Position[] polygon)
        {
            var cn = 0;

            for (var i = 0; i < polygon.Length - 1; i++)
            {
                if (((polygon[i].Y <= point.Y) && (polygon[i + 1].Y > point.Y)) || ((polygon[i].Y > point.Y) && (polygon[i + 1].Y <= point.Y)))
                {
                    var vt = (float)(point.Y - polygon[i].Y) / (polygon[i + 1].Y - polygon[i].Y);
                    if (point.X < polygon[i].X + vt * (polygon[i + 1].X - polygon[i].X))
                        ++cn;
                }
            }

            return cn == 1;
        }
    }

    /// <summary>
    /// Represents the possible results of comparing a point to a line.
    /// </summary>
    enum PointComparisonResult : byte
    {
        LeftOfLine,
        OnLine,
        RightOfLine
    }
}