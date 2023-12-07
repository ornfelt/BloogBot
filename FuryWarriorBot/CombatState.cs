using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and interfaces related to the Fury Warrior Bot.
/// </summary>
namespace FuryWarriorBot
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
        /// The constant string representing the battle stance.
        /// </summary>
        const string BattleStance = "Battle Stance";
        /// <summary>
        /// Represents the constant string value for "Berserker Stance".
        /// </summary>
        const string BerserkerStance = "Berserker Stance";

        /// <summary>
        /// The constant string representing the battle shout.
        /// </summary>
        const string BattleShout = "Battle Shout";
        /// <summary>
        /// Represents the constant string "Berserker Rage".
        /// </summary>
        const string BerserkerRage = "Berserker Rage";
        /// <summary>
        /// Represents the constant string "Berserking".
        /// </summary>
        const string Berserking = "Berserking";
        /// <summary>
        /// Represents the constant string "Blood Fury".
        /// </summary>
        const string BloodFury = "Blood Fury";
        /// <summary>
        /// Represents the constant string "Bloodrage".
        /// </summary>
        const string Bloodrage = "Bloodrage";
        /// <summary>
        /// Represents the constant string "Bloodthirst".
        /// </summary>
        const string Bloodthirst = "Bloodthirst";
        /// <summary>
        /// Represents a constant string named "Cleave".
        /// </summary>
        const string Cleave = "Cleave";
        /// <summary>
        /// Represents the constant string "Death Wish".
        /// </summary>
        const string DeathWish = "Death Wish";
        /// <summary>
        /// Represents the constant string value for "Demoralizing Shout".
        /// </summary>
        const string DemoralizingShout = "Demoralizing Shout";
        /// <summary>
        /// Represents the constant string "Execute".
        /// </summary>
        const string Execute = "Execute";
        /// <summary>
        /// The constant string representing the name "Heroic Strike".
        /// </summary>
        const string HeroicStrike = "Heroic Strike";
        /// <summary>
        /// Represents a constant string with the value "Overpower".
        /// </summary>
        const string Overpower = "Overpower";
        /// <summary>
        /// Represents the constant string "Pummel".
        /// </summary>
        const string Pummel = "Pummel";
        /// <summary>
        /// The constant string value for "Rend".
        /// </summary>
        const string Rend = "Rend";
        /// <summary>
        /// Represents the constant string "Retaliation".
        /// </summary>
        const string Retaliation = "Retaliation";
        /// <summary>
        /// Represents a constant string with the value "Slam".
        /// </summary>
        const string Slam = "Slam";
        /// <summary>
        /// The constant string representing "Sunder Armor".
        /// </summary>
        const string SunderArmor = "Sunder Armor";
        /// <summary>
        /// Represents the constant string "Thunder Clap".
        /// </summary>
        const string ThunderClap = "Thunder Clap";
        /// <summary>
        /// Represents a constant string value for "Ham String".
        /// </summary>
        const string Hamstring = "Ham String";
        /// <summary>
        /// Represents the constant string "Intimidating Shout".
        /// </summary>
        const string IntimidatingShout = "Intimidating Shout";
        /// <summary>
        /// Represents a constant string named "Whirlwind".
        /// </summary>
        const string Whirlwind = "Whirlwind";

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
        /// Represents a boolean value indicating whether the slam is ready.
        /// </summary>
        bool slamReady;
        /// <summary>
        /// Represents the start time for when the slam is ready.
        /// </summary>
        int slamReadyStartTime;

        /// <summary>
        /// Represents a boolean value indicating whether the character is backpedaling.
        /// </summary>
        bool losBackpedaling;
        /// <summary>
        /// Represents the start time of the LOS backpedal.
        /// </summary>
        int losBackpedalStartTime;

        /// <summary>
        /// Represents a boolean value indicating whether backpedaling is occurring.
        /// </summary>
        bool backpedaling;
        /// <summary>
        /// Represents the start time of the backpedal.
        /// </summary>
        int backpedalStartTime;
        /// <summary>
        /// The duration of the backpedal.
        /// </summary>
        int backpedalDuration;

        /// <summary>
        /// Represents a boolean value indicating whether the object has been initialized.
        /// </summary>
        bool initialized;
        /// <summary>
        /// Gets the current tick count of the system when entering the combat state.
        /// </summary>
        int combatStateEnterTime = Environment.TickCount;

        /// <summary>
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 5)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;

            WoWEventHandler.OnSlamReady += OnSlamReadyCallback;
        }

        /// <summary>
        /// Destructor for the CombatState class. Unsubscribes the OnSlamReadyCallback from the WoWEventHandler's OnSlamReady event.
        /// </summary>
        ~CombatState()
        {
            WoWEventHandler.OnSlamReady -= OnSlamReadyCallback;
        }

        /// <summary>
        /// Updates the player's actions and abilities based on the current game state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Environment: TickCount
        /// Update -> player: StopMovement(ControlBits.Back)
        /// Update -> player: CurrentStance
        /// Update -> ObjectManager: Aggressors
        /// Update -> TryUseAbility: BerserkerStance
        /// Update -> TryUseAbility: Pummel
        /// Update -> TryUseAbility: DeathWish
        /// Update -> TryUseAbility: BattleShout
        /// Update -> TryUseAbilityById: BloodFury
        /// Update -> TryUseAbility: Bloodrage
        /// Update -> TryUseAbility: Execute
        /// Update -> TryUseAbility: BerserkerRage
        /// Update -> TryUseAbility: Overpower
        /// Update -> ObjectManager: Aggressors.Count()
        /// Update -> TryUseAbility: IntimidatingShout
        /// Update -> TryUseAbility: DemoralizingShout
        /// Update -> TryUseAbility: Whirlwind
        /// Update -> TryUseAbility: Retaliation
        /// Update -> TryUseAbility: Slam
        /// Update -> TryUseAbility: Bloodthirst
        /// Update -> TryUseAbility: Hamstring
        /// Update -> TryUseAbility: HeroicStrike
        /// Update -> TryUseAbility: Execute
        /// Update -> TryUseAbility: SunderArmor
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (Environment.TickCount - backpedalStartTime > backpedalDuration)
            {
                player.StopMovement(ControlBits.Back);
                // player.StopMovement(ControlBits.StrafeLeft);
                // player.StopMovement(ControlBits.Right);
                backpedaling = false;
            }

            if (backpedaling)
                return;

            if (Environment.TickCount - slamReadyStartTime > 250)
            {
                slamReady = false;
            }

            //if (!FacingAllTargets && ObjectManager.Aggressors.Count() >= 2 && AggressorsInMelee)
            //{
            //    WalkBack(50);
            //    return;
            //}

            if (base.Update())
                return;

            var currentStance = player.CurrentStance;
            var spellcastingAggressors = ObjectManager.Aggressors
                .Where(a => a.Mana > 0);
            // Use these abilities when fighting any number of mobs.   
            TryUseAbility(BerserkerStance, condition: player.Level >= 30 && currentStance == BattleStance && (target.HasDebuff(Rend) || target.HealthPercent < 80 || target.CreatureType == CreatureType.Elemental || target.CreatureType == CreatureType.Undead));

            TryUseAbility(Pummel, 10, currentStance == BerserkerStance && target.Mana > 0 && (target.IsCasting || target.IsChanneling));

            // TryUseAbility(Rend, 10, (currentStance == BattleStance && target.HealthPercent > 50 && !target.HasDebuff(Rend) && (target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead)));

            TryUseAbility(DeathWish, 10, player.IsSpellReady(DeathWish) && target.HealthPercent > 80);

            TryUseAbility(BattleShout, 10, !player.HasBuff(BattleShout));

            TryUseAbilityById(BloodFury, 4, 0, target.HealthPercent > 80);

            TryUseAbility(Bloodrage, condition: target.HealthPercent > 50);

            TryUseAbility(Execute, 15, target.HealthPercent < 20);

            TryUseAbility(BerserkerRage, condition: target.HealthPercent > 70 && currentStance == BerserkerStance);

            TryUseAbility(Overpower, 5, currentStance == BattleStance && player.CanOverpower);

            // Use these abilities if you are fighting TWO OR MORE mobs at once.
            if (ObjectManager.Aggressors.Count() >= 2)
            {
                TryUseAbility(IntimidatingShout, 25, !(target.HasDebuff(IntimidatingShout) || player.HasBuff(Retaliation)) && ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10));

                TryUseAbility(DemoralizingShout, 10, !target.HasDebuff(DemoralizingShout));

                // TryUseAbility(Cleave, 20, target.HealthPercent > 20 && FacingAllTargets);

                TryUseAbility(Whirlwind, 25, target.HealthPercent > 20 && currentStance == BerserkerStance && !target.HasDebuff(IntimidatingShout) && AggressorsInMelee);

                // if our target uses melee, but there's a caster attacking us, do not use retaliation
                TryUseAbility(Retaliation, 0, player.IsSpellReady(Retaliation) && spellcastingAggressors.Count() == 0 && currentStance == BattleStance && FacingAllTargets && !ObjectManager.Aggressors.Any(a => a.HasDebuff(IntimidatingShout)));
            }

            // Use these abilities if you are fighting only one mob at a time, or multiple and one or more are not in melee range.
            if (ObjectManager.Aggressors.Count() >= 1 || (ObjectManager.Aggressors.Count() > 1 && !AggressorsInMelee))
            {
                TryUseAbility(Slam, 15, target.HealthPercent > 20 && slamReady, SlamCallback);

                // TryUseAbility(Rend, 10, (currentStance == BattleStance && target.HealthPercent > 50 && !target.HasDebuff(Rend) && (target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead)));

                TryUseAbility(Bloodthirst, 30);

                TryUseAbility(Hamstring, 10, target.CreatureType == CreatureType.Humanoid && !target.HasDebuff(Hamstring));

                TryUseAbility(HeroicStrike, player.Level < 30 ? 15 : 45, target.HealthPercent > 30);

                TryUseAbility(Execute, 15, target.HealthPercent < 20);

                TryUseAbility(SunderArmor, 15, target.HealthPercent < 80 && !target.HasDebuff(SunderArmor));
            }
        }

        /// <summary>
        /// Callback method for when the slam is ready.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// object -> OnSlamReadyCallback : EventArgs e
        /// OnSlamReadyCallback -> OnSlamReady : 
        /// \enduml
        /// </remarks>
        void OnSlamReadyCallback(object sender, EventArgs e)
        {
            OnSlamReady();
        }

        /// <summary>
        /// Sets the slamReady flag to true and records the start time.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "OnSlamReady Method" as O
        /// participant "Environment" as E
        /// 
        /// O -> E: Get TickCount
        /// E --> O: Return TickCount
        /// 
        /// \enduml
        /// </remarks>
        void OnSlamReady()
        {
            slamReady = true;
            slamReadyStartTime = Environment.TickCount;
        }

        /// <summary>
        /// Callback function for Slam. Sets the slamReady flag to false.
        /// </summary>
        /// <remarks>
        /// \startuml
        ///  :SlamCallback() is called;
        ///  note right: slamReady is set to false
        /// \enduml
        /// </remarks>
        void SlamCallback()
        {
            slamReady = false;
        }

        /// <summary>
        /// Check if the player is facing all the targets and they are within melee range.
        /// </summary>
        // Check to see if toon is facing all the targets and they are within melee, used to determine if player should walkbackwards to reposition targets in front of mob.
        bool FacingAllTargets
        {
            get
            {
                return ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 7 && player.IsInCleave(a.Position));
            }
        }

        /// <summary>
        /// Check to see if toon is within melee distance of mobs. This is used to determine if player should use single mob rotation or multi-mob rotation.
        /// </summary>
        // Check to see if toon is with melee distance of mobs.  This is used to determine if player should use single mob rotation or multi-mob rotation.
        bool AggressorsInMelee
        {
            get
            {
                return ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 7);
            }
        }

        /// <summary>
        /// Sets the player to walk backwards for a specified duration in milliseconds.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// activate "WalkBack"
        /// "WalkBack" -> "Environment": TickCount
        /// "WalkBack" -> "Player": StartMovement(ControlBits.Back)
        /// deactivate "WalkBack"
        /// \enduml
        /// </remarks>
        void WalkBack(int milleseconds)
        {
            backpedaling = true;
            backpedalStartTime = Environment.TickCount;
            backpedalDuration = milleseconds;
            player.StartMovement(ControlBits.Back);
            // player.StartMovement(ControlBits.StrafeLeft);
            // player.StartMovement(ControlBits.Right);
        }
    }
}
