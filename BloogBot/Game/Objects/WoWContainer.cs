using BloogBot.Game.Enums;
using System;

/// <summary>
/// Represents a World of Warcraft container object.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a container in the World of Warcraft game.
    /// </summary>
    /// <summary>
    /// Represents a container in the World of Warcraft game.
    /// </summary>
    public class WoWContainer : WoWItem
    {
        /// <summary>
        /// Initializes a new instance of the WoWContainer class with the specified pointer, guid, and objectType.
        /// </summary>
        internal WoWContainer(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
        }

        /// <summary>
        /// Reads the integer value from the memory address obtained by adding the offset of WoWItem_ContainerSlotsOffset to the pointer and returns it.
        /// </summary>
        public int Slots => MemoryManager.ReadInt(IntPtr.Add(Pointer, MemoryAddresses.WoWItem_ContainerSlotsOffset));

        /// <summary>
        /// Retrieves the GUID of the item at the specified slot index.
        /// </summary>
        // slot index starts at 0
        public ulong GetItemGuid(int slot) =>
            MemoryManager.ReadUlong(GetDescriptorPtr() + (MemoryAddresses.WoWItem_ContainerFirstItemOffset + (slot * 8)));
    }
}
