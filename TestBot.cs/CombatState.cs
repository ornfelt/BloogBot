// Ported from BloogBot/TestBot.cs/CombatState.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.AI;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace TestBot
{
    class CombatState : IBotState
    {
        readonly Stack<IBotState> botStates;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true)
        {
            this.botStates = botStates;
        }

        public void Update()
        {
            botStates.Pop();
        }
    }
}
