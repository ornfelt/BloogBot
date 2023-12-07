﻿using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Namespace for the Arcane Mage Bot.
/// </summary>
namespace ArcaneMageBot
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
        /// Updates the player's food and drink items based on the bot settings and current state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Inventory: GetAllItems()
        /// Inventory --> Update: foodItem
        /// Update -> Inventory: GetAllItems()
        /// Inventory --> Update: drinkItem
        /// Update -> player: IsCasting
        /// Update -> player: ManaPercent
        /// Update -> botStates: Pop()
        /// Update -> botStates: Push(new RestState)
        /// Update -> Inventory: GetItemCount(foodItem.ItemId)
        /// Update -> Update: Wait.For("ArcaneMageConjureFood", 3000)
        /// Update -> Update: TryCastSpell(ConjureFood)
        /// Update -> Inventory: GetItemCount(drinkItem.ItemId)
        /// Update -> Update: Wait.For("ArcaneMageConjureDrink", 3000)
        /// Update -> Update: TryCastSpell(ConjureWater)
        /// \enduml
        /// </remarks>
        public void Update()
        {
            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);

            if (player.IsCasting)
                return;

            if (player.ManaPercent < 20)
            {
                botStates.Pop();
                botStates.Push(new RestState(botStates, container));
                return;
            }

            if ((foodItem != null || !player.KnowsSpell(ConjureFood)) && (drinkItem != null || !player.KnowsSpell(ConjureWater)))
            {
                botStates.Pop();

                if (player.ManaPercent <= 80)
                    botStates.Push(new RestState(botStates, container));

                return;
            }

            var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
            if ((foodItem == null || foodCount <= 2) && Wait.For("ArcaneMageConjureFood", 3000))
                TryCastSpell(ConjureFood);

            var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);
            if ((drinkItem == null || drinkCount <= 2) && Wait.For("ArcaneMageConjureDrink", 3000))
                TryCastSpell(ConjureWater);
        }

        /// <summary>
        /// Tries to cast a spell by its name if the spell is ready and the player is not currently casting.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TryCastSpell Method" as T
        /// participant "Player" as P
        /// T -> P: IsSpellReady(name)
        /// alt spell is ready and player is not casting
        /// T -> P: LuaCall("CastSpellByName('name')")
        /// end
        /// \enduml
        /// </remarks>
        void TryCastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting)
                player.LuaCall($"CastSpellByName('{name}')");
        }
    }
}
