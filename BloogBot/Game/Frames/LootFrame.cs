using BloogBot.Game.Cache;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to handling loot frames in the game.
/// </summary>
namespace BloogBot.Game.Frames
{
    /// <summary>
    /// Represents a frame that displays a list of loot items.
    /// </summary>
    public class LootFrame
    {
        /// <summary>
        /// Gets or sets the list of loot items.
        /// </summary>
        readonly public IList<LootItem> LootItems = new List<LootItem>();

        /// <summary>
        /// Initializes a new instance of the LootFrame class.
        /// </summary>
        public LootFrame()
        {
            var hasCoins = MemoryManager.ReadInt((IntPtr)MemoryAddresses.CoinCountPtr) > 0;
            if (hasCoins)
                LootItems.Add(new LootItem(null, 0, 0, true));
            for (var i = 0; i <= 15; i++)
            {
                var itemId = MemoryManager.ReadInt((IntPtr)(MemoryAddresses.LootFrameItemsBasePtr + i * MemoryAddresses.LootFrameItemOffset));
                if (itemId == 0) break;
                var itemCacheEntry = MemoryManager.ReadItemCacheEntry(Functions.GetItemCacheEntry(itemId));
                var itemCacheInfo = new ItemCacheInfo(itemCacheEntry);
                var lootSlot = hasCoins ? i + 1 : i;
                LootItems.Add(new LootItem(itemCacheInfo, itemId, lootSlot, false));
            }
        }
    }

    /// <summary>
    /// Represents a loot item with information about the item, its ID, loot slot, and whether it is coins.
    /// </summary>
    public class LootItem
    {
        /// <summary>
        /// Initializes a new instance of the LootItem class.
        /// </summary>
        internal LootItem(
                    ItemCacheInfo info,
                    int itemId,
                    int lootSlot,
                    bool isCoins)
        {
            Info = info;
            ItemId = itemId;
            LootSlot = lootSlot;
            IsCoins = isCoins;
        }

        /// <summary>
        /// Gets or sets the loot slot.
        /// </summary>
        internal int LootSlot { get; set; }

        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Gets the information about the item cache.
        /// </summary>
        public ItemCacheInfo Info { get; }

        /// <summary>
        /// Gets a value indicating whether the object is coins.
        /// </summary>
        public bool IsCoins { get; }

        /// <summary>
        /// Calls the LootSlot function from the Functions class with the specified LootSlot parameter.
        /// </summary>
        public void Loot() => Functions.LootSlot(LootSlot);
    }
}
