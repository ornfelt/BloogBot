// Ported from BloogBot/BloogBot/Game/XYZ.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System.Runtime.InteropServices;

namespace BloogBot.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XYZ
    {
        internal float X;
        internal float Y;
        internal float Z;

        internal XYZ(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
