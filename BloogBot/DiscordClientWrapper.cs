using BloogBot.Game.Enums;
using Discord;
using Discord.WebSocket;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This namespace contains classes for interacting with the Discord client.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a wrapper for a Discord client.
    /// </summary>
    /// <summary>
    /// Represents a wrapper for a Discord client.
    /// </summary>
    public class DiscordClientWrapper
    {
        /// <summary>
        /// Represents a Discord socket client.
        /// </summary>
        static DiscordSocketClient client;
        /// <summary>
        /// Represents a socket guild.
        /// </summary>
        static SocketGuild guild;
        /// <summary>
        /// Represents the role assigned to the botsmiths.
        /// </summary>
        static SocketRole botsmithsRole;
        /// <summary>
        /// Represents a static socket text channel.
        /// </summary>
        static SocketTextChannel channel;

        /// <summary>
        /// The guild ID for Bloog's Minions.
        /// </summary>
        static ulong bloogsMinionsGuildId;
        /// <summary>
        /// The ID of the botsmiths role.
        /// </summary>
        static ulong botsmithsRoleId;
        /// <summary>
        /// The ID of the bloogBot channel.
        /// </summary>
        static ulong bloogBotChannelId;

        /// <summary>
        /// Gets or sets a value indicating whether the Discord bot is enabled.
        /// </summary>
        static bool discordBotEnabled;

        /// <summary>
        /// Initializes the bot with the provided settings.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Initialize -> BotSettings: Receive botSettings
        /// Initialize -> BotSettings: Set discordBotEnabled
        /// alt discordBotEnabled is true
        ///     Initialize -> ServicePointManager: Set SecurityProtocol to Tls12
        ///     Initialize -> BotSettings: Get DiscordGuildId, DiscordChannelId, DiscordBotToken
        ///     Initialize -> Convert: Convert DiscordGuildId, DiscordChannelId to UInt64
        ///     Initialize -> DiscordSocketClient: Create new client
        ///     Initialize -> client: Attach Log and ClientReady events
        ///     Initialize -> Task: Start new task
        ///     Task -> client: LoginAsync and StartAsync
        ///     alt Exception occurs
        ///         Task -> Console: Write exception message
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        static internal void Initialize(BotSettings botSettings)
        {
            discordBotEnabled = botSettings.DiscordBotEnabled;
            if (discordBotEnabled)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                bloogsMinionsGuildId = Convert.ToUInt64(botSettings.DiscordGuildId);
                botsmithsRoleId = Convert.ToUInt64(botSettings.DiscordGuildId);
                bloogBotChannelId = Convert.ToUInt64(botSettings.DiscordChannelId);
                client = new DiscordSocketClient();

                client.Log += Log;
                client.Ready += ClientReady;

                Task.Run(async () =>
                {
                    try
                    {
                        await client.LoginAsync(TokenType.Bot, botSettings.DiscordBotToken);
                        await client.StartAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Discord connection failed with exception: {e}");
                    }
                });
            }
        }

        /// <summary>
        /// Logs a message using the Logger class.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// LogMessage -> Logger: Log(msg.ToString())
        /// Logger --> LogMessage: Task.CompletedTask
        /// \enduml
        /// </remarks>
        static Task Log(LogMessage msg)
        {
            Logger.Log(msg.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Method to handle when the client is ready.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// activate "ClientReady" 
        /// "ClientReady" -> "client" : GetGuild(bloogsMinionsGuildId)
        /// "client" --> "ClientReady" : guild
        /// "ClientReady" -> "guild" : GetRole(botsmithsRoleId)
        /// "guild" --> "ClientReady" : botsmithsRole
        /// "ClientReady" -> "client" : GetChannel(bloogBotChannelId)
        /// "client" --> "ClientReady" : channel
        /// deactivate "ClientReady"
        /// \enduml
        /// </remarks>
        static Task ClientReady()
        {
            guild = client.GetGuild(bloogsMinionsGuildId);
            botsmithsRole = guild.GetRole(botsmithsRoleId);
            channel = client.GetChannel(bloogBotChannelId) as SocketTextChannel;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a killswitch alert message to the specified player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "KillswitchAlert Method" as K
        /// participant "Discord Bot" as D
        /// 
        /// K -> D: Check if discordBotEnabled
        /// alt discordBotEnabled is true
        ///     K -> D: Send message to channel
        /// end
        /// \enduml
        /// </remarks>
        static internal void KillswitchAlert(string playerName)
        {
            if (discordBotEnabled)
                Task.Run(async () =>
                await channel.SendMessageAsync($"{botsmithsRole.Mention} \uD83D\uDEA8 ALERT ALERT! {playerName} has arrived in GM Island! Stopping for now. \uD83D\uDEA8")
            );
        }

        /// <summary>
        /// Sends a teleport alert message to the Discord channel.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// TeleportAlert -> "if discordBotEnabled": Check if discord bot is enabled
        /// "if discordBotEnabled" -> Task: Run Task
        /// Task -> channel: Send message
        /// \enduml
        /// </remarks>
        static internal void TeleportAlert(string playerName)
        {
            if (discordBotEnabled)
                Task.Run(async () =>
                await channel.SendMessageAsync($"{botsmithsRole.Mention} \uD83D\uDEA8 ALERT ALERT! {playerName} has been teleported! Stopping for now. \uD83D\uDEA8")
            );
        }

        /// <summary>
        /// Sends a message using the Discord bot if it is enabled.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Application -> DiscordBot: SendMessage(message)
        /// activate DiscordBot
        /// DiscordBot -> Channel: SendMessageAsync(message)
        /// deactivate DiscordBot
        /// \enduml
        /// </remarks>
        static public void SendMessage(string message)
        {
            if (discordBotEnabled)
                Task.Run(async () =>
                await channel.SendMessageAsync(message)
            );
        }

        /// <summary>
        /// Sends a notification to the Discord channel with information about a found item.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "SendItemNotification Method" as Method
        /// participant "Discord Bot" as Bot
        /// participant "Channel" as Channel
        ///
        /// Method -> Bot: Check if discordBotEnabled
        /// alt discordBotEnabled is true
        ///     Method -> Method: Create message with playerName, quality, itemId
        ///     Method -> Channel: Send message asynchronously
        /// end
        /// \enduml
        /// </remarks>
        static public void SendItemNotification(string playerName, ItemQuality quality, int itemId)
        {
            if (discordBotEnabled)
            {
                var sb = new StringBuilder();
                var article = quality == ItemQuality.Rare ? "a" : "an";
                sb.Append($"{playerName} here! I just found {article} {quality} item!\n");
                sb.Append($"https://classic.wowhead.com/item={itemId}");

                Task.Run(async () =>
                    await channel.SendMessageAsync(sb.ToString())
                );
            }
        }
    }
}
