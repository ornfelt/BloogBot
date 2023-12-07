using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Frames;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// This namespace contains the shared states for the AI system related to buying items.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents the state of buying items in the bot.
    /// </summary>
    /// <summary>
    /// Represents the state of buying items in the bot.
    /// </summary>
    public class BuyItemsState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Gets the name of the non-player character (NPC).
        /// </summary>
        readonly string npcName;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Gets or sets the dictionary of items to buy, where the key is the item name and the value is the quantity.
        /// </summary>
        readonly IDictionary<string, int> itemsToBuy;

        /// <summary>
        /// Initializes a new instance of the State class and sets the state to Uninitialized.
        /// </summary>
        State state = State.Uninitialized;
        /// <summary>
        /// Represents a non-player character (NPC) in the World of Warcraft.
        /// </summary>
        WoWUnit npc;
        /// <summary>
        /// Represents a dialog frame.
        /// </summary>
        DialogFrame dialogFrame;
        /// <summary>
        /// Represents a merchant frame.
        /// </summary>
        MerchantFrame merchantFrame;

        /// <summary>
        /// Initializes a new instance of the BuyItemsState class.
        /// </summary>
        public BuyItemsState(Stack<IBotState> botStates, string npcName, IDictionary<string, int> itemsToBuy)
        {
            this.botStates = botStates;
            this.npcName = npcName;
            this.itemsToBuy = itemsToBuy;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the state of the bot and performs actions based on the current state.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// Update -> ObjectManager: Get Units
        /// ObjectManager --> Update: Return NPC
        /// Update -> NPC: Interact
        /// Update -> MerchantFrame: Initialize
        /// MerchantFrame --> Update: Ready status
        /// Update -> DialogFrame: Initialize
        /// Update -> MerchantFrame: BuyItemByName
        /// Update -> DialogFrame: SelectFirstGossipOfType
        /// Update -> MerchantFrame: CloseMerchantFrame
        /// Update -> Wait: RemoveAll
        /// Update -> BotStates: Pop
        /// \enduml
        /// </remarks>
        public void Update()
        {
            if (state == State.Uninitialized)
            {
                npc = ObjectManager
                    .Units
                    .Single(u => u.Name == npcName);
                state = State.Interacting;
            }
            if (state == State.Interacting)
            {
                npc.Interact();
                state = State.PrepMerchantFrame;
            }
            if (state == State.PrepMerchantFrame && Wait.For("PrepMerchantFrameDelay", 500))
            {
                merchantFrame = new MerchantFrame();

                if (merchantFrame.Ready)
                    state = State.Initialized;
                else
                {
                    dialogFrame = new DialogFrame();
                    state = State.Dialog;
                }
            }
            if (state == State.Initialized && Wait.For("InitializeDelay", 500))
            {
                foreach (var item in itemsToBuy)
                {
                    merchantFrame.BuyItemByName(npc.Guid, item.Key, item.Value);
                    Thread.Sleep(200);
                }

                state = State.CloseMerchantFrame;
            }
            if (state == State.Dialog && Wait.For("DialogFrameDelay", 500))
            {
                dialogFrame.SelectFirstGossipOfType(player, DialogType.vendor);
                state = State.PrepMerchantFrame;
            }
            if (state == State.CloseMerchantFrame && Wait.For("BuyItemsCloseMerchantFrameStateDelay", 2000))
            {
                merchantFrame.CloseMerchantFrame();
                state = State.ReadyToPop;
            }
            if (state == State.ReadyToPop && Wait.For("BuyItemsPopBuyItemsStateDelay", 5000))
            {
                Wait.RemoveAll();
                botStates.Pop();
            }
        }
    }

    /// <summary>
    /// Represents the possible states of the program.
    /// </summary>
    enum State
    {
        Uninitialized,
        Interacting,
        PrepMerchantFrame,
        Initialized,
        Dialog,
        CloseMerchantFrame,
        ReadyToPop
    }
}