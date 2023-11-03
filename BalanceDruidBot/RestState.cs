using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the implementation of the RestState class, which represents the state of the Balance Druid bot when the player is in a resting state.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// Represents the state of the bot when it is resting.
    /// </summary>
    /// <summary>
    /// Represents the state of the bot when it is resting.
    /// </summary>
    class RestState : IBotState
    {
        /// <summary>
        /// The number of stacks.
        /// </summary>
        const int stackCount = 5;

        /// <summary>
        /// Represents the constant string "Regrowth".
        /// </summary>
        const string Regrowth = "Regrowth";
        /// <summary>
        /// The constant string representing "Rejuvenation".
        /// </summary>
        const string Rejuvenation = "Rejuvenation";
        /// <summary>
        /// Represents the constant string "Moonkin Form".
        /// </summary>
        const string MoonkinForm = "Moonkin Form";

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

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        /// <summary>
        /// Updates the player's actions based on various conditions, such as combat status, health and mana levels, and available items.
        /// </summary>
        public void Update()
        {
            if (player.IsCasting)
                return;

            if (InCombat)
            {
                Wait.RemoveAll();
                player.Stand();
                botStates.Pop();
                return;
            }
            if (HealthOk && ManaOk)
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
                else
                    botStates.Push(new BuffSelfState(botStates));
            }

            if (player.HealthPercent < 60 && !player.HasBuff(Regrowth) && Wait.For("SelfHealDelay", 5000, true))
            {
                TryCastSpell(MoonkinForm, player.HasBuff(MoonkinForm));
                TryCastSpell(Regrowth);
            }

            if (player.HealthPercent < 80 && !player.HasBuff(Rejuvenation) && !player.HasBuff(Regrowth) && Wait.For("SelfHealDelay", 5000, true))
            {
                TryCastSpell(MoonkinForm, player.HasBuff(MoonkinForm));
                TryCastSpell(Rejuvenation);
            }

            if (player.Level >= 6 && drinkItem != null && !player.IsDrinking && player.ManaPercent < 60)
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the player's health is above or equal to 81%.
        /// </summary>
        bool HealthOk => player.HealthPercent >= 81;

        /// <summary>
        /// Checks if the player's mana is sufficient for certain conditions.
        /// </summary>
        bool ManaOk => (player.Level < 6 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the current object is in combat.
        /// </summary>
        bool InCombat => ObjectManager.Aggressors.Count() > 0;

        /// <summary>
        /// Tries to cast a spell with the given name if the spell is ready, the player is not currently casting, the player has enough mana, the player is not currently drinking, and the given condition is true.
        /// </summary>
        void TryCastSpell(string name, bool condition = true)
        {
            if (player.IsSpellReady(name) && !player.IsCasting && player.Mana > player.GetManaCost(name) && !player.IsDrinking && condition)
            {
                player.Stand();
                player.LuaCall($"CastSpellByName('{name}',1)");
            }
        }
    }
}
