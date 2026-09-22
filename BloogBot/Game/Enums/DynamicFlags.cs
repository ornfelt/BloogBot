// Ported from BloogBot/BloogBot/Game/Enums/DynamicFlags.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System;

namespace BloogBot.Game.Enums
{
    [Flags]
    public enum DynamicFlags
    {
        None =   0x0,
        CanBeLooted = 0x1,
        IsMarked =    0x2,
        Tapped =      0x4, // Makes creature name tag appear grey
        TappedByMe =  0x8
    }
}
