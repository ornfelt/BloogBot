using System.Runtime.InteropServices;

/// <summary>
/// The BloogBot.Game namespace contains classes and structures related to the game logic and mechanics.
/// </summary>
namespace BloogBot.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XYZ
    {
        /// <summary>
        /// Represents the X coordinate.
        /// </summary>
        internal float X;
        /// <summary>
        /// Represents the Y coordinate.
        /// </summary>
        internal float Y;
        /// <summary>
        /// Represents the Z coordinate.
        /// </summary>
        internal float Z;

        /// <summary>
        /// Initializes a new instance of the XYZ class with the specified x, y, and z values.
        /// </summary>
        internal XYZ(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
