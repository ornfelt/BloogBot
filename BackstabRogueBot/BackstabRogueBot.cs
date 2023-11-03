// Nat owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

/// <summary>
/// Namespace for the Backstab Rogue Bot.
/// </summary>
namespace BackstabRogueBot
{
    /// <summary>
    /// This class represents a Backstab Rogue bot.
    /// </summary>
    /// <summary>
    /// This class represents a Backstab Rogue bot.
    /// </summary>
    [Export(typeof(IBot))]
    class BackstabRogueBot : Bot, IBot
    {
        /// <summary>
        /// Gets the name of the Backstab Rogue.
        /// </summary>
        public string Name => "Backstab Rogue";

        /// <summary>
        /// Gets the file name of the BackstabRogueBot.dll.
        /// </summary>
        public string FileName => "BackstabRogueBot.dll";

        /// <summary>
        /// Determines if there are any additional targeting criteria for a given WoWUnit.
        /// </summary>
        bool AdditionalTargetingCriteria(WoWUnit u) =>
                    !ObjectManager.Units.Any(o =>
                        o.Level > ObjectManager.Player.Level - 4 &&
                        (o.UnitReaction == UnitReaction.Hated || o.UnitReaction == UnitReaction.Hostile) &&
                        o.Guid != ObjectManager.Player.Guid &&
                        o.Guid != u.Guid &&
                        o.Position.DistanceTo(u.Position) < 20
                    );

        /// <summary>
        /// Creates a new instance of the RestState class with the specified botStates and container.
        /// </summary>
        IBotState CreateRestState(Stack<IBotState> botStates, IDependencyContainer container) =>
                    new RestState(botStates, container);

        /// <summary>
        /// Creates a new instance of the MoveToTargetState class and returns it.
        /// </summary>
        IBotState CreateMoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) =>
                    new MoveToTargetState(botStates, container, target);

        /// <summary>
        /// Creates a new instance of PowerlevelCombatState with the specified parameters.
        /// </summary>
        IBotState CreatePowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget) =>
                    new PowerlevelCombatState(botStates, container, target, powerlevelTarget);

        /// <summary>
        /// Gets the dependency container for the bot.
        /// </summary>
        public IDependencyContainer GetDependencyContainer(BotSettings botSettings, Probe probe, IEnumerable<Hotspot> hotspots) =>
                    new DependencyContainer(
                        AdditionalTargetingCriteria,
                        CreateRestState,
                        CreateMoveToTargetState,
                        CreatePowerlevelCombatState,
                        botSettings,
                        probe,
                        hotspots);

        /// <summary>
        /// This method swaps out the player's mainhand weapon with an item from a bag.
        /// </summary>
        public void Test(IDependencyContainer container)
        {

            // this script simply swaps out your mainhand for something in a bag



            ThreadSynchronizer.RunOnMainThread(() =>
            {


                var player = ObjectManager.Player;
                bool SwapDaggerReady;
                bool DaggerEquipped;
                bool SwapMaceOrSwordReady;
                bool MaceOrSwordEquipped;

                WoWItem MainHand = Inventory.GetEquippedItem(EquipSlot.MainHand);
                WoWItem OffHand = Inventory.GetEquippedItem(EquipSlot.OffHand);
                WoWItem SwapSlotWeap = Inventory.GetItem(4, 1);

                Logger.LogVerbose("Mainhand Item Type:  " + MainHand.Info.ItemSubclass);
                Logger.LogVerbose("Offhand Item Type:  " + OffHand.Info.ItemSubclass);
                Logger.LogVerbose("Swap Weapon Item Type:  " + SwapSlotWeap.Info.ItemSubclass);
                Logger.LogVerbose("Swap Weapon Item Type:  " + SwapSlotWeap.Info.Name);

                // Check to see if a Dagger is Equipped in the mainhand

                if (MainHand.Info.ItemSubclass == ItemSubclass.Dagger)

                    DaggerEquipped = true;

                else DaggerEquipped = false;

                // Check to see if a 1H Sword or Mace is Equipped in the mainhand

                // if (MainHand.Info.ItemSubclass == ItemSubclass.OneHandedMace || ItemSubclass.OneHandedSword || ItemSubclass.OneHandedExotic)
                if (MainHand.Info.ItemSubclass == ItemSubclass.OneHandedSword)

                    MaceOrSwordEquipped = true;

                else MaceOrSwordEquipped = false;

                // Check to see if a Dagger is ready in the swap slot

                if (SwapSlotWeap.Info.ItemSubclass == ItemSubclass.Dagger)

                    SwapDaggerReady = true;

                else SwapDaggerReady = false;


                // Check to see if a Sword, 1H Mace, or fist weapon is ready in the swap slot

                // if (SwapSlotWeap.Info.ItemSubclass == ItemSubclass.OneHandedMace || ItemSubclass.OneHandedSword || ItemSubclass.OneHandedExotic)
                if (SwapSlotWeap.Info.ItemSubclass == ItemSubclass.OneHandedSword)

                    SwapMaceOrSwordReady = true;

                else SwapMaceOrSwordReady = false;

                // If Swap dagger is ready and the playe is not in combat, swap to a mainhand dagger.

                if (SwapDaggerReady == true && !player.IsInCombat)
                {
                    player.LuaCall($"UseContainerItem({4}, {2})");
                    Logger.LogVerbose(MainHand.Info.Name + " swapped Into Mainhand!");
                }

                // If there is a mace or swap in the swap slot, the player swap back to the 1H sword or mace.

                if (SwapMaceOrSwordReady == true)
                {
                    player.LuaCall($"UseContainerItem({4}, {2})");
                    Logger.LogVerbose(MainHand.Info.Name + "Swapped Into Mainhand!");
                }


            });


        }
    }
}
