using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the implementation of the PowerlevelCombatState class, which represents the state of the bot when in powerlevel combat mode.
/// </summary>
namespace ElementalShamanBot
{
    /// <summary>
    /// Represents the combat state for a character with a power level.
    /// </summary>
    /// <summary>
    /// Represents the combat state for a character with a power level.
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
        /// Represents the constant string "Clearcasting".
        /// </summary>
        const string Clearcasting = "Clearcasting";
        /// <summary>
        /// Represents the constant string "Earth Shock".
        /// </summary>
        const string EarthShock = "Earth Shock";
        /// <summary>
        /// Represents the constant string value "Elemental Mastery".
        /// </summary>
        const string ElementalMastery = "Elemental Mastery";
        /// <summary>
        /// Represents the constant string "Flame Shock".
        /// </summary>
        const string FlameShock = "Flame Shock";
        /// <summary>
        /// Represents the constant string "Flametongue Weapon".
        /// </summary>
        const string FlametongueWeapon = "Flametongue Weapon";
        /// <summary>
        /// Represents the constant string value for "Focused Casting".
        /// </summary>
        const string FocusedCasting = "Focused Casting";
        /// <summary>
        /// Represents the constant string value for "Grounding Totem".
        /// </summary>
        const string GroundingTotem = "Grounding Totem";
        /// <summary>
        /// Represents the constant string "Mana Spring Totem".
        /// </summary>
        const string ManaSpringTotem = "Mana Spring Totem";
        /// <summary>
        /// The constant string representing the name "Healing Wave".
        /// </summary>
        const string HealingWave = "Healing Wave";
        /// <summary>
        /// Represents a constant string value for "Lightning Bolt".
        /// </summary>
        const string LightningBolt = "Lightning Bolt";
        /// <summary>
        /// Represents the constant string "Lightning Shield".
        /// </summary>
        const string LightningShield = "Lightning Shield";
        /// <summary>
        /// Represents the constant string "Rockbiter Weapon".
        /// </summary>
        const string RockbiterWeapon = "Rockbiter Weapon";
        /// <summary>
        /// Represents the constant string value for "Searing Totem".
        /// </summary>
        const string SearingTotem = "Searing Totem";
        /// <summary>
        /// Represents the constant string "Stoneclaw Totem".
        /// </summary>
        const string StoneclawTotem = "Stoneclaw Totem";
        /// <summary>
        /// Represents the constant string "Stoneskin Totem".
        /// </summary>
        const string StoneskinTotem = "Stoneskin Totem";
        /// <summary>
        /// Represents the constant string "Tremor Totem".
        /// </summary>
        const string TremorTotem = "Tremor Totem";
        /// <summary>
        /// Represents the constant string "Lesser Healing Wave".
        /// </summary>
        const string LesserHealingWave = "Lesser Healing Wave";

        /// <summary>
        /// Array of creatures that are feared by the player.
        /// </summary>
        readonly string[] fearingCreatures = new[] { "Scorpid Terror" };
        /// <summary>
        /// Array of fire-immune creatures.
        /// </summary>
        readonly string[] fireImmuneCreatures = new[] { "Rogue Flame Spirit", "Burning Destroyer" };
        /// <summary>
        /// An array of creatures that are immune to nature attacks.
        /// </summary>
        readonly string[] natureImmuneCreatures = new[] { "Swirling Vortex", "Gusting Vortex", "Dust Stormer" };


        /// <summary>
        /// Represents the last position of the target.
        /// </summary>
        Position targetLastPosition;

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
        }

        /// <summary>
        /// Updates the combat rotation for the player character.
        /// </summary>
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


            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HealingWave))
            {
                botStates.Push(new HealSelfState(botStates, container));
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

            if (player.TargetGuid != player.Guid & !player.IsCasting)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            // ensure auto-attack is turned on
            player.LuaCall(AutoAttackLuaScript);

            // ensure we're in melee range
            if (player.Position.DistanceTo(target.Position) > 35 || (natureImmuneCreatures.Contains(target.Name) || player.Mana < player.GetManaCost(LightningBolt) && (player.Position.DistanceTo(target.Position) > 3)))
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
            else
                player.StopAllMovement();


            // ----- COMBAT ROTATION -----
            var partyMembers = ObjectManager.GetPartyMembers();
            var healTarget = partyMembers.FirstOrDefault(p => p.HealthPercent < 50);

            if (healTarget != null && player.Mana > player.GetManaCost(HealingWave))
            {
                player.SetTarget(healTarget.Guid);
                TryCastSpell(HealingWave);
            }

            TryCastSpell(LightningBolt, player.ManaPercent > 50 && target.HealthPercent < 90 && !natureImmuneCreatures.Contains(target.Name) && ((TargetMovingTowardPlayer && target.Position.DistanceTo(player.Position) > 15) || (!TargetMovingTowardPlayer && target.Position.DistanceTo(player.Position) > 5) || (player.HasBuff(FocusedCasting) && target.HealthPercent > 20 && Wait.For("FocusedLightningBoltDelay", 4000, true))));

            TryCastSpell(FlameShock, player.ManaPercent > 50 && target.HealthPercent < 90 && !target.HasDebuff(FlameShock) && (target.HealthPercent >= 50 || natureImmuneCreatures.Contains(target.Name)) && !fireImmuneCreatures.Contains(target.Name));

            TryCastSpell(LightningShield, !natureImmuneCreatures.Contains(target.Name) && !player.HasBuff(LightningShield));

            TryCastSpell(RockbiterWeapon, player.KnowsSpell(RockbiterWeapon) && (fireImmuneCreatures.Contains(target.Name) || !player.MainhandIsEnchanted && !player.KnowsSpell(FlametongueWeapon)));

            TryCastSpell(FlametongueWeapon, player.KnowsSpell(FlametongueWeapon) && !player.MainhandIsEnchanted && !fireImmuneCreatures.Contains(target.Name));

            TryCastSpell(ManaSpringTotem, !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && u.Name.Contains(ManaSpringTotem)));

            TryCastSpell(ElementalMastery);

            targetLastPosition = target.Position;
        }

        /// <summary>
        /// Tries to cast a spell with the given name if the player meets the necessary conditions.
        /// </summary>
        void TryCastSpell(string name, bool condition = true, Action callback = null)
        {
            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && condition && !player.IsStunned && !player.IsCasting && !player.IsChanneling)
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

        /// <summary>
        /// Gets a value indicating whether the target is fleeing.
        /// </summary>
        bool TargetIsFleeing =>
                    targetLastPosition != null &&
                    targetLastPosition.DistanceTo(player.Position) < target.Position.DistanceTo(player.Position);

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
