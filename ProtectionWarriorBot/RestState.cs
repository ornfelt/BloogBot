using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the implementation of the RestState class, which represents the state of the Protection Warrior Bot when the player character is resting.
/// </summary>
namespace ProtectionWarriorBot
{
    /// <summary>
    /// Represents a state where the bot is resting.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot is resting.
    /// </summary>
    class RestState : IBotState
    {
        /// <summary>
        /// The number of stacks.
        /// </summary>
        const int stackCount = 5;

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
        /// Initializes a new instance of the RestState class.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);
        }

        /// <summary>
        /// Updates the player's actions based on certain conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: Check HealthPercent
        /// Update -> Player: Check IsEating
        /// Update -> ObjectManager: Check Player.IsInCombat
        /// Update -> ObjectManager: Check Units.TargetGuid
        /// Update -> Player: Stand
        /// Update -> BotStates: Pop
        /// Update -> Inventory: GetItemCount
        /// Update -> Container: Check RunningErrands
        /// Update -> Container: GetCurrentHotspot
        /// Update -> BotStates: Push TravelState
        /// Update -> BotStates: Push MoveToPositionState
        /// Update -> BotStates: Push BuyItemsState
        /// Update -> BotStates: Push SellItemsState
        /// Update -> BotStates: Push MoveToPositionState
        /// Update -> Container: CheckForTravelPath
        /// Update -> Container: Set RunningErrands
        /// Update -> FoodItem: Use
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.HealthPercent >= 95 ||
                player.HealthPercent >= 80 && !player.IsEating ||
                ObjectManager.Player.IsInCombat ||
                ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid))
            {
                player.Stand();
                botStates.Pop();

                var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
                if (!InCombat && foodCount == 0 && !container.RunningErrands)
                {
                    var foodToBuy = 28 - (foodCount / stackCount);
                    var itemsToBuy = new Dictionary<string, int>
                    {
                        { container.BotSettings.Food, foodToBuy }
                    };

                    var currentHotspot = container.GetCurrentHotspot();
                    if (currentHotspot.TravelPath != null)
                    {
                        botStates.Push(new TravelState(botStates, container, currentHotspot.TravelPath.Waypoints, 0));
                        botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.TravelPath.Waypoints[0]));
                    }

                    botStates.Push(new BuyItemsState(botStates, currentHotspot.Innkeeper.Name, itemsToBuy));
                    botStates.Push(new SellItemsState(botStates, container, currentHotspot.Innkeeper.Name));
                    botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.Innkeeper.Position));
                    container.CheckForTravelPath(botStates, true, false);
                    container.RunningErrands = true;
                }

                return;
            }

            if (foodItem != null && !ObjectManager.Player.IsEating)
                foodItem.Use();
        }

        /// <summary>
        /// Gets a value indicating whether the player is currently in combat or if there are any units targeting the player.
        /// </summary>
        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
