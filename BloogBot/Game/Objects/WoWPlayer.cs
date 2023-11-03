using BloogBot.Game.Enums;
using System;

/// <summary>
/// Represents a player character in the World of Warcraft game.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a player in the World of Warcraft game.
    /// </summary>
    public class WoWPlayer : WoWUnit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WoWPlayer"/> class.
        /// </summary>
        internal WoWPlayer(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the character is currently eating.
        /// </summary>
        public bool IsEating
        {
            get
            {
                if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    return MemoryManager.ReadInt(Pointer + 0xC70) > 0;
                }
                else
                {
                    return HasBuff("Food");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the character is currently drinking.
        /// </summary>
        public bool IsDrinking
        {
            get
            {
                if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                {
                    return MemoryManager.ReadInt(Pointer + 0xF3C) == 4;
                }
                else
                {
                    return HasBuff("Drink"); ;
                }
            }
        }
    }
}
