using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains classes related to the Affliction Warlock bot.
/// </summary>
namespace AfflictionWarlockBot
{
    /// <summary>
    /// Represents a class that handles self-buffing behavior for a bot.
    /// </summary>
    /// <summary>
    /// Represents a class that handles self-buffing behavior for a bot.
    /// </summary>
    class BuffSelfState : IBotState
    {
        /// <summary>
        /// Represents the constant string value for "Demon Armor".
        /// </summary>
        const string DemonArmor = "Demon Armor";
        /// <summary>
        /// Represents the constant string "Demon Skin".
        /// </summary>
        const string DemonSkin = "Demon Skin";

        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;

        /// <summary>
        /// Initializes a new instance of the BuffSelfState class.
        /// </summary>
        public BuffSelfState(Stack<IBotState> botStates)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
            player.SetTarget(player.Guid);
        }

        /// <summary>
        /// Updates the player's buffs and casts the appropriate spell if necessary.
        /// </summary>
        public void Update()
        {
            if (player.HasBuff(DemonSkin) || player.HasBuff(DemonArmor))
            {
                if (HasEnoughSoulShards)
                {
                    botStates.Pop();
                    return;
                }
                else
                    DeleteSoulShard();
            }

            if (player.KnowsSpell(DemonArmor))
                TryCastSpell(DemonArmor);
            else
                TryCastSpell(DemonSkin);
        }

        /// <summary>
        /// Tries to cast a spell with the given name and required level.
        /// </summary>
        void TryCastSpell(string name, int requiredLevel = 1)
        {

        }

        /// <summary>
        /// Deletes the last soul shard from the player's inventory.
        /// </summary>
        void DeleteSoulShard()
        {
            var ss = GetSoulShards.Last();
            player.LuaCall($"PickupContainerItem({Inventory.GetBagId(ss.Guid)},{Inventory.GetSlotId(ss.Guid)})");
            player.LuaCall("DeleteCursorItem()");
        }

        /// <summary>
        /// Checks if the number of soul shards obtained is less than or equal to 1.
        /// </summary>
        bool HasEnoughSoulShards => GetSoulShards.Count() <= 1;

        /// <summary>
        /// Retrieves all the Soul Shards from the inventory.
        /// </summary>
        IEnumerable<WoWItem> GetSoulShards => Inventory.GetAllItems().Where(i => i.Info.Name == "Soul Shard");
    }
}
