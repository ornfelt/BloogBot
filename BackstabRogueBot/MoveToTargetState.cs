// Nat owns this file!

using BloogBot.AI;
using BloogBot.Game.Objects;
using BloogBot.Game.Enums;
using System.Collections.Generic;
using BloogBot;
using BloogBot.Game;

/// <summary>
/// This namespace contains classes related to the Backstab Rogue Bot.
/// </summary>
namespace BackstabRogueBot
{
    /// <summary>
    /// Represents a state where the bot moves towards its target.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot moves towards its target.
    /// </summary>
    class MoveToTargetState : IBotState
    {
        /// <summary>
        /// The constant string value for distraction.
        /// </summary>
        const string Distract = "Distract";
        /// <summary>
        /// Represents a constant string named "Garrote".
        /// </summary>
        const string Garrote = "Garrote";
        /// <summary>
        /// Represents a constant string with the value "Stealth".
        /// </summary>
        const string Stealth = "Stealth";
        /// <summary>
        /// Represents a constant string value for "Cheap Shot".
        /// </summary>
        const string CheapShot = "Cheap Shot";
        /// <summary>
        /// Represents the constant string "Ambush".
        /// </summary>
        const string Ambush = "Ambush";

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
        /// Indicates whether the swap dagger is ready.
        /// </summary>
        bool SwapDaggerReady;
        /// <summary>
        /// Gets or sets a value indicating whether a dagger is equipped.
        /// </summary>
        bool DaggerEquipped;
        /// <summary>
        /// Indicates whether the swap between mace and sword is ready.
        /// </summary>
        bool SwapMaceOrSwordReady;
        /// <summary>
        /// Represents whether a mace or a sword is currently equipped.
        /// </summary>
        bool MaceOrSwordEquipped;

        /// <summary>
        /// Initializes a new instance of the MoveToTargetState class.
        /// </summary>
        internal MoveToTargetState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        /// <summary>
        /// Updates the behavior of the player character.
        /// </summary>
        public void Update()
        {
            if (target.TappedByOther || container.FindClosestTarget()?.Guid != target.Guid)
            {
                player.StopAllMovement();
                botStates.Pop();
                return;
            }

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (distanceToTarget < 30 && !player.HasBuff(Stealth) && player.KnowsSpell(Garrote) && !player.IsInCombat)
                player.LuaCall($"CastSpellByName('{Stealth}')");

            // Weapon Swap Logic
            ThreadSynchronizer.RunOnMainThread(() =>
            {

                WoWItem MainHand = Inventory.GetEquippedItem(EquipSlot.MainHand);
                WoWItem OffHand = Inventory.GetEquippedItem(EquipSlot.OffHand);
                WoWItem SwapSlotWeap = Inventory.GetItem(4, 1);

                //Logger.LogVerbose("Mainhand Item Type:  " + MainHand.Info.ItemSubclass);
                //Logger.LogVerbose("Offhand Item Type:  " + OffHand.Info.ItemSubclass);
                //Logger.LogVerbose("Swap Weapon Item Type:  " + SwapSlotWeap.Info.ItemSubclass);
                //Logger.LogVerbose("Swap Weapon Item Type:  " + SwapSlotWeap.Info.Name);

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

                // If there is a mace or swap in the swap slot, the player swap back to the 1H sword or mace.

                // if (SwapMaceOrSwordReady == true)
                // {
                //    player.LuaCall($"UseContainerItem({4}, {2})");
                //    Logger.LogVerbose(MainHand.Info.Name + "Swapped Into Mainhand!");
                //}

                // If Swap dagger is ready and the playe is not in combat, swap to a mainhand dagger.

                if (SwapDaggerReady == true && !player.IsInCombat && !player.HasBuff(Stealth))
                {
                    player.LuaCall($"UseContainerItem({4}, {2})");
                    Logger.LogVerbose(MainHand.Info.Name + " swapped Into Mainhand!");
                }

            });


            if (distanceToTarget < 25 && player.KnowsSpell(Distract) && player.IsSpellReady(Distract) && player.HasBuff(Stealth))
            {
                var delta = target.Position - player.Position;
                var normalizedVector = delta.GetNormalizedVector();
                var scaledVector = normalizedVector * 5;
                var targetPosition = target.Position + scaledVector;

                player.CastSpellAtPosition(Distract, targetPosition);
            }

            if (distanceToTarget < 5 && player.HasBuff(Stealth) && player.IsSpellReady(Ambush) && DaggerEquipped && !player.IsInCombat && player.IsBehind(target))
            {
                player.LuaCall($"CastSpellByName('{Ambush}')");
                return;
            }

            if (distanceToTarget < 5 && player.HasBuff(Stealth) && player.IsSpellReady(Garrote) && !DaggerEquipped && !player.IsInCombat && player.IsBehind(target))
            {
                player.LuaCall($"CastSpellByName('{Garrote}')");
                return;
            }

            if (distanceToTarget < 5 && player.HasBuff(Stealth) && player.IsSpellReady(CheapShot) && !player.IsInCombat && !player.IsBehind(target))
            {
                player.LuaCall($"CastSpellByName('{CheapShot}')");
                return;
            }

            if (distanceToTarget < 2.5)
            {
                player.StopAllMovement();
                botStates.Pop();
                botStates.Push(new CombatState(botStates, container, target));
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }
    }
}


