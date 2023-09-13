using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
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

        private static readonly List<string> s_FoodNames = new List<string> 
        {
            "Conjured Cinnamon Roll", "Conjured Croissant", "Conjured Pumpernickel", 
            "Conjured Rye", "Conjured Muffin", "Conjured Sourdough", 
            "Conjured Mana Pie", "Conjured Sweet Roll", "Conjured Bread", 
            "Conjured Mana Strudel", "Conjured Mana Biscuit"
        };

        private static readonly List<string> s_DrinkNames = new List<string>
        {
            "Conjured Crystal Water", "Conjured Purified Water", "Conjured Fresh Water",
            "Conjured Spring Water", "Conjured Water", "Conjured Mineral Water",
            "Conjured Sparkling Water", "Conjured Glacier Water",
            "Conjured Mountain Spring Water", "Conjured Mana Strudel",
            "Conjured Mana Biscuit", "Conjured Mana Pie"
        };

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => s_FoodNames.Contains(i.Info.Name));

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => s_DrinkNames.Contains(i.Info.Name));
        }

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
            else if (player.ManaPercent < 20 && drinkItem == null)
            {
                // Is this needed?
                botStates.Pop();
                botStates.Push(new ConjureItemsState(botStates, container));
            }

            if (foodItem != null && !player.IsEating && player.HealthPercent < 80)
                foodItem.Use();

            if (drinkItem != null && !player.IsDrinking)
                drinkItem.Use();
        }

        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 80 && !player.IsEating);

        bool ManaOk => drinkItem == null || player.ManaPercent >= 90 || (player.ManaPercent >= 80 && !player.IsDrinking);

        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);
    }
}
