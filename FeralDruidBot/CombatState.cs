using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functions related to the Feral Druid Bot.
/// </summary>
namespace FeralDruidBot
{
    /// <summary>
    /// This class represents the combat state of a bot in a game.
    /// </summary>
    class CombatState : IBotState
    {
        /// <summary>
        /// The Lua script for auto attacking. If the current action is not '12', it casts the 'Attack' spell.
        /// </summary>
        const string AutoAttackLuaScript = "if IsCurrentAction('12') == nil then CastSpellByName('Attack') end";

        /// <summary>
        /// Represents the constant string value for "Bear Form" used for shapeshifting.
        /// </summary>
        // Shapeshifting
        const string BearForm = "Bear Form";
        /// <summary>
        /// Represents the constant string "Cat Form".
        /// </summary>
        const string CatForm = "Cat Form";
        /// <summary>
        /// Represents the human form.
        /// </summary>
        const string HumanForm = "Human Form";

        /// <summary>
        /// Represents a bear.
        /// </summary>
        // Bear
        const string Maul = "Maul";
        /// <summary>
        /// Represents the constant string "Enrage".
        /// </summary>
        const string Enrage = "Enrage";
        /// <summary>
        /// Represents the constant string value for "Demoralizing Roar".
        /// </summary>
        const string DemoralizingRoar = "Demoralizing Roar";

        /// <summary>
        /// Represents a cat.
        /// </summary>
        // Cat
        const string Claw = "Claw";
        /// <summary>
        /// Represents a constant string with the value "Rake".
        /// </summary>
        const string Rake = "Rake";
        /// <summary>
        /// Represents a constant string with the value "Rip".
        /// </summary>
        const string Rip = "Rip";
        /// <summary>
        /// Represents the constant string "Tiger's Fury".
        /// </summary>
        const string TigersFury = "Tiger's Fury";

        /// <summary>
        /// Represents a human with the ability to perform a healing touch.
        /// </summary>
        // Human
        const string HealingTouch = "Healing Touch";
        /// <summary>
        /// Represents the constant string "Moonfire".
        /// </summary>
        const string Moonfire = "Moonfire";
        /// <summary>
        /// Represents the constant string "Wrath".
        /// </summary>
        const string Wrath = "Wrath";

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
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;

        /// <summary>
        /// Represents the last position of the target.
        /// </summary>
        Position targetLastPosition;

