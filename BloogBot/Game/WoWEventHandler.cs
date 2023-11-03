using BloogBot.Game.Frames;
using System;

/// <summary>
/// This namespace contains classes for handling World of Warcraft events.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Initializes the WoWEventHandler class by subscribing to the OnNewSignalEvent and OnNewSignalEventNoArgs events and assigning the EvaluateEvent method as the event handler.
    /// </summary>
    /// <summary>
    /// Initializes the WoWEventHandler class by subscribing to the OnNewSignalEvent and OnNewSignalEventNoArgs events and assigning the EvaluateEvent method as the event handler.
    /// </summary>
    public class WoWEventHandler
    {
        /// <summary>
        /// Initializes the WoWEventHandler class by subscribing to the OnNewSignalEvent and OnNewSignalEventNoArgs events and assigning the EvaluateEvent method as the event handler.
        /// </summary>
        static WoWEventHandler()
        {
            SignalEventManager.OnNewSignalEvent += EvaluateEvent;
            SignalEventManager.OnNewSignalEventNoArgs += EvaluateEvent;
        }

        /// <summary>
        /// Evaluates an event and performs corresponding actions based on the event name.
        /// </summary>
        static void EvaluateEvent(string eventName, object[] args)
        {
            ThreadSynchronizer.RunOnMainThread(() =>
            {
                switch (eventName)
                {
                    case "LOOT_OPENED":
                        OpenLootFrame();
                        break;
                    case "GOSSIP_SHOW":
                        OpenDialogFrame();
                        break;
                    case "MERCHANT_SHOW":
                        OpenMerchantFrame();
                        break;
                    case "UNIT_COMBAT":
                        // "NONE" represents a partial block (damage reduction). "BLOCK" represents a full block (damage avoidance).
                        if ((string)args[0] == "player" && ((string)args[1] == "DODGE" || (string)args[1] == "PARRY" || (string)args[1] == "NONE" || (string)args[1] == "BLOCK"))
                            OnBlockParryDodge?.Invoke(null, new EventArgs());
                        if ((string)args[0] == "player" && (string)args[1] == "PARRY")
                            OnParry?.Invoke(null, new EventArgs());
                        break;
                    case "UI_ERROR_MESSAGE":
                        OnErrorMessage?.Invoke(null, new OnUiMessageArgs((string)args[0]));
                        break;
                    case "CHAT_MSG_COMBAT_SELF_HITS":
                    case "CHAT_MSG_COMBAT_SELF_MISSES":
                        OnSlamReady?.Invoke(null, new EventArgs());
                        break;
                    case "CHAT_MSG_SPELL_SELF_DAMAGE":
                        var messageText = (string)args[0];
                        if (messageText.Contains("Heroic Strike") || messageText.Contains("Cleave"))
                            OnSlamReady?.Invoke(null, new EventArgs());
                        break;
                    case "CHAT_MSG_SAY":
                        OnChatMessage?.Invoke(null, new OnChatMessageArgs((string)args[0], (string)args[1], "Say"));
                        break;
                    case "CHAT_MSG_WHISPER":
                        OnChatMessage?.Invoke(null, new OnChatMessageArgs((string)args[0], (string)args[1], "Whisper"));
                        break;
                    case "UNIT_LEVEL":
                        if ((string)args[0] == "player")
                            OnLevelUp?.Invoke(null, new EventArgs());
                        break;
                }
            });
        }

        /// <summary>
        /// Event that is triggered when a block, parry, or dodge action occurs.
        /// </summary>
        static public event EventHandler<EventArgs> OnBlockParryDodge;

        /// <summary>
        /// Event that is triggered when a parry occurs.
        /// </summary>
        static public event EventHandler<EventArgs> OnParry;

        /// <summary>
        /// Event that is triggered when the loot frame is opened.
        /// </summary>
        static public event EventHandler<OnLootFrameOpenArgs> OnLootOpened;

        /// <summary>
        /// Event that is triggered when a dialog frame is opened.
        /// </summary>
        static public event EventHandler<OnDialogFrameOpenArgs> OnDialogOpened;

        /// <summary>
        /// Event that is triggered when the merchant frame is opened.
        /// </summary>
        static public event EventHandler<OnMerchantFrameOpenArgs> OnMerchantFrameOpened;

        /// <summary>
        /// Event that is raised when an error message is received.
        /// </summary>
        static public event EventHandler<OnUiMessageArgs> OnErrorMessage;

        /// <summary>
        /// Event that is triggered when the slam is ready.
        /// </summary>
        static public event EventHandler<EventArgs> OnSlamReady;

        /// <summary>
        /// Event that is triggered when a chat message is received.
        /// </summary>
        static public event EventHandler<OnChatMessageArgs> OnChatMessage;

        /// <summary>
        /// Event that is triggered when a level up occurs.
        /// </summary>
        static public event EventHandler<EventArgs> OnLevelUp;

        /// <summary>
        /// Clears the OnErrorMessage event handler.
        /// </summary>
        static public void ClearOnErrorMessage()
        {
            OnErrorMessage = null;
        }

        /// <summary>
        /// Opens the loot frame and invokes the OnLootOpened event.
        /// </summary>
        static void OpenLootFrame()
        {
            var lootFrame = new LootFrame();
            OnLootOpened?.Invoke(null, new OnLootFrameOpenArgs(lootFrame));
        }

        /// <summary>
        /// Opens a dialog frame and invokes the OnDialogOpened event with the specified arguments.
        /// </summary>
        static void OpenDialogFrame()
        {
            var dialogFrame = new DialogFrame();
            OnDialogOpened?.Invoke(null, new OnDialogFrameOpenArgs(dialogFrame));
        }

        /// <summary>
        /// Opens the merchant frame and invokes the OnMerchantFrameOpened event.
        /// </summary>
        static void OpenMerchantFrame()
        {
            var merchantFrame = new MerchantFrame();
            OnMerchantFrameOpened?.Invoke(null, new OnMerchantFrameOpenArgs(merchantFrame));
        }
    }

    /// <summary>
    /// Represents the arguments for when the loot frame is opened.
    /// </summary>
    public class OnLootFrameOpenArgs : EventArgs
    {
        /// <summary>
        /// Represents a read-only instance of the LootFrame class.
        /// </summary>
        public readonly LootFrame LootFrame;

        /// <summary>
        /// Initializes a new instance of the OnLootFrameOpenArgs class.
        /// </summary>
        internal OnLootFrameOpenArgs(LootFrame lootFrame)
        {
            LootFrame = lootFrame;
        }
    }

    /// <summary>
    /// Represents the arguments for when a dialog frame is opened.
    /// </summary>
    public class OnDialogFrameOpenArgs : EventArgs
    {
        /// <summary>
        /// Represents a readonly DialogFrame object.
        /// </summary>
        public readonly DialogFrame DialogFrame;

        /// <summary>
        /// Initializes a new instance of the OnDialogFrameOpenArgs class.
        /// </summary>
        internal OnDialogFrameOpenArgs(DialogFrame dialogFrame)
        {
            DialogFrame = dialogFrame;
        }
    }

    /// <summary>
    /// Represents the arguments for when a merchant frame is opened.
    /// </summary>
    public class OnMerchantFrameOpenArgs : EventArgs
    {
        /// <summary>
        /// Represents a read-only instance of the MerchantFrame class.
        /// </summary>
        public readonly MerchantFrame MerchantFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnMerchantFrameOpenArgs"/> class.
        /// </summary>
        /// <param name="merchantFrame">The <see cref="MerchantFrame"/> to be assigned.</param>
        internal OnMerchantFrameOpenArgs(MerchantFrame merchantFrame)
        {
            MerchantFrame = merchantFrame;
        }
    }

    /// <summary>
    /// Represents the arguments for an event that occurs when a message is received on the UI.
    /// </summary>
    public class OnUiMessageArgs : EventArgs
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Initializes a new instance of the OnUiMessageArgs class with the specified message.
        /// </summary>
        internal OnUiMessageArgs(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Represents the arguments for the event when a chat message is received.
    /// </summary>
    public class OnChatMessageArgs : EventArgs
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        public readonly string Message;
        /// <summary>
        /// Gets the name of the unit.
        /// </summary>
        public readonly string UnitName;
        /// <summary>
        /// The chat channel for communication.
        /// </summary>
        public readonly string ChatChannel;

        /// <summary>
        /// Initializes a new instance of the OnChatMessageArgs class.
        /// </summary>
        internal OnChatMessageArgs(string message, string unitName, string chatChannel)
        {
            Message = message;
            UnitName = unitName;
            ChatChannel = chatChannel;
        }
    }
}
