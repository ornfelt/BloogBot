// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Namespace for the BeastMasterHunterBot, which handles various bot states and actions for a Beast Master Hunter character in World of Warcraft.
/// </summary>
namespace BeastMasterHunterBot
{
    /// <summary>
    /// Represents the state of the bot when it is at rest.
    /// </summary>
    /// <summary>
    /// Represents the state of the bot when it is at rest.
    /// </summary>
    // TODO: add in ammo buying/management
    class RestState : IBotState
    {
        /// <summary>
        /// The number of stacks.
        /// </summary>
        const int stackCount = 5;

        /// <summary>
        /// Error message displayed when the user does not have a pet.
        /// </summary>
        const string noPetErrorMessage = "You do not have a pet";

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
        /// Represents a local pet that cannot be modified.
        /// </summary>
        readonly LocalPet pet;
        /// <summary>
        /// Represents a read-only World of Warcraft item for food.
        /// </summary>
        readonly WoWItem foodItem;
        /// <summary>
        /// Represents a read-only World of Warcraft item used for drinking.
        /// </summary>
        readonly WoWItem drinkItem;
        /// <summary>
        /// Represents a read-only World of Warcraft item for pet food.
        /// </summary>
        readonly WoWItem petFood;

        /// <summary>
        /// Initializes a new instance of the RestState class.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            pet = ObjectManager.Pet;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        /// <summary>
        /// Updates the state of the player and performs necessary actions based on certain conditions.
        /// </summary>
        public void Update()
        {
            // Check on your pet
            if (pet != null && !PetHappy && !PetBeingFed)
            {

            }
            if (player.HealthPercent >= 95 ||
                player.HealthPercent >= 80 && !player.IsEating ||
                ObjectManager.Player.IsInCombat ||
                ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid))
            {
                Wait.RemoveAll();
                player.Stand();
                botStates.Pop();

                var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
                var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);

                if (!InCombat && (foodCount == 0 || drinkCount == 0) && !container.RunningErrands)
                {
                    var foodToBuy = 12 - (foodCount / stackCount);
                    var drinkToBuy = 28 - (drinkCount / stackCount);

                    var itemsToBuy = new Dictionary<string, int>();
                    if (foodToBuy > 0)
                        itemsToBuy.Add(container.BotSettings.Food, foodToBuy);
                    if (drinkToBuy > 0)
                        itemsToBuy.Add(container.BotSettings.Drink, drinkToBuy);

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

            if (foodItem != null && !ObjectManager.Player.IsEating && Wait.For("EatDelay", 250, true))
                foodItem.Use();
        }

        /// <summary>
        /// Gets a value indicating whether the current object is in combat.
        /// </summary>
        bool InCombat => ObjectManager.Aggressors.Count() > 0;
        /// <summary>
        /// Checks if the pet's health is okay. Returns true if the pet is null or its health percentage is greater than or equal to 80.
        /// </summary>
        bool PetHealthOk => ObjectManager.Pet == null || ObjectManager.Pet.HealthPercent >= 80;
        /// <summary>
        /// Checks if the pet is happy.
        /// </summary>
        bool PetHappy => pet.IsHappy();
        /// <summary>
        /// Checks if the pet is being fed by checking if it has the "Feed Pet Effect" buff.
        /// </summary>
        bool PetBeingFed => pet.HasBuff("Feed Pet Effect");
    }
}
