using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using BloogBot.AI.SharedStates;
using BloogBot.Game.Enums;

/// <summary>
/// This namespace contains the implementation of the PowerlevelCombatState class which represents the state of the bot when in combat and focusing on power leveling.
/// </summary>
namespace ShadowPriestBot
{
    /// <summary>
    /// Represents the combat state of a bot with powerleveling capabilities.
    /// </summary>
    class PowerlevelCombatState : IBotState
    {
        /// <summary>
        /// The Lua script for auto attacking. If the current action is not '12', it casts the 'Attack' spell.
        /// </summary>
        const string AutoAttackLuaScript = "if IsCurrentAction('12') == nil then CastSpellByName('Attack') end";
        /// <summary>
        /// Represents the error message for when the target is not in line of sight.
        /// </summary>
        const string LosErrorMessage = "Target not in line of sight";
        /// <summary>
        /// The Lua script for casting the "Shoot" spell if the action with ID 11 is not set to auto-repeat.
        /// </summary>
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";
        /// <summary>
        /// Lua script to turn off the wand if the action is set to auto-repeat.
        /// </summary>
        const string TurnOffWandLuaScript = "if IsAutoRepeatAction(11) ~= nil then CastSpellByName('Shoot') end";

        /// <summary>
        /// Represents the constant string "Abolish Disease".
        /// </summary>
        const string AbolishDisease = "Abolish Disease";
        /// <summary>
        /// Represents the constant string "Cure Disease".
        /// </summary>
        const string CureDisease = "Cure Disease";
        /// <summary>
        /// Represents the constant string "Dispel Magic".
        /// </summary>
        const string DispelMagic = "Dispel Magic";
        /// <summary>
        /// Represents the constant string "Inner Fire".
        /// </summary>
        const string InnerFire = "Inner Fire";
        /// <summary>
        /// Represents the constant string "Lesser Heal".
        /// </summary>
        const string LesserHeal = "Lesser Heal";
        /// <summary>
        /// Represents the constant string "Mind Blast".
        /// </summary>
        const string MindBlast = "Mind Blast";
        /// <summary>
        /// Represents the constant string "Mind Flay".
        /// </summary>
        const string MindFlay = "Mind Flay";
        /// <summary>
        /// Represents the constant string "Power Word: Shield".
        /// </summary>
        const string PowerWordShield = "Power Word: Shield";
        /// <summary>
        /// Represents the constant string "Psychic Scream".
        /// </summary>
        const string PsychicScream = "Psychic Scream";
        /// <summary>
        /// The constant string representing "Shadowform".
        /// </summary>
        const string ShadowForm = "Shadowform";
        /// <summary>
        /// Represents the constant string "Shadow Word: Pain".
        /// </summary>
        const string ShadowWordPain = "Shadow Word: Pain";
        /// <summary>
        /// Represents the constant string "Smite".
        /// </summary>
        const string Smite = "Smite";
        /// <summary>
        /// Represents the constant string "Vampiric Embrace".
        /// </summary>
        const string VampiricEmbrace = "Vampiric Embrace";
        /// <summary>
        /// Represents the constant string "Weakened Soul".
        /// </summary>
        const string WeakenedSoul = "Weakened Soul";

        /// <summary>
        /// Represents a boolean value indicating whether there are any line of sight (LOS) obstacles.
        /// </summary>
        bool noLos;
        /// <summary>
        /// Represents the start time for the "no loss" period.
        /// </summary>
        int noLosStartTime;

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
        /// Initializes a new instance of the PowerlevelCombatState class.
        /// </summary>
        public PowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            this.powerlevelTarget = powerlevelTarget;
            player = ObjectManager.Player;

