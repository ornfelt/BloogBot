// Ported from BloogBot/BloogBot/UI/CommandHandler.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System;
using System.Windows.Input;

namespace BloogBot.UI
{
    class CommandHandler : ICommand
    {
        readonly Action action;
        readonly bool canExecute;

        public CommandHandler(Action action, bool canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }
        
        public void Execute(object parameter) => action();

        public bool CanExecute(object parameter) => canExecute;

// this needs to be here to satisfy the compiler even though we never use it
#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
    }
}
