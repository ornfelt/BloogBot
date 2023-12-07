using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functionality for the Frost Mage Bot.
/// </summary>
namespace FrostMageBot
{
    /// <summary>
    /// Represents a combat state in the game. Inherits from CombatStateBase and implements IBotState.
    /// </summary>
    /// <summary>
    /// Represents a combat state in the game. Inherits from CombatStateBase and implements IBotState.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The Lua script for casting the "Shoot" spell if the action with ID 11 is not set to auto-repeat.
        /// </summary>
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";

        /// <summary>
        /// An array of strings representing the targets that are affected by the Fire Ward.
        /// </summary>
        readonly string[] FireWardTargets = new[] { "Fire", "Flame", "Infernal", "Searing", "Hellcaller", "Dragon", "Whelp" };
        /// <summary>
        /// The targets that are affected by Frost Ward.
        /// </summary>
        readonly string[] FrostWardTargets = new[] { "Ice", "Frost" };

        /// <summary>
        /// Represents the constant string "Cold Snap".
        /// </summary>
        const string ColdSnap = "Cold Snap";
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
        /// Represents the constant string "Icy Veins".
        /// </summary>
        const string IcyVeins = "Icy Veins";
        /// <summary>
        /// Represents the constant string value for "Summon Water Elemental".
        /// </summary>
        const string SummonWaterElemental = "Summon Water Elemental";

        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Gets or sets the value of the nuke.
        /// </summary>
        readonly string nuke;
        /// <summary>
        /// Represents a readonly integer range.
        /// </summary>
        readonly int range;

        /// <summary>
        /// Represents a boolean value indicating whether the character is performing a frost nova backpedaling.
        /// </summary>
        bool frostNovaBackpedaling;
        /// <summary>
        /// Represents the start time of the backpedal for the Frost Nova ability.
        /// </summary>
        int frostNovaBackpedalStartTime;
        /// <summary>
        /// Represents a boolean value indicating whether the frost nova has jumped.
        /// </summary>
        bool frostNovaJumped;
        /// <summary>
        /// Indicates whether the Frost Nova has started moving.
        /// </summary>
        bool frostNovaStartedMoving;

