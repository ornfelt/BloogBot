using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functions related to the combat state of the Protection Warrior Bot.
/// </summary>
namespace ProtectionWarriorBot
{
    /// <summary>
    /// Represents the combat state of the bot.
    /// </summary>
    /// <summary>
    /// Represents the combat state of the bot.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The constant string representing the battle shout.
        /// </summary>
        const string BattleShout = "Battle Shout";
        /// <summary>
        /// Represents the constant string "Berserking".
        /// </summary>
        const string Berserking = "Berserking";
        /// <summary>
        /// Represents the constant string "Bloodrage".
        /// </summary>
        const string Bloodrage = "Bloodrage";
        /// <summary>
        /// Represents the constant string "Concussion Blow".
        /// </summary>
        const string ConcussionBlow = "Concussion Blow";
        /// <summary>
        /// Represents the constant string value for "Demoralizing Shout".
        /// </summary>
        const string DemoralizingShout = "Demoralizing Shout";
        /// <summary>
        /// Represents the constant string "Execute".
        /// </summary>
        const string Execute = "Execute";
        /// <summary>
        /// The constant string representing the name of the Heroic Strike ability.
        /// </summary>
        const string HeroicStrike = "Heroic Strike";
        /// <summary>
        /// Represents the constant string "Last Stand".
        /// </summary>
        const string LastStand = "Last Stand";
        /// <summary>
        /// Represents a constant string with the value "Overpower".
        /// </summary>
        const string Overpower = "Overpower";
        /// <summary>
        /// The constant string value for "Rend".
        /// </summary>
        const string Rend = "Rend";
        /// <summary>
        /// Represents the constant string "Retaliation".
        /// </summary>
        const string Retaliation = "Retaliation";
        /// <summary>
        /// Represents the constant string "Shield Bash".
        /// </summary>
        const string ShieldBash = "Shield Bash";
        /// <summary>
        /// Represents the constant string "Shield Slam".
        /// </summary>
        const string ShieldSlam = "Shield Slam";
        /// <summary>
        /// Represents the constant string "Thunder Clap".
        /// </summary>
        const string ThunderClap = "Thunder Clap";

        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 3)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the character's abilities and performs actions based on the current state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Update -> Update: base.Update()
        /// Update -> TryUseAbility: Bloodrage
        /// Update -> ObjectManager: Aggressors.Count()
        /// ObjectManager --> Update: return count
        /// Update -> TryUseAbility: Retaliation
        /// Update -> ObjectManager: Aggressors.Count()
        /// ObjectManager --> Update: return count
        /// Update -> TryUseAbility: DemoralizingShout
        /// Update -> TryUseAbility: ThunderClap
        /// Update -> TryUseAbility: LastStand
        /// Update -> TryUseAbility: Overpower
        /// Update -> TryUseAbility: Berserking
        /// Update -> TryUseAbility: ShieldBash
        /// Update -> TryUseAbility: Rend
        /// Update -> TryUseAbility: BattleShout
        /// Update -> TryUseAbility: ConcussionBlow
        /// Update -> TryUseAbility: Execute
        /// Update -> TryUseAbility: ShieldSlam
        /// Update -> TryUseAbility: HeroicStrike
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (base.Update())
                return;

            TryUseAbility(Bloodrage, condition: target.HealthPercent > 50);

            if (ObjectManager.Aggressors.Count() >= 3)
            {
                TryUseAbility(Retaliation);
            }
            if (ObjectManager.Aggressors.Count() >= 2 && (!target.HasDebuff(DemoralizingShout) || !target.HasDebuff(ThunderClap)))
            {
                TryUseAbility(DemoralizingShout, 10, !target.HasDebuff(DemoralizingShout) && ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10));

                TryUseAbility(ThunderClap, 20, !target.HasDebuff(ThunderClap) && ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10));
            }
            else if (ObjectManager.Aggressors.Count() == 1 || (target.HasDebuff(DemoralizingShout) && target.HasDebuff(ThunderClap)))
            {
                TryUseAbility(LastStand, condition: player.HealthPercent <= 8);

                TryUseAbility(Overpower, 5, player.CanOverpower);

                TryUseAbility(Berserking, 5, player.HealthPercent < 30);

                TryUseAbility(ShieldBash, 10, target.IsCasting && target.Mana > 0);

                TryUseAbility(Rend, 10, (!target.HasDebuff(Rend) && target.HealthPercent > 50 && (target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead)));

                TryUseAbility(BattleShout, 10, !player.HasBuff(BattleShout));

                TryUseAbility(ConcussionBlow, 15, !target.IsStunned && target.HealthPercent > 40);

                TryUseAbility(Execute, 20, target.HealthPercent < 20);

                TryUseAbility(ShieldSlam, 20, target.HealthPercent > 30);

                TryUseAbility(HeroicStrike, 40, target.HealthPercent > 40 && !player.IsCasting);
            }
        }
    }
}
