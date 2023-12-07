using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the implementation of the FeralDruidBot, which is responsible for handling the behavior of a feral druid character in a game.
/// </summary>
namespace FeralDruidBot
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
        /// Represents the human form.
        /// </summary>
        const string HumanForm = "Human Form";
        /// <summary>
        /// Represents the constant string "Bear Form".
        /// </summary>
        const string BearForm = "Bear Form";
        /// <summary>
        /// Represents the constant string "Cat Form".
        /// </summary>
        const string CatForm = "Cat Form";
        /// <summary>
        /// Represents the constant string "Regrowth".
        /// </summary>
        const string Regrowth = "Regrowth";
        /// <summary>
        /// The constant string representing "Rejuvenation".
        /// </summary>
        const string Rejuvenation = "Rejuvenation";

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
        /// Updates the player's actions based on various conditions, such as combat status, health and mana levels, and current shapeshift form.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> player: IsCasting
        /// alt player.IsCasting is true
        ///     Update -> Update: return
        /// else InCombat is true
        ///     Update -> Wait: RemoveAll
        ///     Update -> player: Stand
        ///     Update -> botStates: Pop
        ///     Update -> Update: return
        /// else HealthOk and ManaOk are true
        ///     alt player.HasBuff(BearForm) and Wait.For("BearFormDelay", 1000, true) are true
        ///         Update -> Update: CastSpell(BearForm)
        ///     else player.HasBuff(CatForm) and Wait.For("CatFormDelay", 1000, true) are true
        ///         Update -> Update: CastSpell(CatForm)
        ///     else
        ///         Update -> Wait: RemoveAll
        ///         Update -> player: Stand
        ///         Update -> botStates: Pop
        ///         Update -> Inventory: GetItemCount(drinkItem.ItemId)
        ///         alt !InCombat and drinkCount == 0 and !container.RunningErrands are true
        ///             Update -> container: GetCurrentHotspot
        ///             Update -> botStates: Push(new TravelState)
        ///             Update -> botStates: Push(new MoveToPositionState)
        ///             Update -> botStates: Push(new BuyItemsState)
        ///             Update -> botStates: Push(new SellItemsState)
        ///             Update -> botStates: Push(new MoveToPositionState)
        ///             Update -> container: CheckForTravelPath
        ///             Update -> container: RunningErrands = true
        ///         else
        ///             Update -> botStates: Push(new BuffSelfState)
        ///         end
        ///     end
        /// else player.CurrentShapeshiftForm == BearForm
        ///     Update -> Update: CastSpell(BearForm)
        /// else player.CurrentShapeshiftForm == CatForm
        ///     Update -> Update: CastSpell(CatForm)
        /// else player.HealthPercent < 60 and player.CurrentShapeshiftForm == HumanForm and !player.HasBuff(Regrowth) and Wait.For("SelfHealDelay", 5000, true) are true
        ///     Update -> Update: TryCastSpell(Regrowth)
        /// else player.HealthPercent < 80 and player.CurrentShapeshiftForm == HumanForm and !player.HasBuff(Rejuvenation) and !player.HasBuff(Regrowth) and Wait.For("SelfHealDelay", 5000, true) are true
        ///     Update -> Update: TryCastSpell(Rejuvenation)
        /// else player.Level > 8 and drinkItem != null and !player.IsDrinking and player.ManaPercent < 60 and player.CurrentShapeshiftForm == HumanForm are true
        ///     Update -> drinkItem: Use
        /// end
        /// \enduml
        /// </remarks>
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
                if (player.HasBuff(BearForm) && Wait.For("BearFormDelay", 1000, true))
                    CastSpell(BearForm);
                else if (player.HasBuff(CatForm) && Wait.For("CatFormDelay", 1000, true))
                    CastSpell(CatForm);
                else
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
                        botStates.Push(new BuffSelfState(botStates, container));
                }
            }

            if (player.CurrentShapeshiftForm == BearForm)
                CastSpell(BearForm);

            if (player.CurrentShapeshiftForm == CatForm)
                CastSpell(CatForm);

            if (player.HealthPercent < 60 && player.CurrentShapeshiftForm == HumanForm && !player.HasBuff(Regrowth) && Wait.For("SelfHealDelay", 5000, true))
                TryCastSpell(Regrowth);

            if (player.HealthPercent < 80 && player.CurrentShapeshiftForm == HumanForm && !player.HasBuff(Rejuvenation) && !player.HasBuff(Regrowth) && Wait.For("SelfHealDelay", 5000, true))
                TryCastSpell(Rejuvenation);

            if (player.Level > 8 && drinkItem != null && !player.IsDrinking && player.ManaPercent < 60 && player.CurrentShapeshiftForm == HumanForm)
                drinkItem.Use();
        }

        /// <summary>
        /// Checks if the player's health percentage is greater than or equal to 81.
        /// </summary>
        bool HealthOk => player.HealthPercent >= 81;

        /// <summary>
        /// Checks if the player's mana is sufficient for certain conditions.
        /// </summary>
        bool ManaOk => (player.Level <= 8 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);

        /// <summary>
        /// Gets a value indicating whether the current object is in combat.
        /// </summary>
        bool InCombat => ObjectManager.Aggressors.Count() > 0;

        /// <summary>
        /// Casts a spell by its name if the spell is ready and the player is not currently drinking.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "player" as P
        /// participant "CastSpell" as C
        /// 
        /// C -> P: IsSpellReady(name)
        /// activate P
        /// P --> C: true/false
        /// deactivate P
        /// 
        /// C -> P: IsDrinking
        /// activate P
        /// P --> C: true/false
        /// deactivate P
        /// 
        /// alt spell is ready and player is not drinking
        ///     C -> P: LuaCall("CastSpellByName('name')")
        ///     activate P
        ///     deactivate P
        /// end
        /// \enduml
        /// </remarks>
        void CastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsDrinking)
                player.LuaCall($"CastSpellByName('{name}')");
        }

        /// <summary>
        /// Tries to cast a spell by the given name if the player is able to cast it.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TryCastSpell Method" as T
        /// participant "Player" as P
        /// 
        /// T -> P: IsSpellReady(name)
        /// activate P
        /// P --> T: Spell Ready Status
        /// deactivate P
        /// 
        /// T -> P: IsCasting
        /// activate P
        /// P --> T: Casting Status
        /// deactivate P
        /// 
        /// T -> P: Mana
        /// activate P
        /// P --> T: Current Mana
        /// deactivate P
        /// 
        /// T -> P: GetManaCost(name)
        /// activate P
        /// P --> T: Mana Cost
        /// deactivate P
        /// 
        /// T -> P: IsDrinking
        /// activate P
        /// P --> T: Drinking Status
        /// deactivate P
        /// 
        /// T -> P: LuaCall("CastSpellByName('name',1)")
        /// activate P
        /// P --> T: Spell Casted
        /// deactivate P
        /// 
        /// \enduml
        /// </remarks>
        void TryCastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting && player.Mana > player.GetManaCost(name) && !player.IsDrinking)
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
