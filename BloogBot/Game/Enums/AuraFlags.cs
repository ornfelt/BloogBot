using System;

/// <summary>
/// Contains an enumeration of aura flags used in the game.
/// </summary>
namespace BloogBot.Game.Enums
{
    /// <summary>
    /// Represents the flags for an aura.
    /// </summary>
    [Flags]
    public enum AuraFlags
    {
        Active = 0x80,
        Passive = 0x10, // Check if !Active
        Harmful = 0x20
    }
}
