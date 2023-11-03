using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the implementation of the PowerlevelCombatState class, which represents the combat state for a Balance Druid bot.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// Represents the power level combat state for the bot.
    /// </summary>
    /// <summary>
    /// Represents the power level combat state for the bot.
    /// </summary>
    class PowerlevelCombatState : IBotState
    {
        /// <summary>
        /// Represents the error message for when the target is not in line of sight.
        /// </summary>
        const string LosErrorMessage = "Target not in line of sight";

        /// <summary>
        /// The Lua script for auto attacking. If the current action is not '12', it casts the 'Attack' spell.
        /// </summary>
        const string AutoAttackLuaScript = "if IsCurrentAction('12') == nil then CastSpellByName('Attack') end";

        /// <summary>
        /// Represents the constant string "Healing Touch".
        /// </summary>
        const string HealingTouch = "Healing Touch";
        /// <summary>
        /// Represents the constant string "Mark of the Wild".
        /// </summary>
        const string MarkOfTheWild = "Mark of the Wild";
        /// <summary>
        /// Represents the constant string "Moonfire".
        /// </summary>
        const string Moonfire = "Moonfire";
        /// <summary>
        /// Represents the constant string "Regrowth".
        /// </summary>
        const string Regrowth = "Regrowth";
        /// <summary>
        /// The constant string representing "Rejuvenation".
        /// </summary>
        const string Rejuvenation = "Rejuvenation";
        /// <summary>
        /// Represents a constant string with the value "Thorns".
        /// </summary>
        const string Thorns = "Thorns";
        /// <summary>
        /// Represents the constant string "Wrath".
        /// </summary>
        const string Wrath = "Wrath";

        /// <summary>
        /// The constant integer value representing the range of a spell.
        /// </summary>
        const int spellRange = 29;

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
        /// Represents a World of Warcraft player who is the target of power leveling.
        /// </summary>
        readonly WoWPlayer powerlevelTarget;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Represents a boolean value indicating whether there are any line of sight (LOS) obstacles.
        /// </summary>
        bool noLos;
        /// <summary>
        /// Represents the start time for the "no loss" period.
        /// </summary>
        int noLosStartTime;

        /// <summary>
        /// Initializes a new instance of the PowerlevelCombatState class.
        /// </summary>
        public PowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            this.powerlevelTarget = powerlevelTarget;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the behavior of the player character.
        /// </summary>
        public void Update()
        {
            // handle no los with target
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

            // heal self if we're injured
            if (player.HealthPercent < 30 && (player.Mana >= player.GetManaCost(HealingTouch) || player.Mana >= player.GetManaCost(Rejuvenation)))
            {
                botStates.Push(new HealSelfState(botStates, target));
                return;
            }

            // pop state when the target is dead
            if (target.Health == 0)
            {
                botStates.Pop();

                if (player.ManaPercent < 20)
                    botStates.Push(new RestState(botStates, container));

                return;
            }

            // make sure the target is set (sometimes it inexplicably gets unset)
            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            // ensure auto-attack is turned on
            player.LuaCall(AutoAttackLuaScript);

            // ensure we're in casting range, or melee range if oom
            if (player.Position.DistanceTo(target.Position) > 29 || (player.Mana < player.GetManaCost(Wrath) && player.Position.DistanceTo(target.Position) > 4))
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
            else
                player.StopAllMovement();

            // combat rotation
            TryCastSpell(Regrowth, 0, 29, powerlevelTarget.HealthPercent < 40);

            if (!powerlevelTarget.HasBuff(MarkOfTheWild))
            {
                player.SetTarget(powerlevelTarget.Guid);
                TryCastSpell(MarkOfTheWild, 0, 29);
            }

            if (!powerlevelTarget.HasBuff(Thorns))
            {
                player.SetTarget(powerlevelTarget.Guid);
                TryCastSpell(Thorns, 0, 29);
            }

            TryCastSpell(Moonfire, 0, 29, !target.HasDebuff(Moonfire));

            TryCastSpell(Wrath, 0, spellRange);
        }

        /// <summary>
        /// Tries to cast a spell with the given name within a specified range, under certain conditions, and with optional callback and self-casting options.
        /// </summary>
        void TryCastSpell(string name, int minRange, int maxRange, bool condition = true, Action callback = null, bool castOnSelf = false)
        {
            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && distanceToTarget >= minRange && distanceToTarget <= maxRange && condition && !player.IsStunned && !player.IsCasting && !player.IsChanneling)
            {
                var castOnSelfString = castOnSelf ? ",1" : "";
                player.LuaCall($"CastSpellByName(\"{name}\"{castOnSelfString})");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Event handler for error messages.
        /// </summary>
        void OnErrorMessageCallback(object sender, OnUiMessageArgs e)
        {
            if (e.Message == LosErrorMessage)
            {
                noLos = true;
                noLosStartTime = Environment.TickCount;
            }
        }
    }
}
