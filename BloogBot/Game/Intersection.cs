// Ported from BloogBot/BloogBot/Game/Intersection.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System.Runtime.InteropServices;

namespace BloogBot.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Intersection
    {
        internal float X;
        internal float Y;
        internal float Z;
        internal float R;
    }
}
