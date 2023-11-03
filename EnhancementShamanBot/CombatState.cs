using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains the classes and functionality for the Enhancement Shaman Bot.
/// </summary>
namespace EnhancementShamanBot
{
    /// <summary>
    /// Represents a combat state that handles various combat actions and implements the IBotState interface.
    /// </summary>
    /// <summary>
    /// Represents a combat state that handles various combat actions and behaviors.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// Represents the constant string "Clearcasting".
        /// </summary>
        const string Clearcasting = "Clearcasting";
        /// <summary>
        /// Represents the constant string "Earth Shock".
        /// </summary>
        const string EarthShock = "Earth Shock";
        /// <summary>
        /// Represents the constant string "Flame Shock".
        /// </summary>
        const string FlameShock = "Flame Shock";
        /// <summary>
        /// Represents the constant string "Flametongue Weapon".
        /// </summary>
        const string FlametongueWeapon = "Flametongue Weapon";
        /// <summary>
        /// Represents the constant string value for "Grounding Totem".
        /// </summary>
        const string GroundingTotem = "Grounding Totem";
        /// <summary>
        /// The constant string representing the name "Healing Wave".
        /// </summary>
        const string HealingWave = "Healing Wave";
        /// <summary>
        /// Represents the constant string "Mana Spring Totem".
        /// </summary>
        const string ManaSpringTotem = "Mana Spring Totem";
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
        /// Represents the constant string "Stormstrike".
        /// </summary>
        const string Stormstrike = "Stormstrike";
        /// <summary>
        /// Represents the constant string "Tremor Totem".
        /// </summary>
        const string TremorTotem = "Tremor Totem";
        /// <summary>
        /// Represents the constant string "Windfury Weapon".
        /// </summary>
        const string WindfuryWeapon = "Windfury Weapon";

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
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 3)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the behavior of the player character.
        /// </summary>
        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HealingWave))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            TryCastSpell(GroundingTotem, 0, int.MaxValue, ObjectManager.Aggressors.Any(a => a.IsCasting && target.Mana > 0));

            TryCastSpell(TremorTotem, 0, int.MaxValue, fearingCreatures.Contains(target.Name) && !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 29 && u.HealthPercent > 0 && u.Name.Contains(TremorTotem)));

            TryCastSpell(WindfuryWeapon, 0, int.MaxValue, !player.MainhandIsEnchanted && player.KnowsSpell(WindfuryWeapon));

            TryCastSpell(StoneclawTotem, 0, int.MaxValue, ObjectManager.Aggressors.Count() > 1);

            TryCastSpell(ManaSpringTotem, 0, int.MaxValue, !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && u.Name.Contains(ManaSpringTotem)));

            TryCastSpell(StoneskinTotem, 0, int.MaxValue, target.Mana == 0 && !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && (u.Name.Contains(StoneclawTotem) || u.Name.Contains(StoneskinTotem) || u.Name.Contains(TremorTotem))));

            TryCastSpell(SearingTotem, 0, int.MaxValue, target.HealthPercent > 70 && !fireImmuneCreatures.Contains(target.Name) && target.Position.DistanceTo(player.Position) < 20 && !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && u.Name.Contains(SearingTotem)));

            TryCastSpell(Stormstrike, 0, 5);

            TryCastSpell(FlameShock, 0, 20, !target.HasDebuff(FlameShock) && target.HealthPercent > 70 || natureImmuneCreatures.Contains(target.Name) && !fireImmuneCreatures.Contains(target.Name));

            TryCastSpell(EarthShock, 0, 20, !natureImmuneCreatures.Contains(target.Name) && (!player.KnowsSpell(Stormstrike) && target.HealthPercent < 70) || target.HasDebuff(Stormstrike) || target.IsCasting || target.IsChanneling || player.HasBuff(Clearcasting));

            TryCastSpell(LightningShield, 0, int.MaxValue, !natureImmuneCreatures.Contains(target.Name) && !player.HasBuff(LightningShield));

            TryCastSpell(RockbiterWeapon, 0, int.MaxValue, !player.MainhandIsEnchanted && player.KnowsSpell(RockbiterWeapon) && !player.KnowsSpell(FlametongueWeapon) && !player.KnowsSpell(WindfuryWeapon));

            TryCastSpell(FlametongueWeapon, 0, int.MaxValue, !player.MainhandIsEnchanted && player.KnowsSpell(FlametongueWeapon) && !player.KnowsSpell(WindfuryWeapon));
        }
    }
}
