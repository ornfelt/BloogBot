using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the state of the bot when the player character is resting and regenerating health and mana.
/// </summary>
namespace ArcaneMageBot
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
        /// Initializes a new instance of the RestState class.
        /// </summary>
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        /// <summary>
        /// Updates the player's actions based on various conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> InCombat: Check if in combat
        /// InCombat -> Update: Return
        /// Update -> HealthOk: Check if health is ok
        /// HealthOk -> Update: Return
        /// Update -> ManaOk: Check if mana is ok
        /// ManaOk -> Update: Return
        /// Update -> player: Stand
        /// player -> botStates: Pop
        /// botStates -> botStates: Push new BuffSelfState
        /// Update -> player: Check if channeling
        /// player -> Update: Return
        /// Update -> player: Check mana percent and spell readiness
        /// player -> Update: Return
        /// Update -> player: LuaCall to cast spell
        /// player -> Update: Return
        /// Update -> player: Check level, food item, eating status, and health percent
        /// player -> foodItem: Use
        /// Update -> player: Check level, drink item, drinking status, and mana percent
        /// player -> drinkItem: Use
        /// \enduml
        /// </remarks>
        public void Update()
        {
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

            if (player.IsChanneling)
                return;

            if (player.ManaPercent < 20 && player.IsSpellReady(Evocation))
            {
                player.LuaCall($"CastSpellByName('{Evocation}')");
                return;
            }

            if (player.Level > 3 && foodItem != null && !player.IsEating && player.HealthPercent < 80)
                foodItem.Use();

            if (player.Level > 3 && drinkItem != null && !player.IsDrinking && player.ManaPercent < 80)
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the player's health percentage is greater than 90.
        /// </summary>
        bool HealthOk => player.HealthPercent > 90;

        /// <summary>
        /// Checks if the player's mana is sufficient for certain actions.
        /// </summary>
        bool ManaOk => (player.Level < 6 && player.ManaPercent > 60) || player.ManaPercent >= 90 || (player.ManaPercent >= 75 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the player is currently in combat or if there are any units targeting the player.
        /// </summary>
        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
