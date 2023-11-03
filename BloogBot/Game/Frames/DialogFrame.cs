using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes for handling dialog frames in the game.
/// </summary>
namespace BloogBot.Game.Frames
{
    /// <summary>
    /// Represents a dialog frame in the application.
    /// </summary>
    /// <summary>
    /// Represents a dialog frame in the application.
    /// </summary>
    public class DialogFrame
    {
        /// <summary>
        /// Initializes a new instance of the DialogFrame class.
        /// </summary>
        public DialogFrame()
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                var currentItem = (IntPtr)0xBBBE90;
                while ((int)currentItem < 0xBC3F50)
                {
                    if (MemoryManager.ReadInt((currentItem + 0x800)) == -1) break;
                    var optionType = MemoryManager.ReadInt((currentItem + 0x808));

                    DialogOptions.Add(new DialogOption((DialogType)optionType));
                    currentItem = IntPtr.Add(currentItem, 0x80C);
                }
            }
            else
            {
                var vendorGuid = MemoryManager.ReadUlong((IntPtr)MemoryAddresses.DialogFrameBase);
                if (vendorGuid == 0)
                    return;

                var dialogOptionCount = Convert.ToInt32(ObjectManager.Player.LuaCallWithResults($"{{0}} = GetNumGossipOptions()")[0]);

                var script = "";
                for (var i = 0; i < dialogOptionCount; i++)
                {
                    var startingIndex = i * 2;

                    script += "{" + startingIndex + "}, ";
                    script += "{" + (startingIndex + 1) + "}";

                    if (i + 1 == dialogOptionCount)
                        script += " = GetGossipOptions()";
                    else
                        script += ", ";
                }

                var dialogOptions = ObjectManager.Player.LuaCallWithResults(script);

                for (var i = 0; i < dialogOptionCount; i++)
                {
                    var startingIndex = i * 2;

                    var type = (DialogType)Enum.Parse(typeof(DialogType), dialogOptions[startingIndex + 1]);
                    DialogOptions.Add(new DialogOption(type));
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of dialog options.
        /// </summary>
        public IList<DialogOption> DialogOptions { get; } = new List<DialogOption>();

        /// <summary>
        /// Closes the dialog frame for the specified WoWPlayer.
        /// </summary>
        public void CloseDialogFrame(WoWPlayer player) => player.LuaCall("CloseGossip()");

        /// <summary>
        /// Selects the first gossip option of the specified type for the given player.
        /// </summary>
        public void SelectFirstGossipOfType(WoWPlayer player, DialogType type)
        {
            for (var i = 0; i < DialogOptions.Count; i++)
            {
                if (DialogOptions[i].Type != type) continue;
                player.LuaCall("SelectGossipOption(" + (i + 1) + ")");
                return;
            }
        }
    }

    /// <summary>
    /// Represents a dialog option.
    /// </summary>
    public class DialogOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogOption"/> class with the specified type.
        /// </summary>
        internal DialogOption(DialogType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the type of the dialog.
        /// </summary>
        public DialogType Type { get; }
    }
}