            WoWEventHandler.OnErrorMessage += OnErrorMessageCallback;
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
        /// Update -> Environment: TickCount
        /// Update -> player: StopAllMovement
        /// Update -> Navigation: GetNextWaypoint
        /// Update -> player: MoveToward
        /// Update -> botStates: Push(new HealSelfState)
        /// Update -> botStates: Pop
        /// Update -> ObjectManager: Units.FirstOrDefault
        /// Update -> botStates: Pop
        /// Update -> botStates: Push(new LootState)
        /// Update -> player: SetTarget
        /// Update -> player: IsFacing
        /// Update -> player: Face
        /// Update -> player: MoveToward
        /// Update -> player: StopAllMovement
        /// Update -> Inventory: GetEquippedItem
        /// Update -> player: LuaCall
        /// Update -> ObjectManager: Aggressors
        /// Update -> TryCastSpell: ShadowForm
        /// Update -> TryCastSpell: VampiricEmbrace
        /// Update -> TryCastSpell: PsychicScream
        /// Update -> TryCastSpell: ShadowWordPain
        /// Update -> TryCastSpell: DispelMagic
        /// Update -> TryCastSpell: AbolishDisease
        /// Update -> TryCastSpell: CureDisease
        /// Update -> TryCastSpell: InnerFire
        /// Update -> TryCastSpell: PowerWordShield
        /// Update -> TryCastSpell: MindBlast
        /// Update -> TryCastSpell: MindFlay
        /// Update -> TryCastSpell: Smite
        /// Update -> player: SetTarget
        /// Update -> TryCastSpell: LesserHeal
        /// \enduml
        /// </remarks>
        public void Update()
        {
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

            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(LesserHeal))
            {
                botStates.Push(new HealSelfState(botStates, container));
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
            if (target.Health == 0 || target.TappedByOther || checkTarget == null)
            {
                const string waitKey = "PopCombatState";

                if (Wait.For(waitKey, 1500))
                {
                    botStates.Pop();
                    botStates.Push(new LootState(botStates, container, target));
                    Wait.Remove(waitKey);
                }

                return;
            }

            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            // make sure we get into mind flay range for casters
            if ((target.IsCasting || target.IsChanneling) && player.Position.DistanceTo(target.Position) > 19)
                player.MoveToward(target.Position);
            else if (player.IsMoving)
                player.StopAllMovement();

            var hasWand = Inventory.GetEquippedItem(EquipSlot.Ranged) != null;

            // ensure auto-attack is turned on only if we don't have a wand
            if (!hasWand)
                player.LuaCall(AutoAttackLuaScript);

            // ----- COMBAT ROTATION -----
            var useWand = (hasWand && player.ManaPercent <= 10 && !player.IsCasting && !player.IsChanneling) || target.CreatureType == CreatureType.Totem;
            if (useWand)
                player.LuaCall(WandLuaScript);
            else
            {
                var aggressors = ObjectManager.Aggressors;

                TryCastSpell(ShadowForm, 0, int.MaxValue, !player.HasBuff(ShadowForm));

                TryCastSpell(VampiricEmbrace, 0, 29, player.HealthPercent < 100 && !target.HasDebuff(VampiricEmbrace) && target.HealthPercent > 50);

                var noNeutralsNearby = !ObjectManager.Units.Any(u => u.Guid != target.Guid && u.UnitReaction == UnitReaction.Neutral && u.Position.DistanceTo(player.Position) <= 10);
                TryCastSpell(PsychicScream, 0, 7, (target.Position.DistanceTo(player.Position) < 8 && !player.HasBuff(PowerWordShield)) || ObjectManager.Aggressors.Count() > 1 && target.CreatureType != CreatureType.Elemental);

                TryCastSpell(ShadowWordPain, 0, 29, target.HealthPercent > 70 && !target.HasDebuff(ShadowWordPain));

                TryCastSpell(DispelMagic, 0, int.MaxValue, player.HasMagicDebuff, castOnSelf: true);

                if (player.KnowsSpell(AbolishDisease))
                    TryCastSpell(AbolishDisease, 0, int.MaxValue, player.IsDiseased && !player.HasBuff(ShadowForm), castOnSelf: true);
                else if (player.KnowsSpell(CureDisease))
                    TryCastSpell(CureDisease, 0, int.MaxValue, player.IsDiseased && !player.HasBuff(ShadowForm), castOnSelf: true);

                TryCastSpell(InnerFire, 0, int.MaxValue, !player.HasBuff(InnerFire));

                TryCastSpell(PowerWordShield, 0, int.MaxValue, !player.HasDebuff(WeakenedSoul) && !player.HasBuff(PowerWordShield) && (target.HealthPercent > 20 || player.HealthPercent < 10), castOnSelf: true);

                TryCastSpell(MindBlast, 0, 29);

                if (player.KnowsSpell(MindFlay) && target.Position.DistanceTo(player.Position) <= 19 && (!player.KnowsSpell(PowerWordShield) || player.HasBuff(PowerWordShield)))
                    TryCastSpell(MindFlay, 0, 19);
                else
                    TryCastSpell(Smite, 0, 29, !player.HasBuff(ShadowForm));

                if (powerlevelTarget.HealthPercent < 50)
                {
                    player.SetTarget(powerlevelTarget.Guid);
                    TryCastSpell(LesserHeal, 0, 40, player.Mana > player.GetManaCost(LesserHeal));
                }
            }
        }

        /// <summary>
        /// Tries to cast a spell with the given name within a specified range, under certain conditions, and with optional callback and self-casting options.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// TryCastSpell -> player: Get player position
        /// player -> target: Get target position
        /// player -> player: Check if spell is ready
        /// player -> player: Check if player has enough mana
        /// player -> player: Check if distance to target is within range
        /// player -> player: Check if condition is true
        /// player -> player: Check if player is not stunned
        /// player -> player: Check if player is not casting
        /// player -> player: Check if player is not channeling
        /// player -> player: Cast spell by name
        /// player -> callback: Invoke callback function
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// Participant sender as Sender
        /// Participant e as OnUiMessageArgs
        /// Participant this as EventHandler
        /// 
        /// sender -> this: Event Triggered
        /// alt e.Message == LosErrorMessage
        ///     this -> this: Set noLos = true
        ///     this -> this: Record noLosStartTime
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
    }
}
