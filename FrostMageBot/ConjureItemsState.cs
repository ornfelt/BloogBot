using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and interfaces related to the Frost Mage Bot.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Represents a state for conjuring items.
    /// </summary>
    /// <summary>
    /// Represents a state for conjuring items.
    /// </summary>
    class ConjureItemsState : IBotState
    {
        /// <summary>
        /// Represents the constant string value "Conjure Food".
        /// </summary>
        const string ConjureFood = "Conjure Food";
        /// <summary>
        /// The constant string representing the spell "Conjure Water".
        /// </summary>
        const string ConjureWater = "Conjure Water";

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
        /// Represents a World of Warcraft food item.
        /// </summary>
        WoWItem foodItem;
        /// <summary>
        /// Represents a World of Warcraft item that can be consumed as a drink.
        /// </summary>
        WoWItem drinkItem;

        /// <summary>
        /// Initializes a new instance of the ConjureItemsState class.
        /// </summary>
        public ConjureItemsState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's food and drink items based on their inventory and current state.
        /// </summary>
        public void Update()
        {
            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => player.FoodNames.Contains(i.Info.Name) || i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => player.DrinkNames.Contains(i.Info.Name) || i.Info.Name == container.BotSettings.Drink);

            if (player.IsCasting)
                return;

            //player.Stand();

            if (player.ManaPercent < 20)
            {
                botStates.Pop();
                botStates.Push(new RestState(botStates, container));
                return;
            }

            if (Inventory.CountFreeSlots(false) == 0 || (foodItem != null || !player.KnowsSpell(ConjureFood)) && (drinkItem != null || !player.KnowsSpell(ConjureWater)))
            {
                botStates.Pop();

                if (player.ManaPercent <= 70)
                    botStates.Push(new RestState(botStates, container));

                return;
            }

            var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
            if (foodItem == null || foodCount <= 2)
                TryCastSpell(ConjureFood);

            var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);
            if (drinkItem == null || drinkCount <= 2)
                TryCastSpell(ConjureWater);
        }

        /// <summary>
        /// Tries to cast a spell by its name if the spell is ready and the player is not currently casting.
        /// </summary>
        void TryCastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting)
                player.LuaCall($"CastSpellByName('{name}')");
        }
    }
}
