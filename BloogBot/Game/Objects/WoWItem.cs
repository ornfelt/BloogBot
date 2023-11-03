using BloogBot.Game.Cache;
using BloogBot.Game.Enums;
using System;

/// <summary>
/// Represents a World of Warcraft item.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents an item in the World of Warcraft game.
    /// </summary>
    /// <summary>
    /// Represents an item in the World of Warcraft game.
    /// </summary>
    public class WoWItem : WoWObject
    {
        /// <summary>
        /// Initializes a new instance of the WoWItem class with the specified pointer, GUID, and object type.
        /// </summary>
        internal WoWItem(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
            var addr = Functions.GetItemCacheEntry(ItemId);
            if (addr != IntPtr.Zero)
            {
                var itemCacheEntry = MemoryManager.ReadItemCacheEntry(addr);
                Info = new ItemCacheInfo(itemCacheEntry);
            }
        }

        /// <summary>
        /// Gets the item ID by reading the memory at the descriptor pointer offsetted by the WoWItem_ItemIdOffset.
        /// </summary>
        public int ItemId => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWItem_ItemIdOffset);

        /// <summary>
        /// Gets the stack count of the WoW item.
        /// </summary>
        public int StackCount => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWItem_StackCountOffset);

        /// <summary>
        /// Gets the information about the item cache.
        /// </summary>
        public ItemCacheInfo Info { get; }

        /// <summary>
        /// Uses the item pointed to by the pointer.
        /// </summary>
        public void Use() => Functions.UseItem(Pointer);

        /// <summary>
        /// Gets the quality of the item.
        /// </summary>
        public ItemQuality Quality => Info.Quality;

        /// <summary>
        /// Gets the durability of the WoW item.
        /// </summary>
        public int Durability => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWItem_DurabilityOffset);

        /// <summary>
        /// Calculates the durability percentage based on the current durability and maximum durability.
        /// </summary>
        public int DurabilityPercentage => (int)((double)Durability / Info.MaxDurability * 100);
    }
}
