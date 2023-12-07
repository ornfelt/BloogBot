﻿using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

/// <summary>
/// This namespace contains the code for the Affliction Warlock Bot.
/// </summary>
namespace AfflictionWarlockBot
{
    /// <summary>
    /// Represents a state for summoning a Voidwalker.
    /// </summary>
    /// <summary>
    /// Represents a state for summoning a Voidwalker.
    /// </summary>
    class SummonVoidwalkerState : IBotState
    {
        /// <summary>
        /// Represents the constant string value "Summon Imp".
        /// </summary>
        const string SummonImp = "Summon Imp";
        /// <summary>
        /// The constant string representing the action of summoning a Voidwalker.
        /// </summary>
        const string SummonVoidwalker = "Summon Voidwalker";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the SummonVoidwalkerState class.
        /// </summary>
        /// <param name="botStates">The stack of bot states.</param>
        public SummonVoidwalkerState(Stack<IBotState> botStates)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the player's pet based on their known spells and current pet status.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> Player: IsCasting
        /// alt Player IsCasting
        ///   Update -> Update: return
        /// else Player Not Casting
        ///   Update -> Player: KnowsSpell(SummonImp)
        ///   Update -> Player: KnowsSpell(SummonVoidwalker)
        ///   Update -> ObjectManager: Pet
        ///   alt Player KnowsSpell(SummonImp) or KnowsSpell(SummonVoidwalker) and Pet Exists
        ///     Update -> BotStates: Pop
        ///     Update -> BotStates: Push(new BuffSelfState)
        ///     Update -> Update: return
        ///   else Player Doesn't Know Spells or Pet Doesn't Exist
        ///     Update -> Player: KnowsSpell(SummonVoidwalker)
        ///     alt Player KnowsSpell(SummonVoidwalker)
        ///       Update -> Player: LuaCall(CastSpellByName(SummonVoidwalker))
        ///     else Player Doesn't Know SummonVoidwalker
        ///       Update -> Player: LuaCall(CastSpellByName(SummonImp))
        ///     end
        ///   end
        /// end
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (player.IsCasting)
                return;


            if ((!player.KnowsSpell(SummonImp) && !player.KnowsSpell(SummonVoidwalker)) || ObjectManager.Pet != null)
            {
                botStates.Pop();
                botStates.Push(new BuffSelfState(botStates));
                return;
            }

            if (player.KnowsSpell(SummonVoidwalker))
                player.LuaCall($"CastSpellByName('{SummonVoidwalker}')");
            else
                player.LuaCall($"CastSpellByName('{SummonImp}')");
        }
    }
}
