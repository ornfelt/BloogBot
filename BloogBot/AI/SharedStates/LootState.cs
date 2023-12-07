using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Frames;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the shared states for the AI related to looting.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a class that handles the state of looting in a bot.
    /// </summary>
    public class LootState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a helper class for handling stuck operations.
        /// </summary>
        readonly StuckHelper stuckHelper;
        /// <summary>
        /// Represents the start time of the application in milliseconds since the system started.
        /// </summary>
        readonly int startTime = Environment.TickCount;

        /// <summary>
        /// Represents the count of times the program got stuck.
        /// </summary>
        int stuckCount;
        /// <summary>
        /// Represents a loot frame used for displaying loot items.
        /// </summary>
        LootFrame lootFrame;
        /// <summary>
        /// Represents the index of the loot.
        /// </summary>
        int lootIndex;
        /// <summary>
        /// Represents the current state of the loot.
        /// </summary>
        LootStates currentState;

        /// <summary>
        /// Initializes a new instance of the <see cref="LootState"/> class.
        /// </summary>
        public LootState(
                    Stack<IBotState> botStates,
                    IDependencyContainer container,
                    WoWUnit target)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        /// <summary>
        /// Updates the current state of the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// player -> player: Check Distance to Target
        /// player -> Navigation: GetNextWaypoint
        /// player -> player: MoveToward(nextWaypoint)
        /// player -> stuckHelper: CheckIfStuck()
        /// player -> player: Increment stuckCount
        /// player -> player: Check if Target CanBeLooted
        /// player -> player: StopAllMovement()
        /// player -> target: Interact()
        /// player -> player: Change currentState to RightClicked
        /// player -> player: Check State Transition Conditions
        /// player -> player: StopAllMovement()
        /// botStates -> botStates: Pop()
        /// botStates -> botStates: Push(new EquipBagsState)
        /// player -> container: Get nearestWaypoint
        /// botStates -> botStates: Push(new MoveToPositionState)
        /// player -> player: Check if currentState is RightClicked
        /// player -> LootFrame: Create new LootFrame
        /// player -> player: Change currentState to LootFrameReady
        /// player -> player: Check if currentState is LootFrameReady
        /// player -> lootFrame: Get itemToLoot
        /// player -> itemToLoot: Check itemQuality
        /// player -> DiscordClientWrapper: SendItemNotification
        /// player -> itemToLoot: Loot()
        /// player -> player: Increment lootIndex
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.Position.DistanceTo(target.Position) >= 5)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);

                if (!player.IsImmobilized)
                {
                    if (stuckHelper.CheckIfStuck())
                        stuckCount++;
                }
            }

            if (target.CanBeLooted && currentState == LootStates.Initial && player.Position.DistanceTo(target.Position) < 5)
            {
                player.StopAllMovement();

                if (Wait.For("StartLootDelay", 200))
                {
                    target.Interact();
                    currentState = LootStates.RightClicked;
                    return;
                }
            }

            // State Transition Conditions:
            //  - target can't be looted (no items to loot)
            //  - loot frame is open, but we've already looted everything we want
            //  - stuck count is greater than 5 (perhaps the corpse is in an awkward position the character can't reach)
            //  - we've been in the loot state for over 10 seconds (again, perhaps the corpse is unreachable. most common example of this is when a mob dies on a cliff that we can't climb)
            if ((currentState == LootStates.Initial && !target.CanBeLooted) || (lootFrame != null && lootIndex == lootFrame.LootItems.Count) || stuckCount > 5 || Environment.TickCount - startTime > 10000)
            {
                player.StopAllMovement();
                botStates.Pop();
                botStates.Push(new EquipBagsState(botStates, container));
                if (player.IsSwimming)
                {
                    var nearestWaypoint = container
                        .Hotspots
                        .Where(h => h != null)
                        .SelectMany(h => h.Waypoints)
                        .OrderBy(w => player.Position.DistanceTo(w))
                        .FirstOrDefault();
                    if (nearestWaypoint != null)
                        botStates.Push(new MoveToPositionState(botStates, container, nearestWaypoint));
                }
                return;
            }

            if (currentState == LootStates.RightClicked && Wait.For("LootFrameDelay", 1000))
            {
                lootFrame = new LootFrame();
                currentState = LootStates.LootFrameReady;
            }

            //if (currentState == LootStates.LootFrameReady && Wait.For("LootDelay", 150))
            if (currentState == LootStates.LootFrameReady && Wait.For("LootDelay", 150) && lootIndex < lootFrame.LootItems.Count)
            {
                var itemToLoot = lootFrame.LootItems.ElementAt(lootIndex);
                var itemQuality = ItemQuality.Common;
                if (itemToLoot.Info != null)
                {
                    itemQuality = itemToLoot.Info.Quality;
                }

                var poorQualityCondition = itemToLoot.IsCoins || itemQuality == ItemQuality.Poor && container.BotSettings.LootPoor;
                var commonQualityCondition = itemToLoot.IsCoins || itemQuality == ItemQuality.Common && container.BotSettings.LootCommon;
                var uncommonQualityCondition = itemToLoot.IsCoins || itemQuality == ItemQuality.Uncommon && container.BotSettings.LootUncommon;
                var other = itemQuality != ItemQuality.Poor && itemQuality != ItemQuality.Common && itemQuality != ItemQuality.Uncommon;

                if (itemQuality == ItemQuality.Rare || itemQuality == ItemQuality.Epic)
                    DiscordClientWrapper.SendItemNotification(player.Name, itemQuality, itemToLoot.ItemId);

                if (itemToLoot != null && itemToLoot.IsCoins
                    || ((string.IsNullOrWhiteSpace(container.BotSettings.LootExcludedNames) || !container.BotSettings.LootExcludedNames.Split('|').Any(en => itemToLoot.Info.Name.Contains(en)))
                    && (poorQualityCondition || commonQualityCondition || uncommonQualityCondition || other)))
                {
                    if (itemQuality == ItemQuality.Epic || itemToLoot.IsCoins) // Only loot epics / coins to not clutter bag
                        itemToLoot.Loot();
                    else
                        Console.WriteLine($"Skip looting item with quality: {itemQuality}");
                }

                lootIndex++;
            }
        }
    }

    /// <summary>
    /// Represents the possible states of loot.
    /// </summary>
    enum LootStates
    {
        Initial,
        RightClicked,
        LootFrameReady
    }
}
