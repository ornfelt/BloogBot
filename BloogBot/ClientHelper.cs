using BloogBot.Game.Enums;
using System;
using System.Diagnostics;

/// <summary>
/// This class provides helper methods for interacting with the game client.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// This class provides helper methods for the client.
    /// </summary>
    /// <summary>
    /// This class provides helper methods for the client.
    /// </summary>
    public static class ClientHelper
    {
        /// <summary>
        /// Represents the client version.
        /// </summary>
        public static readonly ClientVersion ClientVersion;

        /// <summary>
        /// Sets the client version based on the file version of the WoW process.
        /// </summary>
        static ClientHelper()
        {
            var clientVersion = Process.GetProcessesByName("WoW")[0].MainModule.FileVersionInfo.FileVersion;

            if (clientVersion == "3, 3, 5, 12340")
            {
                ClientVersion = ClientVersion.WotLK;
            }
            else if (clientVersion == "2, 4, 3, 8606")
            {
                ClientVersion = ClientVersion.TBC;
            }
            else if (clientVersion == "1, 12, 1, 5875")
            {
                ClientVersion = ClientVersion.Vanilla;
            }
            else
                throw new InvalidOperationException("Unknown client version.");
        }
    }
}
