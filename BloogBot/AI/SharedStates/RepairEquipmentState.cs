using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Frames;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains shared states for handling equipment repair in the AI system.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state for repairing equipment in the game.
    /// </summary>
    /// <summary>
    /// Represents a state for repairing equipment in the game.
    /// </summary>
    public class RepairEquipmentState : IBotState
    {
        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Gets the name of the non-player character (NPC).
        /// </summary>
        readonly string npcName;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

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
        /// Initializes a new instance of the <see cref="RepairEquipmentState"/> class.
        /// </summary>
        public RepairEquipmentState(Stack<IBotState> botStates, IDependencyContainer container, string npcName)
        {
            this.botStates = botStates;
            this.container = container;
            this.npcName = npcName;
            player = ObjectManager.Player;
        }

        /// <summary>
        /// Updates the state of the bot and performs actions based on the current state.
        /// </summary>
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
                merchantFrame.RepairAll();
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

        /// <summary>
        /// Event handler for when a dialog is opened.
        /// </summary>
        void WowEventHandler_OnDialogOpened(object sender, OnDialogFrameOpenArgs e) =>
                    dialogFrame = e.DialogFrame;

        /// <summary>
        /// Event handler for when the merchant frame is opened.
        /// </summary>
        void WowEventHandler_OnMerchantFrameOpened(object sender, OnMerchantFrameOpenArgs e) =>
                    merchantFrame = e.MerchantFrame;

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
}