        /// <summary>
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 29 + (ObjectManager.GetTalentRank(3, 11) * 3))
        {
            player = ObjectManager.Player;
            this.target = target;

            if (!player.KnowsSpell(Frostbolt))
                nuke = Fireball;
            else if (player.Level >= 8)
                nuke = Frostbolt;
            else if (player.Level >= 6)
                nuke = Fireball;
            else if (player.Level >= 4)
                nuke = Frostbolt;
            else
                nuke = Fireball;

            range = 29 + (ObjectManager.GetTalentRank(3, 11) * 3);
        }

        /// <summary>
        /// Updates the character's actions based on certain conditions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Environment: TickCount
        /// Update -> Player: Turn180()
        /// Update -> Player: StartMovement(ControlBits.Front)
        /// Update -> Player: Jump()
        /// Update -> Player: StopMovement(ControlBits.Front)
        /// Update -> Player: Face(target.Position)
        /// Update -> Update: TryCastSpell(FrostNova)
        /// Update -> Base: Update()
        /// Update -> Update: TryCastSpell(Evocation)
        /// Update -> Inventory: GetEquippedItem(EquipSlot.Ranged)
        /// Update -> Player: LuaCall(WandLuaScript)
        /// Update -> Update: TryCastSpell(SummonWaterElemental)
        /// Update -> Update: TryCastSpell(ColdSnap)
        /// Update -> Update: TryCastSpell(IcyVeins)
        /// Update -> Update: TryCastSpell(FireWard)
        /// Update -> Update: TryCastSpell(FrostWard)
        /// Update -> Update: TryCastSpell(Counterspell)
        /// Update -> Update: TryCastSpell(IceBarrier)
        /// Update -> Update: TryCastSpell(FrostNova)
        /// Update -> Update: TryCastSpell(ConeOfCold)
        /// Update -> Update: TryCastSpell(FireBlast)
        /// Update -> Update: TryCastSpell(nuke)
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (frostNovaBackpedaling && !frostNovaStartedMoving && Environment.TickCount - frostNovaBackpedalStartTime > 200)
            {
                player.Turn180();
                player.StartMovement(ControlBits.Front);
                frostNovaStartedMoving = true;
            }
            if (frostNovaBackpedaling && !frostNovaJumped && Environment.TickCount - frostNovaBackpedalStartTime > 500)
            {
                player.Jump();
                frostNovaJumped = true;
            }
            if (frostNovaBackpedaling && Environment.TickCount - frostNovaBackpedalStartTime > 2500)
            {
                player.StopMovement(ControlBits.Front);
                player.Face(target.Position);
                frostNovaBackpedaling = false;
            }

            if (frostNovaBackpedaling)
            {
                TryCastSpell(FrostNova); // sometimes we try to cast too early and get into this state while FrostNova is still ready.
                return;
            }

            if (base.Update())
                return;

            TryCastSpell(Evocation, 0, int.MaxValue, (player.HealthPercent > 50 || player.HasBuff(IceBarrier)) && player.ManaPercent < 8 && target.HealthPercent > 15);

            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (wand != null && player.ManaPercent <= 10 && !player.IsCasting && !player.IsChanneling)
                player.LuaCall(WandLuaScript);
            else
            {
                TryCastSpell(SummonWaterElemental, !ObjectManager.Units.Any(u => u.Name == "Water Elemental" && u.SummonedByGuid == player.Guid));

                TryCastSpell(ColdSnap, !player.IsSpellReady(SummonWaterElemental));

                TryCastSpell(IcyVeins, ObjectManager.Aggressors.Count() > 1);

                TryCastSpell(FireWard, 0, int.MaxValue, FireWardTargets.Any(c => target.Name.Contains(c)) && (target.HealthPercent > 20 || player.HealthPercent < 10));

                TryCastSpell(FrostWard, 0, int.MaxValue, FrostWardTargets.Any(c => target.Name.Contains(c)) && (target.HealthPercent > 20 || player.HealthPercent < 10));

                TryCastSpell(Counterspell, 0, 30, target.Mana > 0 && target.IsCasting);

                TryCastSpell(IceBarrier, 0, 50, !player.HasBuff(IceBarrier) && (ObjectManager.Aggressors.Count() >= 2 || (!player.IsSpellReady(FrostNova) && player.HealthPercent < 95 && player.ManaPercent > 40 && (target.HealthPercent > 20 || player.HealthPercent < 10))));

                TryCastSpell(FrostNova, 0, 9, target.TargetGuid == player.Guid && (target.HealthPercent > 20 || player.HealthPercent < 30) && !IsTargetFrozen && !ObjectManager.Units.Any(u => u.Guid != target.Guid && u.HealthPercent > 0 && u.Guid != player.Guid && u.Position.DistanceTo(player.Position) <= 12), callback: FrostNovaCallback);

                TryCastSpell(ConeOfCold, 0, 8, player.Level >= 30 && target.HealthPercent > 20 && IsTargetFrozen);

                TryCastSpell(FireBlast, 0, 20, !IsTargetFrozen);

                // Either Frostbolt or Fireball depending on what is stronger. Will always use Frostbolt at level 8+.
                TryCastSpell(nuke, 0, range);
            }
        }

        /// <summary>
        /// Callback function for Frost Nova action.
        /// Resets the flags for frostNovaStartedMoving, frostNovaJumped, and frostNovaBackpedaling.
        /// Sets the frostNovaBackpedalStartTime to the current tick count.
        /// </summary>
        Action FrostNovaCallback => () =>
                {
                    frostNovaStartedMoving = false;
                    frostNovaJumped = false;
                    frostNovaBackpedaling = true;
                    frostNovaBackpedalStartTime = Environment.TickCount;
                };

        /// <summary>
        /// Checks if the target is frozen by checking if it has the debuffs Frostbite or FrostNova.
        /// </summary>
        bool IsTargetFrozen => target.HasDebuff(Frostbite) || target.HasDebuff(FrostNova);
    }
}
