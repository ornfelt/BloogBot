using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostMageBot
{
    class ConjureItemsState : IBotState
    {
        const string ConjureFood = "Conjure Food";
        const string ConjureWater = "Conjure Water";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        WoWItem foodItem;
        WoWItem drinkItem;

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

        public ConjureItemsState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => s_FoodNames.Contains(i.Info.Name));

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => s_DrinkNames.Contains(i.Info.Name));

            if (player.IsCasting)
                return;

            //player.Stand();

            if (player.ManaPercent < 20)
            {
                botStates.Pop();
                botStates.Push(new RestState(botStates, container));
                return;
            }

            if (Inventory.CountFreeSlots(false) == 0 || (foodItem != null || !player.KnowsSpell(ConjureFood)) && (drinkItem != null || !player.KnowsSpell(ConjureWater)))
            {
                botStates.Pop();

                if (player.ManaPercent <= 70)
                    botStates.Push(new RestState(botStates, container));

                return;
            }

            var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
            if (foodItem == null || foodCount <= 2)
                TryCastSpell(ConjureFood);

            var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);
            if (drinkItem == null || drinkCount <= 2)
                TryCastSpell(ConjureWater);
        }

        void TryCastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting)
                player.LuaCall($"CastSpellByName('{name}')");
        }
    }
}
