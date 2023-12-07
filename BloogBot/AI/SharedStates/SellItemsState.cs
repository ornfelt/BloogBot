using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Frames;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// This namespace contains the shared states for handling the selling of items in the AI.
/// </summary>
namespace BloogBot.AI.SharedStates
{
    /// <summary>
    /// Represents a state in which the bot is selling items.
    /// </summary>
    /// <summary>
    /// Represents a state in which the bot is selling items.
    /// </summary>
    public class SellItemsState : IBotState
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
        /// Gets or sets the collection of WoW items to sell.
        /// </summary>
        readonly IEnumerable<WoWItem> itemsToSell;

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
        /// Represents the index of an item.
        /// </summary>
        int itemIndex;

        /// <summary>
        /// Initializes a new instance of the SellItemsState class.
        /// </summary>
        public SellItemsState(Stack<IBotState> botStates, IDependencyContainer container, string npcName)
        {
            this.botStates = botStates;
            this.npcName = npcName;
            player = ObjectManager.Player;

            itemsToSell = Inventory
                .GetAllItems()
                .Where(i =>
                    (i.Info.Name != "Hearthstone") &&
                    (i.Info.Name != container.BotSettings.Food) &&
                    (i.Info.Name != container.BotSettings.Drink) &&
                    (i.Info.ItemClass != ItemClass.Quest) &&
                    (i.Info.ItemClass != ItemClass.Container) &&
                    (string.IsNullOrWhiteSpace(container.BotSettings.SellExcludedNames) || !container.BotSettings.SellExcludedNames.Split('|').Any(n => n == i.Info.Name)) &&
                    (container.BotSettings.SellUncommon ? (int)i.Quality < 3 : (int)i.Quality < 2)
                );
        }

        /// <summary>
        /// Updates the state of the NPC interaction.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// ObjectManager -> Update: Units
        /// Update -> npc: Single(u => u.Name == npcName)
        /// npc -> Update: Interact()
        /// Update -> MerchantFrame: new MerchantFrame()
        /// MerchantFrame -> Update: Ready
        /// Update -> DialogFrame: new DialogFrame()
        /// DialogFrame -> Update: SelectFirstGossipOfType(player, DialogType.vendor)
        /// Update -> MerchantFrame: SellItemByGuid((uint)itemToSell.StackCount, npc.Guid, itemToSell.Guid)
        /// Update -> MerchantFrame: CloseMerchantFrame()
        /// Update -> botStates: Pop()
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
                state = State.ReadyToSell;
            }
            if (state == State.ReadyToSell)
            {
                if (Wait.For("SellItemDelay", 200))
                {
                    var itemToSell = itemsToSell.ElementAt(itemIndex);
                    merchantFrame.SellItemByGuid((uint)itemToSell.StackCount, npc.Guid, itemToSell.Guid);

                    itemIndex++;

                    if (itemIndex == itemsToSell.Count())
                    {
                        state = State.CloseMerchantFrame;
                    }
                }
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
        /// <remarks>
        /// \startuml
        /// :WowEventHandler: -> :OnDialogFrameOpenArgs e: : Dialog Opened
        /// :OnDialogFrameOpenArgs e: --> :WowEventHandler: : DialogFrame = e.DialogFrame
        /// \enduml
        /// </remarks>
        void WowEventHandler_OnDialogOpened(object sender, OnDialogFrameOpenArgs e) =>
                            dialogFrame = e.DialogFrame;

        /// <summary>
        /// Event handler for when the merchant frame is opened.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// object -> WowEventHandler_OnMerchantFrameOpened: OnMerchantFrameOpenArgs e
        /// WowEventHandler_OnMerchantFrameOpened -> merchantFrame: e.MerchantFrame
        /// \enduml
        /// </remarks>
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
            ReadyToPop,
            ReadyToSell
        }
    }
}
