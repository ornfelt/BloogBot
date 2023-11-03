using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a state in which the bot equips bags.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot equips bags.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot equips bags.
    /// </summary>
    public class EquipBagsState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;

        /// <summary>
        /// Represents a World of Warcraft item that is a new bag.
        /// </summary>
        WoWItem newBag;

        /// <summary>
        /// Initializes a new instance of the EquipBagsState class.
        /// </summary>
        public EquipBagsState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
        }

        /// <summary>
        /// Updates the state of the bot. If the player is in combat, pops the current state from the stack and returns. 
        /// If a new bag is not assigned or there are no empty bag slots, pops the current state from the stack and pushes a new EquipArmorState with the container as a parameter. 
        /// Otherwise, gets the bag ID and slot ID of the new bag. If the slot ID is 0, sets the new bag to null. 
        /// Calls the Lua function "UseContainerItem" with the bag ID and slot ID as parameters. Sets the new bag to null afterwards.
        /// </summary>
        public void Update()
        {
            if (ObjectManager.Player.IsInCombat)
            {
                botStates.Pop();
                return;
            }

            if (newBag == null)
                newBag = Inventory
                    .GetAllItems()
                    .FirstOrDefault(i => i.Info.ItemClass == ItemClass.Container);

            if (newBag == null || Inventory.EmptyBagSlots == 0)
            {
                botStates.Pop();
                botStates.Push(new EquipArmorState(botStates, container));
                return;
            }

            var bagId = Inventory.GetBagId(newBag.Guid);
            var slotId = Inventory.GetSlotId(newBag.Guid);

            if (slotId == 0)
            {
                newBag = null;
                return;
            }

            ObjectManager.Player.LuaCall($"UseContainerItem({bagId}, {slotId})");
            newBag = null;
        }
    }
}
