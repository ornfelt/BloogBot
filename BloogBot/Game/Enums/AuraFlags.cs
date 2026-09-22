// Ported from BloogBot/BloogBot/Game/Enums/AuraFlags.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System;

namespace BloogBot.Game.Enums
{
    [Flags]
    public enum AuraFlags
    {
        Active = 0x80,
        Passive = 0x10, // Check if !Active
        Harmful = 0x20
    }
}
