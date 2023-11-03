using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the Enhancement Shaman Bot.
/// </summary>
namespace EnhancementShamanBot
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
        /// The constant string representing the name "Healing Wave".
        /// </summary>
        const string HealingWave = "Healing Wave";

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
        /// Represents a read-only World of Warcraft item used for drinking.
        /// </summary>
        readonly WoWItem drinkItem;

        /// <summary>
        /// Initializes a new instance of the RestState class.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            player.SetTarget(player.Guid);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        /// <summary>
        /// Updates the player's actions based on their current state.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting) return;

            if (InCombat || (HealthOk && ManaOk))
            {
                Wait.RemoveAll();
                player.Stand();
                botStates.Pop();

                var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);
                if (!InCombat && drinkCount == 0 && !container.RunningErrands)
                {
                    var drinkToBuy = 28 - (drinkCount / stackCount);
                    var itemsToBuy = new Dictionary<string, int>
                    {
                        { container.BotSettings.Drink, drinkToBuy }
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

            if (!player.IsDrinking && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();
                if (player.HealthPercent < 70)
                    player.LuaCall($"CastSpellByName('{HealingWave}')");
                if (player.HealthPercent > 70 && player.HealthPercent < 85)
                {
                    if (player.Level >= 40)
                        player.LuaCall($"CastSpellByName('{HealingWave}(Rank 3)')");
                    else
                        player.LuaCall($"CastSpellByName('{HealingWave}(Rank 1)')");
                }
            }

            if (player.Level > 10 && drinkItem != null && !player.IsDrinking && player.ManaPercent < 60)
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the player's health percentage is greater than 90.
        /// </summary>
        bool HealthOk => player.HealthPercent > 90;

        /// <summary>
        /// Checks if the player's mana is sufficient for certain conditions.
        /// </summary>
        bool ManaOk => (player.Level <= 10 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the player is currently in combat or if there are any units targeting the player.
        /// </summary>
        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
