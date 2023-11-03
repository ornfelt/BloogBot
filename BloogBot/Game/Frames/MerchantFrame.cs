using BloogBot.Game.Cache;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This class represents the frame for interacting with a merchant in the game.
/// </summary>
namespace BloogBot.Game.Frames
{
    /// <summary>
    /// Represents a frame that displays a list of merchant items.
    /// </summary>
    /// <summary>
    /// Represents a frame that displays a list of merchant items.
    /// </summary>
    public class MerchantFrame
    {
        /// <summary>
        /// The list of merchant items.
        /// </summary>
        readonly IList<MerchantItem> items = new List<MerchantItem>();

        /// <summary>
        /// Gets or sets a value indicating whether the object is ready.
        /// </summary>
        public readonly bool Ready;

        /// <summary>
        /// Initializes a new instance of the MerchantFrame class.
        /// Retrieves information about the merchant's ability to repair items.
        /// Retrieves information about the items available for sale from the merchant.
        /// </summary>
        public MerchantFrame()
        {
            var canRepairResult = Functions.LuaCallWithResult("{0} = CanMerchantRepair()");
            CanRepair = canRepairResult.Length > 0 && canRepairResult[0] == "1";

            var totalVendorItems = MemoryManager.ReadInt((IntPtr)MemoryAddresses.MerchantFrameItemsBasePtr);
            for (var i = 0; i < totalVendorItems; i++)
            {
                var itemId = MemoryManager.ReadInt((IntPtr)(MemoryAddresses.MerchantFrameItemPtr + i * MemoryAddresses.MerchantFrameItemOffset));
                var address = Functions.GetItemCacheEntry(itemId);
                if (address == IntPtr.Zero) continue;
                var entry = MemoryManager.ReadItemCacheEntry(address);
                var info = new ItemCacheInfo(entry);
                items.Add(new MerchantItem(itemId, info));
            }

            Ready = totalVendorItems > 0;
        }

        /// <summary>
        /// Gets a value indicating whether the object can be repaired.
        /// </summary>
        public bool CanRepair { get; }

        /// <summary>
        /// Sells an item by its GUID to an NPC.
        /// </summary>
        public void SellItemByGuid(uint itemCount, ulong npcGuid, ulong itemGuid) =>
                    Functions.SellItemByGuid(itemCount, npcGuid, itemGuid);

        /// <summary>
        /// Buys an item by its name from a vendor.
        /// </summary>
        public void BuyItemByName(ulong vendorGuid, string itemName, int quantity)
        {
            var item = items.Single(i => i.Name == itemName);
            Functions.BuyVendorItem(vendorGuid, item.ItemId, quantity);
        }

        /// <summary>
        /// Repairs all items.
        /// </summary>
        public void RepairAll() => Functions.LuaCall("RepairAllItems()");

        /// <summary>
        /// Closes the merchant frame.
        /// </summary>
        public void CloseMerchantFrame() => Functions.LuaCall("CloseMerchant()");
    }

    /// <summary>
    /// Represents a merchant item with an item ID and cache information.
    /// </summary>
    public class MerchantItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MerchantItem"/> class.
        /// </summary>
        internal MerchantItem(int itemId, ItemCacheInfo info)
        {
            ItemId = itemId;
            Info = info;
        }

        /// <summary>
        /// Gets the item ID.
        /// </summary>
        public int ItemId { get; }

        /// <summary>
        /// Gets the name from the Info object.
        /// </summary>
        public string Name => Info.Name;

        /// <summary>
        /// Gets the information about the item cache.
        /// </summary>
        public ItemCacheInfo Info { get; }
    }
}
