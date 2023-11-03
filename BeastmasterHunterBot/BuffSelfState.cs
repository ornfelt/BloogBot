// Friday owns this file!

using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes related to the Beast Master Hunter Bot.
/// </summary>
namespace BeastMasterHunterBot
{
    /// <summary>
    /// This class represents a state in which the bot buffs itself.
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string value for "Aspect of the Monkey".
        /// </summary>
        const string AspectOfTheMonkey = "Aspect of the Monkey";
        /// <summary>
        /// Represents the constant string value for "Aspect of the Cheetah".
        /// </summary>
        const string AspectOfTheCheetah = "Aspect of the Cheetah";
        /// <summary>
        /// Represents the constant string value for "Aspect of the Hawk".
        /// </summary>
        const string AspectOfTheHawk = "Aspect of the Hawk";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuffSelfState"/> class.
        /// </summary>
        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
            player.SetTarget(player.Guid);
        }

        /// <summary>
        /// Updates the player's state by checking if they know the spell AspectOfTheHawk and if they have the buff AspectOfTheHawk. If either condition is true, the bot state is popped and the method returns. Otherwise, the method tries to cast the spell AspectOfTheHawk.
        /// </summary>
        public void Update()
        {
            if (!player.KnowsSpell(AspectOfTheHawk) || player.HasBuff(AspectOfTheHawk))
            {
                botStates.Pop();
                return;
            }

            TryCastSpell(AspectOfTheHawk);
        }

        /// <summary>
        /// Tries to cast a spell by the given name if the player has the required level and the spell is ready.
        /// </summary>
        void TryCastSpell(string name, int requiredLevel = 1)
        {
            if (!player.HasBuff(name) && player.Level >= requiredLevel && player.IsSpellReady(name))
                player.LuaCall($"CastSpellByName('{name}')");
        }
    }
}
