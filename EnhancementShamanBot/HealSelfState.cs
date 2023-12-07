using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// Represents a state in which the bot is healing itself.
/// </summary>
namespace EnhancementShamanBot
{
    /// <summary>
    /// Represents a class that handles the state of a bot healing itself.
    /// </summary>
    /// <summary>
    /// Represents a class that handles the state of a bot healing itself.
    /// </summary>
    class HealSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string "War Stomp".
        /// </summary>
        const string WarStomp = "War Stomp";
        /// <summary>
        /// The constant string representing the name "Healing Wave".
        /// </summary>
        const string HealingWave = "Healing Wave";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the HealSelfState class.
        /// </summary>
        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;

            if (player.IsSpellReady(WarStomp))
                player.LuaCall($"CastSpellByName('{WarStomp}')");
        }

        /// <summary>
        /// Updates the player's actions based on their health and mana levels.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: IsCasting
        /// alt player is casting
        ///   Update -> Update: return
        /// else player is not casting
        ///   Update -> Player: HealthPercent
        ///   Update -> Player: GetManaCost(HealingWave)
        ///   alt HealthPercent > 70 or Mana < ManaCost
        ///     Update -> BotStates: Pop
        ///     Update -> Update: return
        ///   else HealthPercent <= 70 and Mana >= ManaCost
        ///     Update -> Player: LuaCall(CastSpellByName)
        ///   end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.IsCasting) return;

            if (player.HealthPercent > 70 || player.Mana < player.GetManaCost(HealingWave))
            {
                botStates.Pop();
                return;
            }

            player.LuaCall($"CastSpellByName('{HealingWave}',1)");
        }
    }
}
