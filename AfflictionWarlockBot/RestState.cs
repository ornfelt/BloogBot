using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes and interfaces related to the Affliction Warlock bot.
/// </summary>
namespace AfflictionWarlockBot
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
        /// Represents the constant string "Consume Shadows".
        /// </summary>
        const string ConsumeShadows = "Consume Shadows";
        /// <summary>
        /// Represents the constant string "Health Funnel".
        /// </summary>
        const string HealthFunnel = "Health Funnel";

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
        /// Represents a local pet.
        /// </summary>
        LocalPet pet;

        /// <summary>
        /// Initializes a new instance of the RestState class.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            player.SetTarget(player.Guid);

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        /// <summary>
        /// Updates the current state of the bot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ObjectManager -> Update : Pet
        /// Update -> Pet : HealthPercent < 60
        /// Pet -> Update : CanUse(ConsumeShadows)
        /// Pet -> Update : !IsCasting
        /// Pet -> Update : !IsChanneling
        /// Update -> Pet : Cast(ConsumeShadows)
        /// Update -> Player : InCombat || (HealthOk && ManaOk)
        /// Player -> Update : !IsCasting
        /// Player -> Update : !IsChanneling
        /// Update -> Player : Stand()
        /// Update -> Pet : InCombat || PetHealthOk
        /// Pet -> Update : FollowPlayer()
        /// Update -> BotStates : Pop()
        /// Update -> Inventory : GetItemCount(foodItem.ItemId)
        /// Update -> Inventory : GetItemCount(drinkItem.ItemId)
        /// Update -> Container : RunningErrands
        /// Update -> Container : GetCurrentHotspot()
        /// Update -> BotStates : Push(new TravelState())
        /// Update -> BotStates : Push(new MoveToPositionState())
        /// Update -> BotStates : Push(new BuyItemsState())
        /// Update -> BotStates : Push(new SellItemsState())
        /// Update -> BotStates : Push(new MoveToPositionState())
        /// Update -> Container : CheckForTravelPath()
        /// Update -> Container : RunningErrands = true
        /// Update -> BotStates : Push(new SummonVoidwalkerState())
        /// Player -> Update : !IsChanneling
        /// Player -> Update : !IsCasting
        /// Player -> Update : KnowsSpell(HealthFunnel)
        /// Player -> Update : HealthPercent > 30
        /// Update -> Player : LuaCall("CastSpellByName('HealthFunnel')")
        /// Update -> FoodItem : Use()
        /// Update -> DrinkItem : Use()
        /// \enduml
        /// </remarks>
        public void Update()
        {
            pet = ObjectManager.Pet;

            if (pet != null && pet.HealthPercent < 60 && pet.CanUse(ConsumeShadows) && !pet.IsCasting && !pet.IsChanneling)
                pet.Cast(ConsumeShadows);

            if (InCombat || (HealthOk && ManaOk))
            {
                if (!player.IsCasting && !player.IsChanneling)
                    player.Stand();

                if (InCombat || PetHealthOk)
                {
                    pet?.FollowPlayer();
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
                    else
                        botStates.Push(new SummonVoidwalkerState(botStates));
                }
                else
                {
                    if (!player.IsChanneling && !player.IsCasting && player.KnowsSpell(HealthFunnel) && player.HealthPercent > 30)
                        player.LuaCall($"CastSpellByName('{HealthFunnel}')");
                }

                return;
            }

            if (foodItem != null && !player.IsEating && player.HealthPercent < 80 && Wait.For("EatDelay", 500, true))
                foodItem.Use();

            if (drinkItem != null && !player.IsDrinking && player.ManaPercent < 60 && Wait.For("DrinkDelay", 500, true))
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the health is okay based on the conditions:
        /// - If there is no food item available
        /// - If the player's health percentage is greater than or equal to 90
        /// - If the player's health percentage is greater than or equal to 70 and the player is not currently eating
        /// </summary>
        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 70 && !player.IsEating);

        /// <summary>
        /// Checks if the pet's health is okay. Returns true if the pet is null or its health percentage is greater than or equal to 80.
        /// </summary>
        bool PetHealthOk => ObjectManager.Pet == null || ObjectManager.Pet.HealthPercent >= 80;

        /// <summary>
        /// Checks if the player's mana is sufficient for certain conditions.
        /// </summary>
        bool ManaOk => (player.Level < 6 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 55 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the player is currently in combat or if any units are targeting the player or the player's pet.
        /// </summary>
        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid || u.TargetGuid == ObjectManager.Pet?.Guid);
    }
}
