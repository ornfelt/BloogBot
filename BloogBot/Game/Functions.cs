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
        /// <remarks>
        /// \startuml
        /// participant "BuyVendorItem Function" as B
        /// participant "GameFunctionHandler" as G
        /// B -> G: BuyVendorItem(vendorGuid, itemId, quantity)
        /// \enduml
        /// </remarks>
        static public void BuyVendorItem(ulong vendorGuid, int itemId, int quantity)
        {
            gameFunctionHandler.BuyVendorItem(vendorGuid, itemId, quantity);
        }

        /// <summary>
        /// Casts a spell at a specified position.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "gameFunctionHandler" as G
        /// participant "CastAtPosition" as C
        /// C -> G: CastAtPosition(spellName, position)
        /// \enduml
        /// </remarks>
        static public void CastAtPosition(string spellName, Position position)
        {
            gameFunctionHandler.CastAtPosition(spellName, position);
        }

        /// <summary>
        /// Gets the count of auras for a given unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetAuraCount Function" as GAC
        /// participant "GameFunctionHandler" as GFH
        /// GAC -> GFH: GetAuraCount(unitPtr)
        /// GFH --> GAC: return aura count
        /// \enduml
        /// </remarks>
        static public int GetAuraCount(IntPtr unitPtr)
        {
            return gameFunctionHandler.GetAuraCount(unitPtr);
        }

        /// <summary>
        /// Retrieves the pointer to the aura at the specified index for the given unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "unitPtr" as A
        /// participant "index" as B
        /// participant "gameFunctionHandler" as C
        /// A -> C: GetAuraPointer(unitPtr, index)
        /// C --> A: return
        /// \enduml
        /// </remarks>
        static public IntPtr GetAuraPointer(IntPtr unitPtr, int index)
        {
            return gameFunctionHandler.GetAuraPointer(unitPtr, index);
        }

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GameFunctionHandler" as G
        /// C -> G: CastSpellById(spellId, targetGuid)
        /// G --> C: return
        /// \enduml
        /// </remarks>
        static public int CastSpellById(int spellId, ulong targetGuid)
        {
            return gameFunctionHandler.CastSpellById(spellId, targetGuid);
        }

        /// <summary>
        /// Dismounts a unit specified by the given pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor "Caller" as c
        /// entity "Dismount Method" as d
        /// entity "gameFunctionHandler" as g
        /// c -> d : Call Dismount(unitPtr)
        /// d -> g : Dismount(unitPtr)
        /// g --> d : Return result
        /// d --> c : Return result
        /// \enduml
        /// </remarks>
        static public int Dismount(IntPtr unitPtr)
        {
            return gameFunctionHandler.Dismount(unitPtr);
        }

        /// <summary>
        /// Enumerates the visible objects using the specified callback and filter.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> EnumerateVisibleObjects : callback, filter
        /// EnumerateVisibleObjects -> gameFunctionHandler : EnumerateVisibleObjects(callback, filter)
        /// \enduml
        /// </remarks>
        static public void EnumerateVisibleObjects(IntPtr callback, int filter)
        {
            gameFunctionHandler.EnumerateVisibleObjects(callback, filter);
        }

        /// <summary>
        /// Gets the rank of a creature.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetCreatureRank Function" as A
        /// participant "gameFunctionHandler" as B
        /// A -> B: GetCreatureRank(unitPtr)
        /// B --> A: return rank
        /// \enduml
        /// </remarks>
        static public int GetCreatureRank(IntPtr unitPtr)
        {
            return gameFunctionHandler.GetCreatureRank(unitPtr);
        }

        /// <summary>
        /// Gets the type of creature based on the provided unit pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetCreatureType Function" as A
        /// participant "GameFunctionHandler" as B
        /// A -> B: GetCreatureType(unitPtr)
        /// B --> A: return CreatureType
        /// \enduml
        /// </remarks>
        static public CreatureType GetCreatureType(IntPtr unitPtr)
        {
            return gameFunctionHandler.GetCreatureType(unitPtr);
        }

        /// <summary>
        /// Retrieves the cache entry for a specific item.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetItemCacheEntry" as G
        /// participant "gameFunctionHandler.GetItemCacheEntry" as GFH
        /// C -> G: GetItemCacheEntry(itemId, guid)
        /// G -> GFH: GetItemCacheEntry(itemId, guid)
        /// GFH --> G: return
        /// G --> C: return
        /// \enduml
        /// </remarks>
        static public IntPtr GetItemCacheEntry(int itemId, ulong guid = 0)
        {
            return gameFunctionHandler.GetItemCacheEntry(itemId, guid);
        }

        /// <summary>
        /// Retrieves the pointer to the object with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetObjectPtr Function" as F
        /// participant "gameFunctionHandler" as G
        /// C -> F: GetObjectPtr(guid)
        /// F -> G: GetObjectPtr(guid)
        /// G --> F: Returns IntPtr
        /// F --> C: Returns IntPtr
        /// \enduml
        /// </remarks>
        static public IntPtr GetObjectPtr(ulong guid)
        {
            return gameFunctionHandler.GetObjectPtr(guid);
        }

        /// <summary>
        /// Retrieves the player's GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// GetPlayerGuid -> gameFunctionHandler: GetPlayerGuid()
        /// gameFunctionHandler --> GetPlayerGuid: return PlayerGuid
        /// \enduml
        /// </remarks>
        static public ulong GetPlayerGuid()
        {
            return gameFunctionHandler.GetPlayerGuid();
        }

        /// <summary>
        /// Gets the unit reaction between two units.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor "unitPtr1" as a
        /// actor "unitPtr2" as b
        /// a -> b: GetUnitReaction(unitPtr1, unitPtr2)
        /// b --> a: return UnitReaction
        /// \enduml
        /// </remarks>
        static public UnitReaction GetUnitReaction(IntPtr unitPtr1, IntPtr unitPtr2)
        {
            return gameFunctionHandler.GetUnitReaction(unitPtr1, unitPtr2);
        }

        /// <summary>
        /// Returns the intersection point between the given start and end positions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Position -> XYZ: start
        /// Position -> XYZ: end
        /// XYZ --> gameFunctionHandler: Intersect(start, end)
        /// \enduml
        /// </remarks>
        static public XYZ Intersect(Position start, Position end)
        {
            return gameFunctionHandler.Intersect(start, end);
        }

        /// <summary>
        /// Checks if a spell is currently on cooldown.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "IsSpellOnCooldown Function" as A
        /// participant "gameFunctionHandler" as B
        /// A -> B: IsSpellOnCooldown(spellId)
        /// B --> A: return
        /// \enduml
        /// </remarks>
        static public bool IsSpellOnCooldown(int spellId)
        {
            return gameFunctionHandler.IsSpellOnCooldown(spellId);
        }

        /// <summary>
        /// Calls the Jump function in the gameFunctionHandler.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "gameFunctionHandler" as A
        /// participant "Jump()" as B
        /// B -> A: Jump()
        /// \enduml
        /// </remarks>
        static public void Jump()
        {
            gameFunctionHandler.Jump();
        }

        /// <summary>
        /// Calls a Lua function with the specified code.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "gameFunctionHandler" as A
        /// participant "LuaCall(string code)" as B
        /// B -> A: LuaCall(code)
        /// \enduml
        /// </remarks>
        static public void LuaCall(string code)
        {
            gameFunctionHandler.LuaCall(code);
        }

        /// <summary>
        /// Executes a Lua code with placeholders and returns an array of results.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "LuaCallWithResult()" as L
        /// participant "GetRandomLuaVarName()" as G
        /// participant "LuaCall()" as LC
        /// participant "gameFunctionHandler.GetText()" as GFH
        /// participant "MemoryManager.ReadString()" as MM
        /// 
        /// L -> G: Get random Lua variable name
        /// loop 11 times
        ///   L -> G: Get random Lua variable name
        ///   L -> L: Replace placeholders with random names
        /// end
        /// L -> LC: Call Lua code
        /// loop for each Lua variable name
        ///   L -> GFH: Get text of variable
        ///   L -> MM: Read string from memory
        /// end
        /// L -> L: Return results as array
        /// \enduml
        /// </remarks>
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
        /// <remarks>
        /// \startuml
        /// participant "GetSpellDBEntry Function" as A
        /// participant "gameFunctionHandler" as B
        /// A -> B: GetSpellDBEntry(index)
        /// B --> A: return Spell
        /// \enduml
        /// </remarks>
        static public Spell GetSpellDBEntry(int index)
        {
            return gameFunctionHandler.GetSpellDBEntry(index);
        }

        /// <summary>
        /// Releases a corpse with the specified pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> gameFunctionHandler: ReleaseCorpse(ptr)
        /// \enduml
        /// </remarks>
        static public void ReleaseCorpse(IntPtr ptr)
        {
            gameFunctionHandler.ReleaseCorpse(ptr);
        }

        /// <summary>
        /// Retrieves the player's corpse.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GameFunctionHandler" as G
        /// participant "RetrieveCorpse Function" as R
        /// R -> G: RetrieveCorpse()
        /// \enduml
        /// </remarks>
        static public void RetrieveCorpse()
        {
            gameFunctionHandler.RetrieveCorpse();
        }

        /// <summary>
        /// Calls the gameFunctionHandler to loot the specified slot.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "gameFunctionHandler" as A
        /// participant "LootSlot" as B
        /// B -> A: LootSlot(slot)
        /// \enduml
        /// </remarks>
        static public void LootSlot(int slot)
        {
            gameFunctionHandler.LootSlot(slot);
        }

        /// <summary>
        /// Sets the target for the game function handler using the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "SetTarget Method" as A
        /// participant "gameFunctionHandler" as B
        /// A -> B: SetTarget(guid)
        /// \enduml
        /// </remarks>
        static public void SetTarget(ulong guid)
        {
            gameFunctionHandler.SetTarget(guid);
        }

        /// <summary>
        /// Sells an item by its unique identifier.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "gameFunctionHandler" as A
        /// participant "SellItemByGuid" as B
        /// B -> A: SellItemByGuid(itemCount, vendorGuid, itemGuid)
        /// \enduml
        /// </remarks>
        static public void SellItemByGuid(uint itemCount, ulong vendorGuid, ulong itemGuid)
        {
            gameFunctionHandler.SellItemByGuid(itemCount, vendorGuid, itemGuid);
        }

        /// <summary>
        /// Sends a movement update to the game server.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "playerPtr" as A
        /// participant "gameFunctionHandler" as B
        /// A -> B: SendMovementUpdate(playerPtr, opcode)
        /// \enduml
        /// </remarks>
        static public void SendMovementUpdate(IntPtr playerPtr, int opcode)
        {
            gameFunctionHandler.SendMovementUpdate(playerPtr, opcode);
        }

        /// <summary>
        /// Sets the control bit for the game function handler.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "SetControlBit Function" as SCB
        /// participant "GameFunctionHandler" as GFH
        /// 
        /// SCB -> GFH: SetControlBit(bit, state, tickCount)
        /// \enduml
        /// </remarks>
        static public void SetControlBit(int bit, int state, int tickCount)
        {
            gameFunctionHandler.SetControlBit(bit, state, tickCount);
        }

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "SetFacing Method" as A
        /// participant "gameFunctionHandler" as B
        /// A -> B: SetFacing(playerSetFacingPtr, facing)
        /// \enduml
        /// </remarks>
        static public void SetFacing(IntPtr playerSetFacingPtr, float facing)
        {
            gameFunctionHandler.SetFacing(playerSetFacingPtr, facing);
        }

        /// <summary>
        /// Uses the specified item.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> gameFunctionHandler: UseItem(itemPtr)
        /// \enduml
        /// </remarks>
        static public void UseItem(IntPtr itemPtr)
        {
            gameFunctionHandler.UseItem(itemPtr);
        }

        /// <summary>
        /// Retrieves a pointer to the specified row in the table.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> GetRow: tablePtr, index
        /// GetRow -> gameFunctionHandler: GetRow(tablePtr, index)
        /// gameFunctionHandler --> GetRow: IntPtr
        /// GetRow --> User: IntPtr
        /// \enduml
        /// </remarks>
        static public IntPtr GetRow(IntPtr tablePtr, int index)
        {
            return gameFunctionHandler.GetRow(tablePtr, index);
        }

        /// <summary>
        /// Retrieves a localized row from a table.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetLocalizedRow" as G
        /// participant "gameFunctionHandler" as GFH
        /// 
        /// C -> G: GetLocalizedRow(tablePtr, index, rowPtr)
        /// G -> GFH: GetLocalizedRow(tablePtr, index, rowPtr)
        /// GFH --> G: return
        /// G --> C: return
        /// \enduml
        /// </remarks>
        static public IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr)
        {
            return gameFunctionHandler.GetLocalizedRow(tablePtr, index, rowPtr);
        }

        /// <summary>
        /// Generates a random Lua variable name consisting of 8 lowercase alphabetic characters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "GetRandomLuaVarName Method" as Method
        /// participant "Random Object" as Random
        /// participant "String Object" as String
        /// 
        /// Method -> Random: Generate random number
        /// Random --> Method: Return random number
        /// Method -> String: Select character at random index
        /// String --> Method: Return selected character
        /// Method -> String: Repeat selection 8 times
        /// String --> Method: Return final string
        /// \enduml
        /// </remarks>
        static string GetRandomLuaVarName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            return new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(8).ToArray());
        }
    }
}
