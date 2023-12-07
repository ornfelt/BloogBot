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
/// This namespace contains the classes and functions related to the combat state of the Balance Druid Bot.
/// </summary>
namespace BalanceDruidBot
{
    /// <summary>
    /// Represents the constant for the ability "Abolish Poison".
    /// </summary>
    /// <summary>
    /// Represents the constant for the ability "Abolish Poison".
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// An array of strings representing abilities that are immune to nature damage.
        /// </summary>
        static readonly string[] ImmuneToNatureDamage = { "Vortex", "Whirlwind", "Whirling", "Dust", "Cyclone" };

        /// <summary>
        /// Represents the constant string "Abolish Poison".
        /// </summary>
        const string AbolishPoison = "Abolish Poison";
        /// <summary>
        /// Represents the constant string "Entangling Roots".
        /// </summary>
        const string EntanglingRoots = "Entangling Roots";
        /// <summary>
        /// Represents the constant string "Healing Touch".
        /// </summary>
        const string HealingTouch = "Healing Touch";
        /// <summary>
        /// Represents the constant string "Moonfire".
        /// </summary>
        const string Moonfire = "Moonfire";
        /// <summary>
        /// The constant string representing "Rejuvenation".
        /// </summary>
        const string Rejuvenation = "Rejuvenation";
        /// <summary>
        /// Represents the constant string "Remove Curse".
        /// </summary>
        const string RemoveCurse = "Remove Curse";
        /// <summary>
        /// Represents the constant string "Wrath".
        /// </summary>
        const string Wrath = "Wrath";
        /// <summary>
        /// Represents the constant string "Insect Swarm".
        /// </summary>
        const string InsectSwarm = "Insect Swarm";
        /// <summary>
        /// Represents the constant string "Innervate".
        /// </summary>
        const string Innervate = "Innervate";
        /// <summary>
        /// Represents the constant string "Moonkin Form".
        /// </summary>
        const string MoonkinForm = "Moonkin Form";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Represents a secondary target in the World of Warcraft game.
        /// </summary>
        WoWUnit secondaryTarget;

        /// <summary>
        /// Represents a boolean value indicating whether casting Entangling Roots is possible.
        /// </summary>
        bool castingEntanglingRoots;
        /// <summary>
        /// Represents a boolean value indicating whether backpedaling is occurring.
        /// </summary>
        bool backpedaling;
        /// <summary>
        /// Represents the start time of the backpedal.
        /// </summary>
        int backpedalStartTime;

        /// <summary>
        /// Sets the castingEntanglingRoots flag to true.
        /// </summary>
        Action EntanglingRootsCallback => () =>
                {
                    castingEntanglingRoots = true;
                };

        /// <summary>
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 30)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the state of the character.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Update -> castingEntanglingRoots: Check if castingEntanglingRoots is true
        /// castingEntanglingRoots -> secondaryTarget: Check if secondaryTarget has EntanglingRoots debuff
        /// castingEntanglingRoots -> player: Start backpedaling
        /// castingEntanglingRoots -> player: Set target
        /// castingEntanglingRoots -> castingEntanglingRoots: Set to false
        /// Update -> backpedaling: Check if backpedaling time is over 1500ms
        /// backpedaling -> player: Stop backpedaling
        /// backpedaling -> backpedaling: Set to false
        /// Update -> player: Check if player's health is below 30% and has enough mana for healing
        /// player -> botStates: Push new HealSelfState
        /// Update -> base: Call base Update method
        /// Update -> ObjectManager: Check if there are 2 aggressors and secondaryTarget is null
        /// ObjectManager -> secondaryTarget: Set secondaryTarget
        /// Update -> secondaryTarget: Check if secondaryTarget has no EntanglingRoots debuff
        /// secondaryTarget -> player: Set target and try to cast EntanglingRoots
        /// Update -> player: Try to cast various spells based on conditions
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (castingEntanglingRoots)
            {
                if (secondaryTarget.HasDebuff(EntanglingRoots))
                {
                    backpedaling = true;
                    backpedalStartTime = Environment.TickCount;
                    player.StartMovement(ControlBits.Back);
                }

                player.SetTarget(target.Guid);
                player.Target = target;
                castingEntanglingRoots = false;
            }

            // handle backpedaling during entangling roots
            if (Environment.TickCount - backpedalStartTime > 1500)
            {
                player.StopMovement(ControlBits.Back);
                backpedaling = false;
            }
            if (backpedaling)
                return;

            // heal self if we're injured
            if (player.HealthPercent < 30 && (player.Mana >= player.GetManaCost(HealingTouch) || player.Mana >= player.GetManaCost(Rejuvenation)))
            {
                Wait.RemoveAll();
                botStates.Push(new HealSelfState(botStates, target));
                return;
            }

            if (base.Update())
                return;

            // if we get an add, root it with Entangling Roots
            if (ObjectManager.Aggressors.Count() == 2 && secondaryTarget == null)
                secondaryTarget = ObjectManager.Aggressors.Single(u => u.Guid != target.Guid);

            if (secondaryTarget != null && !secondaryTarget.HasDebuff(EntanglingRoots))
            {
                player.SetTarget(secondaryTarget.Guid);
                player.Target = secondaryTarget;
                TryCastSpell(EntanglingRoots, 0, 30, !secondaryTarget.HasDebuff(EntanglingRoots), EntanglingRootsCallback);
            }

            TryCastSpell(MoonkinForm, !player.HasBuff(MoonkinForm));

            TryCastSpell(Innervate, player.ManaPercent < 10, castOnSelf: true);

            TryCastSpell(RemoveCurse, 0, int.MaxValue, player.IsCursed && !player.HasBuff(MoonkinForm), castOnSelf: true);

            TryCastSpell(AbolishPoison, 0, int.MaxValue, player.IsPoisoned && !player.HasBuff(MoonkinForm), castOnSelf: true);

            TryCastSpell(InsectSwarm, 0, 30, !target.HasDebuff(InsectSwarm) && target.HealthPercent > 20 && !ImmuneToNatureDamage.Any(s => target.Name.Contains(s)));

            TryCastSpell(Moonfire, 0, 30, !target.HasDebuff(Moonfire));

            TryCastSpell(Wrath, 0, 30, !ImmuneToNatureDamage.Any(s => target.Name.Contains(s)));
        }
    }
}
