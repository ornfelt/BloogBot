using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This namespace contains various game functions and handlers.
/// </summary>
namespace BloogBot.Game
{
    /// <summary>
    /// Represents a collection of utility functions.
    /// </summary>
    /// <summary>
    /// Represents a collection of utility functions.
    /// </summary>
    static public class Functions
    {
        /// <summary>
        /// Represents a static, read-only instance of the Random class.
        /// </summary>
        static readonly Random random = new Random();

        /// <summary>
        /// Represents a static readonly instance of the IGameFunctionHandler interface.
        /// </summary>
        static readonly IGameFunctionHandler gameFunctionHandler;

        /// <summary>
        /// Initializes the game function handler based on the client version.
        /// </summary>
        static Functions()
        {
            if (ClientHelper.ClientVersion == ClientVersion.WotLK)
                gameFunctionHandler = new WotLKGameFunctionHandler();
            else if (ClientHelper.ClientVersion == ClientVersion.TBC)
                gameFunctionHandler = new TBCGameFunctionHandler();
            else if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                gameFunctionHandler = new VanillaGameFunctionHandler();
        }

        /// <summary>
        /// Buys an item from a vendor.
        /// </summary>
        static public void BuyVendorItem(ulong vendorGuid, int itemId, int quantity)
        {
            gameFunctionHandler.BuyVendorItem(vendorGuid, itemId, quantity);
        }

        /// <summary>
        /// Casts a spell at a specified position.
        /// </summary>
        static public void CastAtPosition(string spellName, Position position)
        {
            gameFunctionHandler.CastAtPosition(spellName, position);
        }

        /// <summary>
        /// Gets the count of auras for a given unit.
        /// </summary>
        static public int GetAuraCount(IntPtr unitPtr)
        {
            return gameFunctionHandler.GetAuraCount(unitPtr);
        }

        /// <summary>
        /// Retrieves the pointer to the aura at the specified index for the given unit.
        /// </summary>
        static public IntPtr GetAuraPointer(IntPtr unitPtr, int index)
        {
            return gameFunctionHandler.GetAuraPointer(unitPtr, index);
        }

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        static public int CastSpellById(int spellId, ulong targetGuid)
        {
            return gameFunctionHandler.CastSpellById(spellId, targetGuid);
        }

        /// <summary>
        /// Dismounts a unit specified by the given pointer.
        /// </summary>
        static public int Dismount(IntPtr unitPtr)
        {
            return gameFunctionHandler.Dismount(unitPtr);
        }

        /// <summary>
        /// Enumerates the visible objects using the specified callback and filter.
        /// </summary>
        static public void EnumerateVisibleObjects(IntPtr callback, int filter)
        {
            gameFunctionHandler.EnumerateVisibleObjects(callback, filter);
        }

        /// <summary>
        /// Gets the rank of a creature.
        /// </summary>
        static public int GetCreatureRank(IntPtr unitPtr)
        {
            return gameFunctionHandler.GetCreatureRank(unitPtr);
        }

        /// <summary>
        /// Gets the type of creature based on the provided unit pointer.
        /// </summary>
        static public CreatureType GetCreatureType(IntPtr unitPtr)
        {
            return gameFunctionHandler.GetCreatureType(unitPtr);
        }

        /// <summary>
        /// Retrieves the cache entry for a specific item.
        /// </summary>
        static public IntPtr GetItemCacheEntry(int itemId, ulong guid = 0)
        {
            return gameFunctionHandler.GetItemCacheEntry(itemId, guid);
        }

        /// <summary>
        /// Retrieves the pointer to the object with the specified GUID.
        /// </summary>
        static public IntPtr GetObjectPtr(ulong guid)
        {
            return gameFunctionHandler.GetObjectPtr(guid);
        }

        /// <summary>
        /// Retrieves the player's GUID.
        /// </summary>
        static public ulong GetPlayerGuid()
        {
            return gameFunctionHandler.GetPlayerGuid();
        }

        /// <summary>
        /// Gets the unit reaction between two units.
        /// </summary>
        static public UnitReaction GetUnitReaction(IntPtr unitPtr1, IntPtr unitPtr2)
        {
            return gameFunctionHandler.GetUnitReaction(unitPtr1, unitPtr2);
        }

        /// <summary>
        /// Returns the intersection point between the given start and end positions.
        /// </summary>
        static public XYZ Intersect(Position start, Position end)
        {
            return gameFunctionHandler.Intersect(start, end);
        }

