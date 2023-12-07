using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the Shadow Priest Bot.
/// </summary>
namespace ShadowPriestBot
{
    /// <summary>
    /// Represents a combat state in the bot, implementing the IBotState interface.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The Lua script for casting the "Shoot" spell if the action button with ID 12 is not set to auto-repeat.
        /// </summary>
        const string WandLuaScript = "if IsAutoRepeatAction(12) == nil then CastSpellByName('Shoot') end";
        /// <summary>
        /// Lua script to turn off the wand if the action is set to auto-repeat.
        /// </summary>
        const string TurnOffWandLuaScript = "if IsAutoRepeatAction(12) ~= nil then CastSpellByName('Shoot') end";

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
        /// The constant string representing the name "Shadowform".
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
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 30)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the behavior of the player character.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Update()" as U
        /// participant "player" as P
        /// participant "target" as T
        /// participant "botStates" as B
        /// participant "Inventory" as I
        /// participant "ObjectManager" as O
        /// U -> P: HealthPercent
        /// U -> T: HealthPercent
        /// U -> P: Mana
        /// U -> P: GetManaCost(LesserHeal)
        /// U -> B: Push(new HealSelfState)
        /// U -> U: base.Update()
        /// U -> I: GetEquippedItem(EquipSlot.Ranged)
        /// U -> P: IsCasting
        /// U -> P: IsChanneling
        /// U -> P: ManaPercent
        /// U -> T: CreatureType
        /// U -> T: HealthPercent
        /// U -> P: LuaCall(WandLuaScript)
        /// U -> O: Aggressors
        /// U -> U: TryCastSpell(ShadowForm)
        /// U -> U: TryCastSpell(VampiricEmbrace)
        /// U -> O: Units
        /// U -> U: TryCastSpell(PsychicScream)
        /// U -> U: TryCastSpell(ShadowWordPain)
        /// U -> U: TryCastSpell(DispelMagic)
        /// U -> P: KnowsSpell(AbolishDisease)
        /// U -> U: TryCastSpell(AbolishDisease)
        /// U -> P: KnowsSpell(CureDisease)
        /// U -> U: TryCastSpell(CureDisease)
        /// U -> U: TryCastSpell(InnerFire)
        /// U -> U: TryCastSpell(PowerWordShield)
        /// U -> U: TryCastSpell(MindBlast)
        /// U -> P: KnowsSpell(MindFlay)
        /// U -> U: TryCastSpell(MindFlay)
        /// U -> U: TryCastSpell(Smite)
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(LesserHeal))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            var hasWand = Inventory.GetEquippedItem(EquipSlot.Ranged) != null;
            var useWand = hasWand && !player.IsCasting && !player.IsChanneling && (player.ManaPercent <= 10 || target.CreatureType == CreatureType.Totem || target.HealthPercent <= 10);
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
            }
        }
    }
}
