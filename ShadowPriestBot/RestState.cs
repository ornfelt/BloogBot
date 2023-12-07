using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The namespace for the ShadowPriestBot, which handles the behavior of a bot in a rest state.
/// </summary>
namespace ShadowPriestBot
{
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
        /// Represents the constant string "Abolish Disease".
        /// </summary>
        const string AbolishDisease = "Abolish Disease";
        /// <summary>
        /// Represents the constant string "Cure Disease".
        /// </summary>
        const string CureDisease = "Cure Disease";
        /// <summary>
        /// Represents the constant string "Lesser Heal".
        /// </summary>
        const string LesserHeal = "Lesser Heal";
        /// <summary>
        /// Represents a constant string for healing.
        /// </summary>
        const string Heal = "Heal";
        /// <summary>
        /// The constant string representing "Shadowform".
        /// </summary>
        const string ShadowForm = "Shadowform";

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
        /// Updates the player's actions based on their current state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: IsCasting
        /// Update -> Player: KnowsSpell(ShadowForm)
        /// Update -> Player: HasBuff(ShadowForm)
        /// Update -> Player: IsDiseased
        /// Update -> Player: KnowsSpell(AbolishDisease)
        /// Update -> Player: LuaCall(CastSpellByName(AbolishDisease,1))
        /// Update -> Player: KnowsSpell(CureDisease)
        /// Update -> Player: LuaCall(CastSpellByName(CureDisease,2))
        /// Update -> Player: LuaCall(CastSpellByName(ShadowForm))
        /// Update -> Wait: RemoveAll
        /// Update -> Player: Stand
        /// Update -> BotStates: Pop
        /// Update -> Inventory: GetItemCount(drinkItem.ItemId)
        /// Update -> Container: GetCurrentHotspot
        /// Update -> BotStates: Push(TravelState)
        /// Update -> BotStates: Push(MoveToPositionState)
        /// Update -> BotStates: Push(BuyItemsState)
        /// Update -> BotStates: Push(SellItemsState)
        /// Update -> BotStates: Push(MoveToPositionState)
        /// Update -> Container: CheckForTravelPath
        /// Update -> BotStates: Push(BuffSelfState)
        /// Update -> Player: IsDrinking
        /// Update -> Wait: For("HealSelfDelay", 3500, true)
        /// Update -> Player: HealthPercent
        /// Update -> Player: LuaCall(CastSpellByName(ShadowForm))
        /// Update -> Player: KnowsSpell(Heal)
        /// Update -> Player: LuaCall(CastSpellByName(Heal,1))
        /// Update -> Player: LuaCall(CastSpellByName(LesserHeal,1))
        /// Update -> Player: LuaCall(CastSpellByName(LesserHeal,1))
        /// Update -> Player: Level
        /// Update -> Player: IsDrinking
        /// Update -> Player: ManaPercent
        /// Update -> DrinkItem: Use
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.IsCasting) return;

            if (InCombat || (HealthOk && ManaOk))
            {
                if (player.KnowsSpell(ShadowForm) && !player.HasBuff(ShadowForm) && player.IsDiseased)
                {
                    if (player.KnowsSpell(AbolishDisease))
                        player.LuaCall($"CastSpellByName('{AbolishDisease}',1)");
                    else if (player.KnowsSpell(CureDisease))
                        player.LuaCall($"CastSpellByName('{CureDisease}',2)");

                    return;
                }

                if (player.KnowsSpell(ShadowForm) && !player.HasBuff(ShadowForm))
                    player.LuaCall($"CastSpellByName('{ShadowForm}')");

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
                    botStates.Push(new BuffSelfState(botStates, container));

                return;
            }

            if (!player.IsDrinking && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();

                if (player.HealthPercent < 70)
                {
                    if (player.HasBuff(ShadowForm))
                        player.LuaCall($"CastSpellByName('{ShadowForm}')");
                }

                if (player.HealthPercent < 50)
                {
                    if (player.KnowsSpell(Heal))
                        player.LuaCall($"CastSpellByName('{Heal}',1)");
                    else
                        player.LuaCall($"CastSpellByName('{LesserHeal}',1)");
                }

                if (player.HealthPercent < 70)
                    player.LuaCall($"CastSpellByName('{LesserHeal}',1)");
            }

            if (player.Level >= 5 && drinkItem != null && !player.IsDrinking && player.ManaPercent < 60)
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the player's health percentage is greater than 90.
        /// </summary>
        bool HealthOk => player.HealthPercent > 90;

        /// <summary>
        /// Checks if the player's mana is sufficient for certain conditions.
        /// </summary>
        bool ManaOk => (player.Level < 5 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the player is currently in combat or if there are any units targeting the player.
        /// </summary>
        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
