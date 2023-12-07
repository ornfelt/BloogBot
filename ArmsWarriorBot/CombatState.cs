using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functions related to the combat state of the Arms Warrior Bot.
/// </summary>
namespace ArmsWarriorBot
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
        /// Array of targets that can be affected by the Sunder ability.
        /// </summary>
        static readonly string[] SunderTargets = { "Snapjaw", "Snapper", "Tortoise", "Spikeshell", "Burrower", "Borer", // turtles
            "Bear", "Grizzly", "Ashclaw", "Mauler", "Shardtooth", "Plaguebear", "Bristlefur", "Thistlefur", // bears
            "Scorpid", "Flayer", "Stinger", "Lasher", "Pincer", // scorpids
            "Crocolisk", "Vicejaw", "Deadmire", "Snapper", "Daggermaw", // crocs
            "Crawler", "Crustacean", // crabs
            "Stag" }; // other

        /// <summary>
        /// The icon path for the Sunder Armor ability.
        /// </summary>
        const string SunderArmorIcon = "Interface\\Icons\\Ability_Warrior_Sunder";

        /// <summary>
        /// The constant string representing the battle shout.
        /// </summary>
        const string BattleShout = "Battle Shout";
        /// <summary>
        /// Represents the constant string "Bloodrage".
        /// </summary>
        const string Bloodrage = "Bloodrage";
        /// <summary>
        /// Represents the constant string "Blood Fury".
        /// </summary>
        const string BloodFury = "Blood Fury";
        /// <summary>
        /// Represents the constant string value for "Demoralizing Shout".
        /// </summary>
        const string DemoralizingShout = "Demoralizing Shout";
        /// <summary>
        /// Represents the constant string "Execute".
        /// </summary>
        const string Execute = "Execute";
        /// <summary>
        /// Represents the constant string "Hamstring".
        /// </summary>
        const string Hamstring = "Hamstring";
        /// <summary>
        /// The constant string representing the name "Heroic Strike".
        /// </summary>
        const string HeroicStrike = "Heroic Strike";
        /// <summary>
        /// Represents the constant string "Mortal Strike".
        /// </summary>
        const string MortalStrike = "Mortal Strike";
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
        /// The constant string representing "Sunder Armor".
        /// </summary>
        const string SunderArmor = "Sunder Armor";
        /// <summary>
        /// Represents the constant string "Sweeping Strikes".
        /// </summary>
        const string SweepingStrikes = "Sweeping Strikes";
        /// <summary>
        /// Represents the constant string "Thunder Clap".
        /// </summary>
        const string ThunderClap = "Thunder Clap";
        /// <summary>
        /// Represents the constant string "Intimidating Shout".
        /// </summary>
        const string IntimidatingShout = "Intimidating Shout";

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
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 5)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the character's abilities and actions during combat.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// ObjectManager -> Update: Get Aggressors
        /// Update -> TryUseAbility: Bloodrage
        /// Update -> TryUseAbilityById: BloodFury
        /// Update -> TryUseAbility: Overpower
        /// Update -> TryUseAbility: Execute
        /// 
        /// alt aggressors.Count() == 1
        ///     Update -> TryUseAbility: Hamstring
        ///     Update -> TryUseAbility: BattleShout
        ///     Update -> TryUseAbility: Rend
        ///     Update -> TryUseAbility: SunderArmor
        ///     Update -> TryUseAbility: MortalStrike
        ///     Update -> TryUseAbility: HeroicStrike
        /// else aggressors.Count() >= 2
        ///     Update -> TryUseAbility: IntimidatingShout
        ///     Update -> TryUseAbility: Retaliation
        ///     Update -> TryUseAbility: DemoralizingShout
        ///     Update -> TryUseAbility: ThunderClap
        ///     Update -> TryUseAbility: SweepingStrikes
        ///     alt thunderClapCondition && demoShoutCondition && sweepingStrikesCondition
        ///         Update -> TryUseAbility: Rend
        ///         Update -> TryUseAbility: MortalStrike
        ///         Update -> TryUseAbility: HeroicStrike
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (base.Update())
                return;

            var aggressors = ObjectManager.Aggressors.ToList();

            // Use these abilities when fighting any number of mobs.   
            TryUseAbility(Bloodrage, condition: target.HealthPercent > 50);

            TryUseAbilityById(BloodFury, 4, condition: target.HealthPercent > 80);

            TryUseAbility(Overpower, 5, player.CanOverpower);

            TryUseAbility(Execute, 15, target.HealthPercent < 20);

            // Use these abilities if you are fighting exactly one mob.
            if (aggressors.Count() == 1)
            {
                TryUseAbility(Hamstring, 10, (target.Name.Contains("Plainstrider") || target.CreatureType == CreatureType.Humanoid) && target.HealthPercent < 30 && !target.HasDebuff(Hamstring));

                TryUseAbility(BattleShout, 10, !player.HasBuff(BattleShout));

                TryUseAbility(Rend, 10, target.HealthPercent > 50 && !target.HasDebuff(Rend) && target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead);

                var sunderDebuff = target.GetDebuffs(LuaTarget.Target).FirstOrDefault(f => f.Icon == SunderArmorIcon);
                TryUseAbility(SunderArmor, 15, (sunderDebuff == null || sunderDebuff.StackCount < 5) && target.Level >= player.Level - 2 && target.Health > 40 && SunderTargets.Any(s => target.Name.Contains(s)));

                TryUseAbility(MortalStrike, 30);

                TryUseAbility(HeroicStrike, player.Level < 30 ? 15 : 45, target.HealthPercent > 30);
            }

            // Use these abilities if you are fighting TWO OR MORE mobs at once.
            if (aggressors.Count() >= 2)
            {
                TryUseAbility(IntimidatingShout, 25, !(target.HasDebuff(IntimidatingShout) || player.HasBuff(Retaliation)) && aggressors.All(a => a.Position.DistanceTo(player.Position) < 10) && !ObjectManager.Units.Any(u => u.Guid != target.Guid && u.Position.DistanceTo(player.Position) < 10 && u.UnitReaction == UnitReaction.Neutral));

                TryUseAbility(Retaliation, 0, player.IsSpellReady(Retaliation) && ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10) && !ObjectManager.Aggressors.Any(a => a.HasDebuff(IntimidatingShout)));

                TryUseAbility(DemoralizingShout, 10, aggressors.Any(a => !a.HasDebuff(DemoralizingShout) && a.HealthPercent > 50) && aggressors.All(a => a.Position.DistanceTo(player.Position) < 10) && (!player.IsSpellReady(IntimidatingShout) || player.HasBuff(Retaliation)) && !ObjectManager.Units.Any(u => (u.Guid != target.Guid && u.Position.DistanceTo(player.Position) < 10 && u.UnitReaction == UnitReaction.Neutral) || u.HasDebuff(IntimidatingShout)));

                TryUseAbility(ThunderClap, 20, aggressors.Any(a => !a.HasDebuff(ThunderClap) && a.HealthPercent > 50) && aggressors.All(a => a.Position.DistanceTo(player.Position) < 8) && (!player.IsSpellReady(IntimidatingShout) || player.HasBuff(Retaliation)) && !ObjectManager.Units.Any(u => (u.Guid != target.Guid && u.Position.DistanceTo(player.Position) < 8 && u.UnitReaction == UnitReaction.Neutral) || u.HasDebuff(IntimidatingShout)));

                TryUseAbility(SweepingStrikes, 30, !player.HasBuff(SweepingStrikes) && target.HealthPercent > 30);

                var thunderClapCondition = target.HasDebuff(ThunderClap) || !player.KnowsSpell(ThunderClap) || target.HealthPercent < 50;
                var demoShoutCondition = target.HasDebuff(DemoralizingShout) || !player.KnowsSpell(DemoralizingShout) || target.HealthPercent < 50;
                var sweepingStrikesCondition = player.HasBuff(SweepingStrikes) || !player.IsSpellReady(SweepingStrikes);
                if (thunderClapCondition && demoShoutCondition && sweepingStrikesCondition)
                {
                    TryUseAbility(Rend, 10, target.HealthPercent > 50 && !target.HasDebuff(Rend) && target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead);

                    TryUseAbility(MortalStrike, 30);

                    TryUseAbility(HeroicStrike, player.Level < 30 ? 15 : 45, target.HealthPercent > 30);
                }
            }
        }
    }
}
