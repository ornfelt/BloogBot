// Ported from BloogBot/BloogBot/Game/Objects/WoWPlayer.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.Game.Enums;
using System;

namespace BloogBot.Game.Objects
{
    public class WoWPlayer : WoWUnit
    {
        internal WoWPlayer(
            IntPtr pointer,
            ulong guid,
            ObjectType objectType)
            : base(pointer, guid, objectType)
        {
        }

        public bool IsEating
        {
            get
            {
                return HasBuff("Food") || HasDebuff("Food");
            }
        }

        public bool IsDrinking
        {
            get
            {
                return HasBuff("Drink") || HasDebuff("Drink");
            }
        }
    }
}