        /// <summary>
        /// Checks if a spell is currently on cooldown.
        /// </summary>
        static public bool IsSpellOnCooldown(int spellId)
        {
            return gameFunctionHandler.IsSpellOnCooldown(spellId);
        }

        /// <summary>
        /// Calls the Jump function in the gameFunctionHandler.
        /// </summary>
        static public void Jump()
        {
            gameFunctionHandler.Jump();
        }

        /// <summary>
        /// Calls a Lua function with the specified code.
        /// </summary>
        static public void LuaCall(string code)
        {
            gameFunctionHandler.LuaCall(code);
        }

        /// <summary>
        /// Executes a Lua code with placeholders and returns an array of results.
        /// </summary>
        static public string[] LuaCallWithResult(string code)
        {
            var luaVarNames = new List<string>();
            for (var i = 0; i < 11; i++)
            {
                var currentPlaceHolder = "{" + i + "}";
                if (!code.Contains(currentPlaceHolder)) break;
                var randomName = GetRandomLuaVarName();
                code = code.Replace(currentPlaceHolder, randomName);
                luaVarNames.Add(randomName);
            }

            LuaCall(code);

            var results = new List<string>();
            foreach (var varName in luaVarNames)
            {
                var address = gameFunctionHandler.GetText(varName);
                results.Add(MemoryManager.ReadString(address));
            }

            return results.ToArray();
        }

        /// <summary>
        /// Retrieves a Spell database entry based on the specified index.
        /// </summary>
        static public Spell GetSpellDBEntry(int index)
        {
            return gameFunctionHandler.GetSpellDBEntry(index);
        }

        /// <summary>
        /// Releases a corpse with the specified pointer.
        /// </summary>
        static public void ReleaseCorpse(IntPtr ptr)
        {
            gameFunctionHandler.ReleaseCorpse(ptr);
        }

        /// <summary>
        /// Retrieves the player's corpse.
        /// </summary>
        static public void RetrieveCorpse()
        {
            gameFunctionHandler.RetrieveCorpse();
        }

        /// <summary>
        /// Calls the gameFunctionHandler to loot the specified slot.
        /// </summary>
        static public void LootSlot(int slot)
        {
            gameFunctionHandler.LootSlot(slot);
        }

        /// <summary>
        /// Sets the target for the game function handler using the specified GUID.
        /// </summary>
        static public void SetTarget(ulong guid)
        {
            gameFunctionHandler.SetTarget(guid);
        }

        /// <summary>
        /// Sells an item by its unique identifier.
        /// </summary>
        static public void SellItemByGuid(uint itemCount, ulong vendorGuid, ulong itemGuid)
        {
            gameFunctionHandler.SellItemByGuid(itemCount, vendorGuid, itemGuid);
        }

        /// <summary>
        /// Sends a movement update to the game server.
        /// </summary>
        static public void SendMovementUpdate(IntPtr playerPtr, int opcode)
        {
            gameFunctionHandler.SendMovementUpdate(playerPtr, opcode);
        }

        /// <summary>
        /// Sets the control bit for the game function handler.
        /// </summary>
        static public void SetControlBit(int bit, int state, int tickCount)
        {
            gameFunctionHandler.SetControlBit(bit, state, tickCount);
        }

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
        static public void SetFacing(IntPtr playerSetFacingPtr, float facing)
        {
            gameFunctionHandler.SetFacing(playerSetFacingPtr, facing);
        }

        /// <summary>
        /// Uses the specified item.
        /// </summary>
        static public void UseItem(IntPtr itemPtr)
        {
            gameFunctionHandler.UseItem(itemPtr);
        }

        /// <summary>
        /// Retrieves a pointer to the specified row in the table.
        /// </summary>
        static public IntPtr GetRow(IntPtr tablePtr, int index)
        {
            return gameFunctionHandler.GetRow(tablePtr, index);
        }

        /// <summary>
        /// Retrieves a localized row from a table.
        /// </summary>
        static public IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr)
        {
            return gameFunctionHandler.GetLocalizedRow(tablePtr, index, rowPtr);
        }

        /// <summary>
        /// Generates a random Lua variable name consisting of 8 lowercase alphabetic characters.
        /// </summary>
        static string GetRandomLuaVarName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            return new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(8).ToArray());
        }
    }
}
