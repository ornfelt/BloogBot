using BloogBot.Game.Enums;
using System;

/// <summary>
/// Represents a game object in World of Warcraft.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a game object in World of Warcraft.
    /// </summary>
    /// <summary>
    /// Represents a game object in World of Warcraft.
    /// </summary>
    public class WoWGameObject : WoWObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WoWGameObject"/> class.
        /// </summary>
        internal WoWGameObject(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
        }
    }
}
