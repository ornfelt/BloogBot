using System.Collections.Generic;

/// <summary>
/// This namespace contains the CommandModel class which represents a command with its associated properties.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a model for a command with associated player and arguments.
    /// </summary>
    public class CommandModel
    {
        /// <summary>
        /// Initializes a new instance of the CommandModel class.
        /// </summary>
        public CommandModel(int id, string command, string player, string args)
        {
            Id = id;
            Command = command;
            Player = player;
            Args = args;
        }

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Gets the player name.
        /// </summary>
        public string Player { get; }

        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        public string Args { get; }
    }

    /// <summary>
    /// Represents a report signature with an ID, player name, and command ID.
    /// </summary>
    public class ReportSignature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportSignature"/> class.
        /// </summary>
        /// <param name="id">The ID of the report signature.</param>
        /// <param name="player">The player associated with the report signature.</param>
        /// <param name="commandId">The ID of the command associated with the report signature.</param>
        public ReportSignature(int id, string player, int commandId)
        {
            Id = id;
            Player = player;
            CommandId = commandId;
        }

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the player name.
        /// </summary>
        public string Player { get; }

        /// <summary>
        /// Gets the command ID.
        /// </summary>
        public int CommandId { get; }
    }

    /// <summary>
    /// Represents a summary of a report, including the command ID and a collection of report signatures.
    /// </summary>
    public class ReportSummary
    {
        /// <summary>
        /// Initializes a new instance of the ReportSummary class.
        /// </summary>
        public ReportSummary(int commandId, IEnumerable<ReportSignature> signatures)
        {
            CommandId = commandId;
            Signatures = signatures;
        }

        /// <summary>
        /// Gets the command ID.
        /// </summary>
        public int CommandId { get; }

        /// <summary>
        /// Gets the collection of report signatures.
        /// </summary>
        public IEnumerable<ReportSignature> Signatures { get; }
    }
}
