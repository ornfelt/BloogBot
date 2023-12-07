using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functions related to the Frost Mage Bot.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Represents a combat state for a bot with powerlevel abilities.
    /// </summary>
    /// <summary>
    /// Represents a combat state for a bot with powerlevel abilities.
    /// </summary>
    class PowerlevelCombatState : IBotState
    {
        /// <summary>
        /// Represents the error message for when the target is not in line of sight.
        /// </summary>
        const string LosErrorMessage = "Target not in line of sight";
        /// <summary>
        /// The Lua script for casting the "Shoot" spell if the action with ID 11 is not set to auto-repeat.
        /// </summary>
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";

        /// <summary>
        /// An array of strings representing the targets for a Fire Ward.
        /// </summary>
        readonly string[] FireWardTargets = new[] { "Fire", "Flame", "Infernal", "Searing", "Hellcaller" };
        /// <summary>
        /// The targets that are affected by Frost Ward.
        /// </summary>
        readonly string[] FrostWardTargets = new[] { "Ice", "Frost" };

        /// <summary>
        /// Represents the spell "Cone of Cold".
        /// </summary>
        const string ConeOfCold = "Cone of Cold";
        /// <summary>
        /// Represents the name of the counterspell.
        /// </summary>
        const string Counterspell = "Counterspell";
        /// <summary>
        /// Represents the constant string "Evocation".
        /// </summary>
        const string Evocation = "Evocation";
        /// <summary>
        /// Represents a constant string value for "Fireball".
        /// </summary>
        const string Fireball = "Fireball";
        /// <summary>
        /// Represents the constant string "Fire Blast".
        /// </summary>
        const string FireBlast = "Fire Blast";
        /// <summary>
        /// Represents the constant string "Fire Ward".
        /// </summary>
        const string FireWard = "Fire Ward";
        /// <summary>
        /// Represents the constant string "Frost Nova".
        /// </summary>
        const string FrostNova = "Frost Nova";
        /// <summary>
        /// Represents the constant string "Frost Ward".
        /// </summary>
        const string FrostWard = "Frost Ward";
        /// <summary>
        /// Represents the constant value "Frostbite".
        /// </summary>
        const string Frostbite = "Frostbite";
        /// <summary>
        /// The constant string representing the spell "Frostbolt".
        /// </summary>
        const string Frostbolt = "Frostbolt";
        /// <summary>
        /// Represents the constant string "Ice Barrier".
        /// </summary>
        const string IceBarrier = "Ice Barrier";

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
        /// Gets or sets the value of the nuke.
        /// </summary>
        readonly string nuke;
        /// <summary>
        /// Represents a readonly integer range.
        /// </summary>
        readonly int range;

        /// <summary>
        /// Represents a boolean value indicating whether there are any line of sight (LOS) obstacles.
        /// </summary>
        bool noLos;
        /// <summary>
        /// Represents the start time for the "no loss" period.
        /// </summary>
        int noLosStartTime;

        /// <summary>
        /// Represents a boolean value indicating whether backpedaling is occurring.
        /// </summary>
        bool backpedaling;
        /// <summary>
        /// Represents the start time of the backpedal.
        /// </summary>
        int backpedalStartTime;

        /// <summary>
        /// Callback function for Frost Nova action. Sets the backpedaling flag to true, records the start time of backpedaling, and starts the player's movement in the backward direction.
        /// </summary>
        Action FrostNovaCallback => () =>
                {
                    backpedaling = true;
                    backpedalStartTime = Environment.TickCount;
                    player.StartMovement(ControlBits.Back);
                };

        /// <summary>
        /// Initializes a new instance of the PowerlevelCombatState class.
        /// </summary>
        public PowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;

            WoWEventHandler.OnErrorMessage += OnErrorMessageCallback;

            if (player.Level >= 8)
                nuke = Frostbolt;
            else if (player.Level >= 6 || !player.KnowsSpell(Frostbolt))
                nuke = Fireball;
            else if (player.Level >= 4 && player.KnowsSpell(Frostbolt))
                nuke = Frostbolt;
            else
                nuke = Fireball;

            range = 29 + (ObjectManager.GetTalentRank(3, 11) * 3);
        }

        /// <summary>
        /// Destructor for the PowerlevelCombatState class. Unsubscribes from the OnErrorMessage event.
        /// </summary>
        ~PowerlevelCombatState()
        {
            WoWEventHandler.OnErrorMessage -= OnErrorMessageCallback;
        }

        /// <summary>
        /// Updates the combat rotation for the player character.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> player: IsChanneling
        /// alt player is channeling
        ///     Update -> Update: return
        /// else player is not channeling
        ///     Update -> Environment: TickCount
        ///     Update -> backpedalStartTime: compare
        ///     alt backpedalStartTime > 1500
        ///         Update -> player: StopMovement(ControlBits.Back)
        ///         Update -> backpedaling: set false
        ///     end
        ///     alt backpedaling is true
        ///         Update -> Update: return
        ///     else backpedaling is false
        ///         Update -> Environment: TickCount
        ///         Update -> noLosStartTime: compare
        ///         alt noLosStartTime > 1000
        ///             Update -> player: StopAllMovement
        ///             Update -> noLos: set false
        ///         end
        ///         alt noLos is true
        ///             Update -> Navigation: GetNextWaypoint
        ///             Update -> player: MoveToward(nextWaypoint)
        ///             Update -> Update: return
        ///         else noLos is false
        ///             Update -> target: TappedByOther
        ///             alt target is tapped by other
        ///                 Update -> botStates: Pop
        ///                 Update -> Update: return
        ///             else target is not tapped by other
        ///                 Update -> ObjectManager: Units.FirstOrDefault
        ///                 alt target.Health == 0 or checkTarget == null
        ///                     Update -> Wait: For(waitKey, 1500)
        ///                     alt wait is true
        ///                         Update -> botStates: Pop
        ///                         alt player is not swimming
        ///                             Update -> botStates: Push(new RestState)
        ///                         end
        ///                         Update -> Wait: Remove(waitKey)
        ///                     end
        ///                     Update -> Update: return
        ///                 else target.Health != 0 and checkTarget != null
        ///                     Update -> player: TargetGuid
        ///                     alt player.TargetGuid != target.Guid
        ///                         Update -> player: SetTarget(target.Guid)
        ///                     end
        ///                     Update -> player: IsFacing(target.Position)
        ///                     alt player is not facing target
        ///                         Update -> player: Face(target.Position)
        ///                     end
        ///                     Update -> player: Position.DistanceTo(target.Position)
        ///                     alt distance > range and player is not casting
        ///                         Update -> Navigation: GetNextWaypoint
        ///                         Update -> player: MoveToward(nextWaypoint)
        ///                     else distance <= range or player is casting
        ///                         Update -> player: StopAllMovement
        ///                     end
        ///                     Update -> Update: TryCastSpell
        ///                 end
        ///             end
        ///         end
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.IsChanneling)
                return;

            if (Environment.TickCount - backpedalStartTime > 1500)
            {
                player.StopMovement(ControlBits.Back);
                backpedaling = false;
            }

            if (backpedaling)
                return;

            if (Environment.TickCount - noLosStartTime > 1000)
            {
                player.StopAllMovement();
                noLos = false;
            }

            if (noLos)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
                return;
            }

            if (target.TappedByOther)
            {
                botStates.Pop();
                return;
            }

            // when killing certain summoned units (like totems), our local reference to target will still have 100% health even after the totem is destroyed
            // so we need to lookup the target again in the object manager, and if it's null, we can assume it's dead and leave combat.
            var checkTarget = ObjectManager.Units.FirstOrDefault(u => u.Guid == target.Guid);
            if (target.Health == 0 || checkTarget == null)
            {
                const string waitKey = "PopCombatState";

                if (Wait.For(waitKey, 1500))
                {
                    botStates.Pop();

                    if (!player.IsSwimming)
                        botStates.Push(new RestState(botStates, container));

                    Wait.Remove(waitKey);
                }

                return;
            }

            // ensure the correct target is set
            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            // ensure we're in melee range
            if (player.Position.DistanceTo(target.Position) > range && !player.IsCasting)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
            else
                player.StopAllMovement();

            // ----- COMBAT ROTATION -----
            TryCastSpell(Evocation, 0, Int32.MaxValue, (player.HealthPercent > 50 || player.HasBuff(IceBarrier)) && player.ManaPercent < 8 && target.HealthPercent > 15);

            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (wand != null && player.ManaPercent <= 10 && !player.IsCasting && !player.IsChanneling)
                player.LuaCall(WandLuaScript);
            else
            {
                TryCastSpell(FireWard, 0, Int32.MaxValue, FireWardTargets.Any(c => target.Name.Contains(c)) && (target.HealthPercent > 20 || player.HealthPercent < 10));

                TryCastSpell(FrostWard, 0, Int32.MaxValue, FrostWardTargets.Any(c => target.Name.Contains(c)) && (target.HealthPercent > 20 || player.HealthPercent < 10));

                TryCastSpell(Counterspell, 0, 29, target.Mana > 0 && target.IsCasting);

                TryCastSpell(IceBarrier, 0, 50, !player.HasBuff(IceBarrier) && player.HealthPercent < 95 && player.ManaPercent > 40 && (target.HealthPercent > 20 || player.HealthPercent < 10));

                TryCastSpell(FrostNova, 0, 10, target.TargetGuid == player.Guid && target.HealthPercent > 20 && !IsTargetFrozen && !ObjectManager.Units.Any(u => u.Guid != target.Guid && u.HealthPercent > 0 && u.Guid != player.Guid && u.Position.DistanceTo(player.Position) <= 12), callback: FrostNovaCallback);

                TryCastSpell(ConeOfCold, 0, 8, player.Level >= 30 && target.HealthPercent > 20 && IsTargetFrozen);

                TryCastSpell(FireBlast, 0, 19, !IsTargetFrozen);

                // Either Frostbolt or Fireball depending on what is stronger. Will always use Frostbolt at level 8+.
                TryCastSpell(nuke, 0, range);
            }
        }

        /// <summary>
        /// Tries to cast a spell with the given name within a specified range, under certain conditions, and with an optional callback.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "TryCastSpell Function" as TCS
        /// participant "Player" as P
        /// TCS -> P: IsSpellReady(name)
        /// activate P
        /// P --> TCS: Spell Ready Status
        /// deactivate P
        /// TCS -> P: GetManaCost(name)
        /// activate P
        /// P --> TCS: Mana Cost
        /// deactivate P
        /// TCS -> P: IsStunned
        /// activate P
        /// P --> TCS: Stunned Status
        /// deactivate P
        /// TCS -> P: IsCasting
        /// activate P
        /// P --> TCS: Casting Status
        /// deactivate P
        /// TCS -> P: IsChanneling
        /// activate P
        /// P --> TCS: Channeling Status
        /// deactivate P
        /// TCS -> P: LuaCall(CastSpellByName)
        /// activate P
        /// P --> TCS: Spell Casted
        /// deactivate P
        /// TCS -> TCS: callback?.Invoke()
        /// \enduml
        /// </remarks>
        void TryCastSpell(string name, int minRange, int maxRange, bool condition = true, Action callback = null)
        {
            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && distanceToTarget >= minRange && distanceToTarget <= maxRange && condition && !player.IsStunned && !player.IsCasting && !player.IsChanneling)
            {
                player.LuaCall($"CastSpellByName('{name}')");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Event handler for error messages.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// object -> OnErrorMessageCallback : sender, OnUiMessageArgs e
        /// OnErrorMessageCallback -> OnErrorMessageCallback : Check if e.Message == LosErrorMessage
        /// alt e.Message == LosErrorMessage
        /// OnErrorMessageCallback -> OnErrorMessageCallback : Set noLos = true
        /// OnErrorMessageCallback -> OnErrorMessageCallback : Set noLosStartTime = Environment.TickCount
        /// end
        /// \enduml
        /// </remarks>
        void OnErrorMessageCallback(object sender, OnUiMessageArgs e)
        {
            if (e.Message == LosErrorMessage)
            {
                noLos = true;
                noLosStartTime = Environment.TickCount;
            }
        }

        /// <summary>
        /// Checks if the target is frozen by checking if it has the debuffs Frostbite or FrostNova.
        /// </summary>
        bool IsTargetFrozen => target.HasDebuff(Frostbite) || target.HasDebuff(FrostNova);
    }
}