        /// <summary>
        /// Initializes a new instance of the CombatState class.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the player's actions based on their health, mana, and target status.
        /// </summary>
        public void Update()
        {
            if (player.HealthPercent < 30 && player.Mana >= player.GetManaCost(HealingTouch))
            {
                if (player.CurrentShapeshiftForm == BearForm && Wait.For("BearFormDelay", 1000, true))
                    CastSpell(BearForm);

                if (player.CurrentShapeshiftForm == CatForm && Wait.For("CatFormDelay", 1000, true))
                    CastSpell(CatForm);

                Wait.RemoveAll();
                botStates.Push(new HealSelfState(botStates, container, target));
                return;
            }

            if (target.TappedByOther)
            {
                player.StopAllMovement();
                Wait.RemoveAll();
                botStates.Pop();
                return;
            }

            if (target.Health == 0)
            {
                const string waitKey = "PopCombatState";

                if (Wait.For(waitKey, 1500))
                {
                    player.StopAllMovement();
                    botStates.Pop();
                    botStates.Push(new LootState(botStates, container, target));
                    Wait.Remove(waitKey);
                }

                return;
            }

            if (player.TargetGuid == player.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            // ensure auto-attack is turned on
            player.LuaCall(AutoAttackLuaScript);

            // if less than level 13, use spellcasting
            if (player.Level <= 12)
            {
                // if low on mana, move into melee range
                if (player.ManaPercent < 20 && player.Position.DistanceTo(target.Position) > 5)
                {
                    player.MoveToward(target.Position);
                    return;
                }
                else player.StopAllMovement();

                TryCastSpell(Moonfire, 0, 10, !target.HasDebuff(Moonfire));

                TryCastSpell(Wrath, 10, 30);
            }
            // bear form
            else if (player.Level > 12 && player.Level < 20)
            {
                // ensure we're in melee range
                if ((player.Position.DistanceTo(target.Position) > 3 && player.CurrentShapeshiftForm == BearForm && target.IsInCombat && !TargetMovingTowardPlayer) || (!target.IsInCombat && !player.IsCasting))
                {
                    var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                    player.MoveToward(nextWaypoint);
                }
                else
                    player.StopAllMovement();

                TryCastSpell(BearForm, 0, 50, player.CurrentShapeshiftForm != BearForm && Wait.For("BearFormDelay", 1000, true));

                if (ObjectManager.Aggressors.Count() > 1)
                {
                    TryUseBearAbility(DemoralizingRoar, 10, !target.HasDebuff(DemoralizingRoar) && player.CurrentShapeshiftForm == BearForm);
                }

                TryUseBearAbility(Enrage, condition: player.CurrentShapeshiftForm == BearForm);

                TryUseBearAbility(Maul, Math.Max(15 - (player.Level - 9), 10), player.CurrentShapeshiftForm == BearForm);
            }
            // cat form
            else if (player.Level >= 20)
            {
                // ensure we're in melee range
                if ((player.Position.DistanceTo(target.Position) > 3 && player.CurrentShapeshiftForm == CatForm && target.IsInCombat && !TargetMovingTowardPlayer) || (!target.IsInCombat && !player.IsCasting))
                {
                    var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                    player.MoveToward(nextWaypoint);
                }
                else
                    player.StopAllMovement();

                TryCastSpell(CatForm, 0, 50, player.CurrentShapeshiftForm != CatForm);

                TryUseCatAbility(TigersFury, 30, condition: target.HealthPercent > 30 && !player.HasBuff(TigersFury));

                TryUseCatAbility(Rake, 35, condition: target.HealthPercent > 50 && !target.HasDebuff(Rake));

                TryUseCatAbility(Claw, 40);

                //TryUseCatAbility(Rip, 30, true, (target.HealthPercent < 70 && !target.HasDebuff(Rip)));
            }

            targetLastPosition = target.Position;
        }

        /// <summary>
        /// Tries to use a bear ability with the specified name.
        /// </summary>
        void TryUseBearAbility(string name, int requiredRage = 0, bool condition = true, Action callback = null)
        {
            if (player.IsSpellReady(name) && player.Rage >= requiredRage && !player.IsStunned && player.CurrentShapeshiftForm == BearForm && condition)
            {
                player.LuaCall($"CastSpellByName(\"{name}\")");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Tries to use a cat ability with the specified name.
        /// </summary>
        void TryUseCatAbility(string name, int requiredEnergy = 0, bool requiresComboPoints = false, bool condition = true, Action callback = null)
        {
            if (player.IsSpellReady(name) && player.Energy >= requiredEnergy && (!requiresComboPoints || player.ComboPoints > 0) && !player.IsStunned && player.CurrentShapeshiftForm == CatForm && condition)
            {
                player.LuaCall($"CastSpellByName(\"{name}\")");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Casts a spell by name if the spell is ready and the player is not currently casting.
        /// </summary>
        void CastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting)
                player.LuaCall($"CastSpellByName(\"{name}\")");
        }

        /// <summary>
        /// Tries to cast a spell with the given name within a specified range, under certain conditions, and with an optional callback.
        /// </summary>
        void TryCastSpell(string name, int minRange, int maxRange, bool condition = true, Action callback = null)
        {
            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && distanceToTarget >= minRange && distanceToTarget <= maxRange && condition && !player.IsStunned && !player.IsCasting && !player.IsChanneling)
            {
                player.LuaCall($"CastSpellByName(\"{name}\")");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Determines if the target is moving toward the player.
        /// </summary>
        bool TargetMovingTowardPlayer =>
                    targetLastPosition != null &&
                    targetLastPosition.DistanceTo(player.Position) > target.Position.DistanceTo(player.Position);
    }
}
