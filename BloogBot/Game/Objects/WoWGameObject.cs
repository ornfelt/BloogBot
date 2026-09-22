// Ported from BloogBot/BloogBot/Game/Objects/WoWGameObject.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.Game.Enums;
using System;

namespace BloogBot.Game.Objects
{
    public class WoWGameObject : WoWObject
    {
        internal WoWGameObject(
            IntPtr pointer,
            ulong guid,
            ObjectType objectType)
            : base(pointer, guid, objectType)
        {
        }
    }
}
