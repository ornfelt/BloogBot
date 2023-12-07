using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

/// <summary>
/// Represents the state of the bot when the player is resting and regenerating health and mana.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Represents a state of rest for the bot.
    /// </summary>
    /// <summary>
    /// Represents a state of rest for the bot.
    /// </summary>
    class RestState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Evocation".
        /// </summary>
        const string Evocation = "Evocation";

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
        /// Represents a read-only World of Warcraft item for food.
        /// </summary>
        readonly WoWItem foodItem;
        /// <summary>
        /// Represents a read-only World of Warcraft item used for drinking.
        /// </summary>
        readonly WoWItem drinkItem;

        /// <summary>
        /// Initializes a new instance of the RestState class with the specified botStates and container.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => player.FoodNames.Contains(i.Info.Name) || i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => player.DrinkNames.Contains(i.Info.Name) || i.Info.Name == container.BotSettings.Drink);
        }

        /// <summary>
        /// Updates the player's actions based on various conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: IsChanneling
        /// alt InCombat
        ///     Update -> Player: Stand
        ///     Update -> BotStates: Pop
        /// else HealthOk and ManaOk
        ///     Update -> Player: Stand
        ///     Update -> BotStates: Pop
        ///     Update -> BotStates: Push(new BuffSelfState)
        /// else Player's ManaPercent < 20 and IsSpellReady(Evocation)
        ///     Update -> Player: LuaCall("CastSpellByName('Evocation')")
        ///     Update -> Thread: Sleep(200)
        /// else FoodItem != null and !IsEating and HealthPercent < 80
        ///     Update -> FoodItem: Use
        /// else DrinkItem != null and !IsDrinking
        ///     Update -> DrinkItem: Use
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.IsChanneling)
                return;

            if (InCombat)
            {
                player.Stand();
                botStates.Pop();
                return;
            }

            if (HealthOk && ManaOk)
            {
                player.Stand();
                botStates.Pop();
                botStates.Push(new BuffSelfState(botStates, container));
                return;
            }

            if (player.ManaPercent < 20 && player.IsSpellReady(Evocation))
            {
                player.LuaCall($"CastSpellByName('{Evocation}')");
                Thread.Sleep(200);
                return;
            }

            if (foodItem != null && !player.IsEating && player.HealthPercent < 80)
                foodItem.Use();

            if (drinkItem != null && !player.IsDrinking)
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the health is okay based on the conditions:
        /// - If there is no food item available
        /// - If the player's health percentage is greater than or equal to 90
        /// - If the player's health percentage is greater than or equal to 80 and the player is not eating
        /// </summary>
        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 80 && !player.IsEating);

        /// <summary>
        /// Checks if the player's mana is sufficient for action.
        /// </summary>
        bool ManaOk => drinkItem == null || player.ManaPercent >= 90 || (player.ManaPercent >= 80 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the player is currently in combat or if there are any units targeting the player.
        /// </summary>
        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
