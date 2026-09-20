using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace FrostMageBot
{
    class RestState : IBotState
    {
        const string Evocation = "Evocation";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        readonly WoWItem foodItem;
        readonly WoWItem drinkItem;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

#if USE_CUSTOM_CHANGES
            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => player.FoodNames.Contains(i.Info.Name) || i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => player.DrinkNames.Contains(i.Info.Name) || i.Info.Name == container.BotSettings.Drink);
#else
            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
#endif
        }

        public void Update()
        {
            if (player.IsChanneling)
                return;

#if USE_CUSTOM_CHANGES
            if (InCombat)
#else
            if (InCombat || ObjectManager.GetPartyMembers().Any(p => p.IsInCombat))
#endif
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

#if USE_CUSTOM_CHANGES
            if (drinkItem != null && !player.IsDrinking)
                drinkItem.Use();
#else
            var drinkItemExists = drinkItem != null;
            var soloCondition = !ObjectManager.IsGrouped && player.ManaPercent < 70;
            var groupedCondition = ObjectManager.IsGrouped && (player.ManaPercent < 30 || (ObjectManager.GetPartyMembers().Any(p => p.IsDrinking) && player.ManaPercent < 70));
            if (drinkItem != null && !player.IsDrinking && (soloCondition || groupedCondition))
                drinkItem.Use();
#endif
        }

        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 80 && !player.IsEating);

#if USE_CUSTOM_CHANGES
        bool ManaOk => drinkItem == null || player.ManaPercent >= 90 || (player.ManaPercent >= 80 && !player.IsDrinking);
#else
        bool ManaOk => drinkItem == null || player.ManaPercent >= 90 || (player.ManaPercent >= 80 && !player.IsDrinking) || (ObjectManager.GetPartyMembers().Any(p => p.IsDrinking) && player.ManaPercent >= 90) || (!ObjectManager.GetPartyMembers().Any(p => p.IsDrinking) && player.ManaPercent >= 30 && ObjectManager.IsGrouped);
#endif

        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
