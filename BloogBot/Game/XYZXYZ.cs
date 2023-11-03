using System.Runtime.InteropServices;

/// <summary>
/// Represents a set of XYZ coordinates in 3D space.
/// </summary>
namespace BloogBot.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XYZXYZ
    {
        /// <summary>
        /// Represents the value of the X1 coordinate.
        /// </summary>
        internal float X1;
        /// <summary>
        /// Represents the Y1 value.
        /// </summary>
        internal float Y1;
        /// <summary>
        /// Represents the internal float variable Z1.
        /// </summary>
        internal float Z1;
        /// <summary>
        /// Represents the X2 value.
        /// </summary>
        internal float X2;
        /// <summary>
        /// Represents the Y2 coordinate.
        /// </summary>
        internal float Y2;
        /// <summary>
        /// Represents the internal float variable Z2.
        /// </summary>
        internal float Z2;

        /// <summary>
        /// Initializes a new instance of the XYZXYZ class with the specified coordinates.
        /// </summary>
        internal XYZXYZ(float x1, float y1, float z1,
                    float x2, float y2, float z2)
        {
            X1 = x1;
            Y1 = y1;
            Z1 = z1;
            X2 = x2;
            Y2 = y2;
            Z2 = z2;
        }
    }
}
