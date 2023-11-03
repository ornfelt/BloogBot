using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This class provides methods for interacting with the player's inventory.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// This class represents the inventory and provides methods for managing items.
    /// </summary>
    /// <summary>
    /// This class represents the inventory and provides methods for managing items.
    /// </summary>
    static public class Inventory
    {
        /// <summary>
        /// Returns the total count of items with the specified name.
        /// </summary>
        static public int GetItemCount(string parItemName)
        {
            var totalCount = 0;
            for (var i = 0; i < 5; i++)
            {
                int slots;
                if (i == 0)
                {
                    slots = 16;
                }
                else
                {
                    var iAdjusted = i - 1;
                    var bag = GetExtraBag(iAdjusted);
                    if (bag == null) continue;
                    slots = bag.Slots;
                }

                for (var k = 0; k <= slots; k++)
                {
                    var item = GetItem(i, k);
                    if (item?.Info.Name == parItemName) totalCount += item.StackCount;
                }
            }
            return totalCount;
        }

        /// <summary>
        /// Returns the total count of items with the specified item ID across all bags and slots.
        /// </summary>
        static public int GetItemCount(int itemId)
        {
            var totalCount = 0;
            for (var i = 0; i < 5; i++)
            {
                int slots;
                if (i == 0)
                {
                    slots = 16;
                }
                else
                {
                    var iAdjusted = i - 1;
                    var bag = GetExtraBag(iAdjusted);
                    if (bag == null) continue;
                    slots = bag.Slots;
                }

                for (var k = 0; k <= slots; k++)
                {
                    var item = GetItem(i, k);
                    if (item?.ItemId == itemId) totalCount += item.StackCount;
                }
            }
            return totalCount;
        }

        /// <summary>
        /// Retrieves all WoWItems from the player's bags and containers.
        /// </summary>
        static public IList<WoWItem> GetAllItems()
        {
            var items = new List<WoWItem>();
            for (int bag = 0; bag < 5; bag++)
            {
                var container = GetExtraBag(bag - 1);
                if (bag != 0 && container == null)
                {
                    continue;
                }

                for (int slot = 0; slot < (bag == 0 ? 16 : container.Slots); slot++)
                {
                    var item = GetItem(bag, slot);
                    if (item == null)
                    {
                        continue;
                    }

                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>
        /// Counts the number of free slots in the player's inventory.
        /// </summary>
        static public int CountFreeSlots(bool parCountSpecialSlots)
        {
            var freeSlots = 0;
            for (var i = 0; i < 16; i++)
            {
                var tmpSlotGuid = ObjectManager.Player.GetBackpackItemGuid(i);
                if (tmpSlotGuid == 0) freeSlots++;
            }
            var bagGuids = new List<ulong>();
            for (var i = 0; i < 4; i++)
                bagGuids.Add(MemoryManager.ReadUlong(IntPtr.Add((IntPtr)MemoryAddresses.LocalPlayerFirstExtraBag, i * 8)));

            var tmpItems = ObjectManager
                .Containers
                .Where(i => i.Slots != 0 && bagGuids.Contains(i.Guid)).ToList();

            foreach (var bag in tmpItems)
            {
                if ((bag.Info.Name.Contains("Quiver") || bag.Info.Name.Contains("Ammo") || bag.Info.Name.Contains("Shot") ||
                     bag.Info.Name.Contains("Herb") || bag.Info.Name.Contains("Soul")) && !parCountSpecialSlots) continue;

                for (var i = 1; i < bag.Slots; i++)
                {
                    var tmpSlotGuid = bag.GetItemGuid(i);
                    if (tmpSlotGuid == 0) freeSlots++;
                }
            }
            return freeSlots;
        }

        /// <summary>
        /// Gets the number of empty bag slots.
        /// </summary>
        static public int EmptyBagSlots
        {
            get
            {
                var bagGuids = new List<ulong>();
                for (var i = 0; i < 4; i++)
                    bagGuids.Add(MemoryManager.ReadUlong(IntPtr.Add((IntPtr)MemoryAddresses.LocalPlayerFirstExtraBag, i * 8)));

                return bagGuids.Count(b => b == 0);
            }
        }

        /// <summary>
        /// Retrieves the bag ID of an item with the specified GUID.
        /// </summary>
        static public int GetBagId(ulong itemGuid)
        {
            var totalCount = 0;
            for (var i = 0; i < 5; i++)
            {
                int slots;
                if (i == 0)
                {
                    slots = 16;
                }
                else
                {
                    var iAdjusted = i - 1;
                    var bag = GetExtraBag(iAdjusted);
                    if (bag == null) continue;
                    slots = bag.Slots;
                }

                for (var k = 0; k < slots; k++)
                {
                    var item = GetItem(i, k);
                    if (item?.Guid == itemGuid) return i;
                }
            }
            return totalCount;
        }

        /// <summary>
        /// Retrieves the slot ID of an item with the specified item GUID.
        /// </summary>
        static public int GetSlotId(ulong itemGuid)
        {
            var totalCount = 0;
            for (var i = 0; i < 5; i++)
            {
                int slots;
                if (i == 0)
                {
                    slots = 16;
                }
                else
                {
                    var iAdjusted = i - 1;
                    var bag = GetExtraBag(iAdjusted);
                    if (bag == null) continue;
                    slots = bag.Slots;
                }

                for (var k = 0; k < slots; k++)
                {
                    var item = GetItem(i, k);
                    if (item?.Guid == itemGuid) return k + 1;
                }
            }
            return totalCount;
        }

        /// <summary>
        /// Retrieves the equipped item in the specified equipment slot.
        /// </summary>
        static public WoWItem GetEquippedItem(EquipSlot slot)
        {
            var guid = ObjectManager.Player.GetEquippedItemGuid(slot);
            if (guid == 0) return null;
            return ObjectManager.Items.FirstOrDefault(i => i.Guid == guid);
        }

        /// <summary>
        /// Retrieves the extra bag at the specified slot.
        /// </summary>
        static WoWContainer GetExtraBag(int parSlot)
        {
            if (parSlot > 3 || parSlot < 0) return null;
            var bagGuid = MemoryManager.ReadUlong(IntPtr.Add((IntPtr)MemoryAddresses.LocalPlayerFirstExtraBag, parSlot * 8));
            return bagGuid == 0 ? null : ObjectManager.Containers.FirstOrDefault(i => i.Guid == bagGuid);
        }

        /// <summary>
        /// Retrieves a WoWItem object based on the specified bag and slot.
        /// </summary>
        static public WoWItem GetItem(int parBag, int parSlot)
        {
            parBag += 1;
            switch (parBag)
            {
                case 1:
                    ulong itemGuid = 0;
                    if (parSlot < 16 && parSlot >= 0)
                        itemGuid = ObjectManager.Player.GetBackpackItemGuid(parSlot);
                    return itemGuid == 0 ? null : ObjectManager.Items.FirstOrDefault(i => i.Guid == itemGuid);

                case 2:
                case 3:
                case 4:
                case 5:
                    var tmpBag = GetExtraBag(parBag - 2);
                    if (tmpBag == null) return null;
                    var tmpItemGuid = tmpBag.GetItemGuid(parSlot);
                    if (tmpItemGuid == 0) return null;
                    return ObjectManager.Items.FirstOrDefault(i => i.Guid == tmpItemGuid);

                default:
                    return null;
            }
        }
    }
}
