using System.Runtime.InteropServices;

/// <summary>
/// The BloogBot.Game namespace contains classes and structures related to the game logic and mechanics.
/// </summary>
namespace BloogBot.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Intersection
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
        /// Represents the value of R.
        /// </summary>
        internal float R;
    }
}
