using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the Arcane Mage bot.
/// </summary>
namespace ArcaneMageBot
{
    /// <summary>
    /// Represents the combat state of the bot, implementing the IBotState interface.
    /// </summary>
    /// <summary>
    /// Represents the combat state of the bot and implements the IBotState interface.
    /// </summary>
    class CombatState : CombatStateBase, IBotState
    {
        /// <summary>
        /// The Lua script for casting the "Shoot" spell if the action with ID 11 is not set to auto-repeat.
        /// </summary>
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";

        /// <summary>
        /// Represents the constant string value for "Arcane Missiles".
        /// </summary>
        const string ArcaneMissiles = "Arcane Missiles";
        /// <summary>
        /// Represents the constant string "Arcane Power".
        /// </summary>
        const string ArcanePower = "Arcane Power";
        /// <summary>
        /// Represents the constant string "Clearcasting".
        /// </summary>
        const string Clearcasting = "Clearcasting";
        /// <summary>
        /// Represents the name of the counterspell.
        /// </summary>
        const string Counterspell = "Counterspell";
        /// <summary>
        /// Represents the constant string "Frost Nova".
        /// </summary>
        const string FrostNova = "Frost Nova";
        /// <summary>
        /// Represents a constant string value for "Fireball".
        /// </summary>
        const string Fireball = "Fireball";
        /// <summary>
        /// Represents the constant string "Fire Blast".
        /// </summary>
        const string FireBlast = "Fire Blast";
        /// <summary>
        /// Represents the constant string "Mana Shield".
        /// </summary>
        const string ManaShield = "Mana Shield";
        /// <summary>
        /// Represents the constant value for "Presence of Mind".
        /// </summary>
        const string PresenceOfMind = "Presence of Mind";

        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a read-only World of Warcraft unit target.
        /// </summary>
        readonly WoWUnit target;

        /// <summary>
        /// Represents a boolean value indicating whether the character is performing a frost nova backpedaling.
        /// </summary>
        bool frostNovaBackpedaling;
        /// <summary>
        /// Represents the start time of the backpedal for the Frost Nova ability.
        /// </summary>
        int frostNovaBackpedalStartTime;

        /// <summary>
        /// Initializes a new instance of the CombatState class with the specified parameters.
        /// </summary>
        internal CombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) : base(botStates, container, target, 30)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        /// <summary>
        /// Updates the character's actions and abilities.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Update()" as U
        /// participant "Environment" as E
        /// participant "Player" as P
        /// participant "Inventory" as I
        /// participant "Target" as T
        /// participant "ObjectManager" as O
        /// 
        /// U -> E: TickCount
        /// U -> P: StopMovement(ControlBits.Back)
        /// U -> P: ManaPercent
        /// U -> P: IsCasting
        /// U -> P: IsChanneling
        /// U -> I: GetEquippedItem(EquipSlot.Ranged)
        /// U -> P: LuaCall(WandLuaScript)
        /// U -> T: HealthPercent
        /// U -> T: Mana
        /// U -> T: IsCasting
        /// U -> P: HasBuff(ManaShield)
        /// U -> P: HealthPercent
        /// U -> P: HasBuff(Clearcasting)
        /// U -> O: Units
        /// U -> P: Level
        /// U -> P: HasBuff(PresenceOfMind)
        /// U -> P: Level
        /// \enduml
        /// </remarks>
        public new void Update()
        {
            if (frostNovaBackpedaling && Environment.TickCount - frostNovaBackpedalStartTime > 1500)
            {
                player.StopMovement(ControlBits.Back);
                frostNovaBackpedaling = false;
            }
            if (frostNovaBackpedaling)
                return;

            if (base.Update())
                return;

            var hasWand = Inventory.GetEquippedItem(EquipSlot.Ranged) != null;
            var useWand = hasWand && player.ManaPercent <= 10 && !player.IsCasting && !player.IsChanneling;
            if (useWand)
                player.LuaCall(WandLuaScript);

            TryCastSpell(PresenceOfMind, 0, 50, target.HealthPercent > 80);

            TryCastSpell(ArcanePower, 0, 50, target.HealthPercent > 80);

            TryCastSpell(Counterspell, 0, 29, target.Mana > 0 && target.IsCasting);

            TryCastSpell(ManaShield, 0, 50, (!player.HasBuff(ManaShield) && player.HealthPercent < 20));

            TryCastSpell(FireBlast, 0, 19, !player.HasBuff(Clearcasting));

            TryCastSpell(FrostNova, 0, 10, !ObjectManager.Units.Any(u => u.Guid != target.Guid && u.Health > 0 && u.Position.DistanceTo(player.Position) < 15), callback: FrostNovaCallback);

            TryCastSpell(Fireball, 0, 34, player.Level < 15 || player.HasBuff(PresenceOfMind));

            TryCastSpell(ArcaneMissiles, 0, 29, player.Level >= 15);
        }

        /// <summary>
        /// Callback function for Frost Nova. Sets the frostNovaBackpedaling flag to true, records the start time of backpedaling, and starts the player's movement in the backward direction.
        /// </summary>
        Action FrostNovaCallback => () =>
                {
                    frostNovaBackpedaling = true;
                    frostNovaBackpedalStartTime = Environment.TickCount;
                    player.StartMovement(ControlBits.Back);
                };
    }
}
