using BloogBot.Game.Enums;
using System;

/// <summary>
/// This namespace contains interfaces and functions for handling game functions.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a handler for game functions.
    /// </summary>
    public interface IGameFunctionHandler
    {
        /// <summary>
        /// Enumerates the visible objects based on the specified callback and filter.
        /// </summary>
        void EnumerateVisibleObjects(IntPtr callback, int filter);

        /// <summary>
        /// Retrieves the pointer to the object with the specified GUID.
        /// </summary>
        IntPtr GetObjectPtr(ulong guid);

        /// <summary>
        /// Retrieves the unique identifier for the player.
        /// </summary>
        ulong GetPlayerGuid();

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
        void SetFacing(IntPtr playerSetFacingPtr, float facing);

        /// <summary>
        /// Sends a movement update to the specified player.
        /// </summary>
        /// <param name="playerPtr">The pointer to the player.</param>
        /// <param name="opcode">The opcode for the movement update.</param>
        void SendMovementUpdate(IntPtr playerPtr, int opcode);

        /// <summary>
        /// Sets the specified control bit to the given state after the specified tick count.
        /// </summary>
        void SetControlBit(int bit, int state, int tickCount);

        /// <summary>
        /// Jumps to a new location.
        /// </summary>
        void Jump();

        /// <summary>
        /// Retrieves the type of creature based on the given unit pointer.
        /// </summary>
        CreatureType GetCreatureType(IntPtr unitPtr);

        /// <summary>
        /// Retrieves the rank of a creature based on its unit pointer.
        /// </summary>
        int GetCreatureRank(IntPtr unitPtr);

        /// <summary>
        /// Retrieves the reaction between two units.
        /// </summary>
        UnitReaction GetUnitReaction(IntPtr unitPtr1, IntPtr unitPtr2);

        /// <summary>
        /// Executes a Lua script.
        /// </summary>
        void LuaCall(string code);

        /// <summary>
        /// Retrieves the text associated with the specified variable name.
        /// </summary>
        IntPtr GetText(string varName);

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        int CastSpellById(int spellId, ulong targetGuid);

        /// <summary>
        /// Retrieves the spell database entry at the specified index.
        /// </summary>
        Spell GetSpellDBEntry(int index);

        /// <summary>
        /// Calculates the intersection point between two positions.
        /// </summary>
        XYZ Intersect(Position start, Position end);

        /// <summary>
        /// Sets the target with the specified GUID.
        /// </summary>
        void SetTarget(ulong guid);

        /// <summary>
        /// Retrieves the corpse.
        /// </summary>
        void RetrieveCorpse();

        /// <summary>
        /// Releases the memory allocated for a corpse object.
        /// </summary>
        void ReleaseCorpse(IntPtr ptr);

        /// <summary>
        /// Retrieves the cache entry for the specified item ID and GUID.
        /// </summary>
        IntPtr GetItemCacheEntry(int itemId, ulong guid = 0);

        /// <summary>
        /// Checks if a spell with the specified ID is currently on cooldown.
        /// </summary>
        bool IsSpellOnCooldown(int spellId);

        /// <summary>
        /// Loots the specified slot.
        /// </summary>
        /// <param name="slot">The slot to loot.</param>
        void LootSlot(int slot);

        /// <summary>
        /// Uses the item pointed to by the specified pointer.
        /// </summary>
        void UseItem(IntPtr itemPtr);

        /// <summary>
        /// Sells the specified number of items with the given itemGuid to the vendor with the specified vendorGuid.
        /// </summary>
        void SellItemByGuid(uint itemCount, ulong vendorGuid, ulong itemGuid);

        /// <summary>
        /// Buys a specified quantity of an item from a vendor.
        /// </summary>
        void BuyVendorItem(ulong vendorGuid, int itemId, int quantity);

        /// <summary>
        /// Dismounts the unit specified by the given pointer.
        /// </summary>
        int Dismount(IntPtr unitPtr);

        /// <summary>
        /// Casts a spell at the specified position.
        /// </summary>
        /// <param name="spellName">The name of the spell to cast.</param>
        /// <param name="position">The position to cast the spell at.</param>
        void CastAtPosition(string spellName, Position position);

        /// <summary>
        /// Retrieves the number of auras associated with the specified unit.
        /// </summary>
        int GetAuraCount(IntPtr unitPtr);

        /// <summary>
        /// Retrieves the pointer to the aura at the specified index for the given unit.
        /// </summary>
        IntPtr GetAuraPointer(IntPtr unitPtr, int index);

        /// <summary>
        /// Retrieves the pointer to the row at the specified index in the table.
        /// </summary>
        IntPtr GetRow(IntPtr tablePtr, int index);

        /// <summary>
        /// Retrieves a localized row from a table at the specified index.
        /// </summary>
        IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr);
    }
}
