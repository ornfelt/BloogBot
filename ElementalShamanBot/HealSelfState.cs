﻿using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the implementation of the HealSelfState class, which handles healing the player character in the Elemental Shaman Bot.
/// </summary>
namespace ElementalShamanBot
{
    /// <summary>
    /// Represents a state where the bot heals itself.
    /// </summary>
    /// <summary>
    /// Represents a state where the bot heals itself.
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
