using System;

/// <summary>
/// This namespace contains the enumeration for dynamic flags used in the game.
/// </summary>
namespace BloogBot.Game.Enums
{
    /// <summary>
    /// Represents the dynamic flags for a creature.
    /// </summary>
    [Flags]
    public enum DynamicFlags
    {
        None = 0x0,
        CanBeLooted = 0x1,
        IsMarked = 0x2,
        Tapped = 0x4, // Makes creature name tag appear grey
        TappedByMe = 0x8
    }
}
