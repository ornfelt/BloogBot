// Ported from BloogBot/TestBot.cs/RestState.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.AI;
using System.Collections.Generic;

namespace TestBot
{
    class RestState : IBotState
    {
        readonly Stack<IBotState> botStates;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
        }

        public void Update()
        {
            botStates.Pop();
        }
    }
}
