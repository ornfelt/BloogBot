using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains shared states for handling equipment armor.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// A class that represents the state of equipping armor for a character.
    /// </summary>
    /// <summary>
    /// A class that represents the state of equipping armor for a character.
    /// </summary>
    public class EquipArmorState : IBotState
    {
        /// <summary>
        /// A dictionary that maps each class to its desired armor type.
        /// </summary>
        static readonly IDictionary<Class, ItemSubclass> desiredArmorTypes = new Dictionary<Class, ItemSubclass>
        {
            { Class.Druid, ItemSubclass.Leather },
            { Class.Hunter, ItemSubclass.Mail },
            { Class.Mage, ItemSubclass.Cloth },
            { Class.Paladin, ItemSubclass.Mail },
            { Class.Priest, ItemSubclass.Cloth },
            { Class.Rogue, ItemSubclass.Leather },
            { Class.Shaman, ItemSubclass.Leather },
            { Class.Warlock, ItemSubclass.Cloth },
            { Class.Warrior, ItemSubclass.Mail }
        };

        /// <summary>
        /// List of equipment slots to check.
        /// </summary>
        readonly IList<EquipSlot> slotsToCheck = new List<EquipSlot>
        {
            EquipSlot.Back,
            EquipSlot.Chest,
            EquipSlot.Feet,
            EquipSlot.Hands,
            EquipSlot.Head,
            EquipSlot.Legs,
            EquipSlot.Shoulders,
            EquipSlot.Waist,
            EquipSlot.Wrist
        };

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Represents an empty equipment slot.
        /// </summary>
        EquipSlot? emptySlot;
        /// <summary>
        /// Represents an item to be equipped in the World of Warcraft.
        /// </summary>
        WoWItem itemToEquip;

        /// <summary>
        /// Initializes a new instance of the EquipArmorState class.
        /// </summary>
        public EquipArmorState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the state of the bot. If the player is in combat, pops the current state from the stack and returns. 
        /// If there is no item to equip, checks the inventory for empty slots and attempts to find an item to equip based on desired armor types, equip slot, and required level. 
        /// If no item is found and there are no more slots to check, pops the current state from the stack and pushes a rest state. 
        /// If there is an item to equip and the equip delay has passed, uses the item and equips it if it is of a quality higher than common. 
        /// Resets the empty slot and item to equip variables after equipping the item.
        /// </summary>
        public void Update()
        {
            if (player.IsInCombat)
            {
                botStates.Pop();
                return;
            }

            if (itemToEquip == null)
            {
                foreach (var slot in slotsToCheck)
                {
                    var equippedItem = Inventory.GetEquippedItem(slot);
                    if (equippedItem == null)
                    {
                        emptySlot = slot;
                        break;
                    }
                }

                if (emptySlot != null)
                {
                    slotsToCheck.Remove(emptySlot.Value);

                    itemToEquip = Inventory.GetAllItems()
                        .FirstOrDefault(i =>
                            (i.Info.ItemSubclass == desiredArmorTypes[player.Class] || i.Info.ItemSubclass == ItemSubclass.Cloth && i.Info.EquipSlot == EquipSlot.Back) &&
                            i.Info.EquipSlot.ToString() == emptySlot.ToString() &&
                            i.Info.RequiredLevel <= player.Level
                        );

                    if (itemToEquip == null)
                        emptySlot = null;
                }
                else
                    slotsToCheck.Clear();
            }

            if (itemToEquip == null && slotsToCheck.Count == 0)
            {
                botStates.Pop();
                botStates.Push(container.CreateRestState(botStates, container));
                return;
            }

            if (itemToEquip != null && Wait.For("EquipItemDelay", 500))
            {
                var bagId = Inventory.GetBagId(itemToEquip.Guid);
                var slotId = Inventory.GetSlotId(itemToEquip.Guid);

                player.LuaCall($"UseContainerItem({bagId}, {slotId})");
                if ((int)itemToEquip.Quality > 1)
                    player.LuaCall("EquipPendingItem(0)");
                emptySlot = null;
                itemToEquip = null;
            }
        }
    }
}
