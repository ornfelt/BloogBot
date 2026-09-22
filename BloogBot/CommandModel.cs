// Ported from BloogBot/BloogBot/CommandModel.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System.Collections.Generic;

namespace BloogBot
{
    public class CommandModel
    {
        public CommandModel(int id, string command, string player, string args)
        {
            Id = id;
            Command = command;
            Player = player;
            Args = args;
        }

        public int Id { get; }

        public string Command { get; }

        public string Player { get; }

        public string Args { get; }
    }

    public class ReportSignature
    {
        public ReportSignature(int id, string player, int commandId)
        {
            Id = id;
            Player = player;
            CommandId = commandId;
        }

        public int Id { get; }

        public string Player { get; }

        public int CommandId { get; }
    }

    public class ReportSummary
    {
        public ReportSummary(int commandId, IEnumerable<ReportSignature> signatures)
        {
            CommandId = commandId;
            Signatures = signatures;
        }

        public int CommandId { get; }

        public IEnumerable<ReportSignature> Signatures { get; }
    }
}
