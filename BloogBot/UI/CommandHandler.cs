using System;
using System.Windows.Input;

/// <summary>
/// This namespace contains classes related to the user interface of BloogBot.
/// </summary>
namespace BloogBot.UI
{
    /// <summary>
    /// Represents a command handler that implements the ICommand interface.
    /// </summary>
    /// <summary>
    /// Represents a command handler that implements the ICommand interface.
    /// </summary>
    class CommandHandler : ICommand
    {
        /// <summary>
        /// Gets the readonly action.
        /// </summary>
        readonly Action action;
        /// <summary>
        /// Gets a value indicating whether the command can be executed.
        /// </summary>
        readonly bool canExecute;

        /// <summary>
        /// Initializes a new instance of the CommandHandler class.
        /// </summary>
        public CommandHandler(Action action, bool canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        public void Execute(object parameter) => action();

        /// <summary>
        /// Determines whether the command can execute with the specified parameter.
        /// </summary>
        public bool CanExecute(object parameter) => canExecute;

        /// <summary>
        /// This event is needed to satisfy the compiler, but it is never used.
        /// </summary>
        // this needs to be here to satisfy the compiler even though we never use it
#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
    }
}
