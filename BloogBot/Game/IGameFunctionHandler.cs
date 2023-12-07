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
        /// <remarks>
        /// \startuml
        /// :Caller: -> EnumerateVisibleObjects: callback, filter
        /// \enduml
        /// </remarks>
        void EnumerateVisibleObjects(IntPtr callback, int filter);

        /// <summary>
        /// Retrieves the pointer to the object with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetObjectPtr" as G
        /// C -> G: GetObjectPtr(guid)
        /// G --> C: return IntPtr
        /// \enduml
        /// </remarks>
        IntPtr GetObjectPtr(ulong guid);

        /// <summary>
        /// Retrieves the unique identifier for the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Game: -> GetPlayerGuid: Call function
        /// GetPlayerGuid --> :Game: Return Player GUID
        /// \enduml
        /// </remarks>
        ulong GetPlayerGuid();

        /// <summary>
        /// Sets the facing direction of the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Game: -> SetFacing: playerSetFacingPtr, facing
        /// \enduml
        /// </remarks>
        void SetFacing(IntPtr playerSetFacingPtr, float facing);

        /// <summary>
        /// Sends a movement update to the specified player.
        /// </summary>
        /// <param name="playerPtr">The pointer to the player.</param>
        /// <param name="opcode">The opcode for the movement update.</param>
        /// <remarks>
        /// \startuml
        /// :Game: -> SendMovementUpdate: playerPtr, opcode
        /// \enduml
        /// </remarks>
        void SendMovementUpdate(IntPtr playerPtr, int opcode);

        /// <summary>
        /// Sets the specified control bit to the given state after the specified tick count.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "SetControlBit" as S
        /// C -> S: SetControlBit(bit, state, tickCount)
        /// \enduml
        /// </remarks>
        void SetControlBit(int bit, int state, int tickCount);

        /// <summary>
        /// Jumps to a new location.
        /// </summary>
        /// <remarks>
        /// \startuml
        ///  :User: -> :System: : Jump()
        /// \enduml
        /// </remarks>
        void Jump();

        /// <summary>
        /// Retrieves the type of creature based on the given unit pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> CreatureType: GetCreatureType(unitPtr)
        /// \enduml
        /// </remarks>
        CreatureType GetCreatureType(IntPtr unitPtr);

        /// <summary>
        /// Retrieves the rank of a creature based on its unit pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> GetCreatureRank: unitPtr
        /// GetCreatureRank --> User: int
        /// \enduml
        /// </remarks>
        int GetCreatureRank(IntPtr unitPtr);

        /// <summary>
        /// Retrieves the reaction between two units.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "unitPtr1" as A
        /// participant "unitPtr2" as B
        /// participant "UnitReaction" as C
        /// A -> B: GetUnitReaction
        /// B --> C: Return UnitReaction
        /// \enduml
        /// </remarks>
        UnitReaction GetUnitReaction(IntPtr unitPtr1, IntPtr unitPtr2);

        /// <summary>
        /// Executes a Lua script.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// App -> Lua: LuaCall(code)
        /// \enduml
        /// </remarks>
        void LuaCall(string code);

        /// <summary>
        /// Retrieves the text associated with the specified variable name.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :User: -> GetText: varName
        /// GetText --> :User: IntPtr
        /// \enduml
        /// </remarks>
        IntPtr GetText(string varName);

        /// <summary>
        /// Casts a spell by its ID on a target with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caster" as C
        /// participant "Target" as T
        /// C -> T: CastSpellById(spellId, targetGuid)
        /// \enduml
        /// </remarks>
        int CastSpellById(int spellId, ulong targetGuid);

        /// <summary>
        /// Retrieves the spell database entry at the specified index.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Client" as C
        /// participant "Server" as S
        /// C -> S: GetSpellDBEntry(index)
        /// S --> C: Spell
        /// \enduml
        /// </remarks>
        Spell GetSpellDBEntry(int index);

        /// <summary>
        /// Calculates the intersection point between two positions.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Position -> XYZ: start
        /// Position -> XYZ: end
        /// XYZ --> Position: Intersect
        /// \enduml
        /// </remarks>
        XYZ Intersect(Position start, Position end);

        /// <summary>
        /// Sets the target with the specified GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> SetTarget: guid
        /// \enduml
        /// </remarks>
        void SetTarget(ulong guid);

        /// <summary>
        /// Retrieves the corpse.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> System: RetrieveCorpse()
        /// \enduml
        /// </remarks>
        void RetrieveCorpse();

        /// <summary>
        /// Releases the memory allocated for a corpse object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Application -> MemoryManager: ReleaseCorpse(ptr)
        /// \enduml
        /// </remarks>
        void ReleaseCorpse(IntPtr ptr);

        /// <summary>
        /// Retrieves the cache entry for the specified item ID and GUID.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetItemCacheEntry" as G
        /// C -> G: GetItemCacheEntry(itemId, guid)
        /// G --> C: Returns IntPtr
        /// \enduml
        /// </remarks>
        IntPtr GetItemCacheEntry(int itemId, ulong guid = 0);

        /// <summary>
        /// Checks if a spell with the specified ID is currently on cooldown.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> System: IsSpellOnCooldown(spellId)
        /// System --> User: bool
        /// \enduml
        /// </remarks>
        bool IsSpellOnCooldown(int spellId);

        /// <summary>
        /// Loots the specified slot.
        /// </summary>
        /// <param name="slot">The slot to loot.</param>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> System: LootSlot(slot)
        /// \enduml
        /// </remarks>
        void LootSlot(int slot);

        /// <summary>
        /// Uses the item pointed to by the specified pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> System: UseItem(itemPtr)
        /// \enduml
        /// </remarks>
        void UseItem(IntPtr itemPtr);

        /// <summary>
        /// Sells the specified number of items with the given itemGuid to the vendor with the specified vendorGuid.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Item Seller" as A
        /// participant "Vendor" as B
        /// A -> B: SellItemByGuid(itemCount, vendorGuid, itemGuid)
        /// \enduml
        /// </remarks>
        void SellItemByGuid(uint itemCount, ulong vendorGuid, ulong itemGuid);

        /// <summary>
        /// Buys a specified quantity of an item from a vendor.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Client" as C
        /// participant "Server" as S
        /// C -> S: BuyVendorItem(vendorGuid, itemId, quantity)
        /// \enduml
        /// </remarks>
        void BuyVendorItem(ulong vendorGuid, int itemId, int quantity);

        /// <summary>
        /// Dismounts the unit specified by the given pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> Dismount: unitPtr
        /// \enduml
        /// </remarks>
        int Dismount(IntPtr unitPtr);

        /// <summary>
        /// Casts a spell at the specified position.
        /// </summary>
        /// <param name="spellName">The name of the spell to cast.</param>
        /// <param name="position">The position to cast the spell at.</param>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "Function" as F
        /// C -> F: CastAtPosition(spellName, position)
        /// \enduml
        /// </remarks>
        void CastAtPosition(string spellName, Position position);

        /// <summary>
        /// Retrieves the number of auras associated with the specified unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// User -> GetAuraCount: unitPtr
        /// GetAuraCount --> User: return aura count
        /// \enduml
        /// </remarks>
        int GetAuraCount(IntPtr unitPtr);

        /// <summary>
        /// Retrieves the pointer to the aura at the specified index for the given unit.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> GetAuraPointer: unitPtr, index
        /// GetAuraPointer --> :Program: IntPtr
        /// \enduml
        /// </remarks>
        IntPtr GetAuraPointer(IntPtr unitPtr, int index);

        /// <summary>
        /// Retrieves the pointer to the row at the specified index in the table.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "GetRow Function" as F
        /// C -> F: GetRow(tablePtr, index)
        /// F --> C: return IntPtr
        /// \enduml
        /// </remarks>
        IntPtr GetRow(IntPtr tablePtr, int index);

        /// <summary>
        /// Retrieves a localized row from a table at the specified index.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// actor User
        /// User -> GetLocalizedRow: tablePtr, index, rowPtr
        /// GetLocalizedRow --> User: IntPtr
        /// \enduml
        /// </remarks>
        IntPtr GetLocalizedRow(IntPtr tablePtr, int index, IntPtr rowPtr);
    }
}
