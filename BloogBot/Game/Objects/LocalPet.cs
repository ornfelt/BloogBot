using BloogBot.Game.Enums;
using System;

/// <summary>
/// This namespace contains classes related to local pets in the game.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a local pet in the World of Warcraft game.
    /// </summary>
    /// <summary>
    /// Represents a local pet in the World of Warcraft game.
    /// </summary>
    public class LocalPet : WoWUnit
    {
        /// <summary>
        /// Initializes a new instance of the LocalPet class.
        /// </summary>
        internal LocalPet(
                    IntPtr pointer,
                    ulong guid,
                    ObjectType objectType)
                    : base(pointer, guid, objectType)
        {
        }

        /// <summary>
        /// Calls the Lua function "PetAttack()" to initiate an attack.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Attack Function" as A
        /// participant "LuaCall Function" as B
        /// A -> B: PetAttack()
        /// \enduml
        /// </remarks>
        public void Attack() => Functions.LuaCall("PetAttack()");

        /// <summary>
        /// Makes the pet follow the player.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Game: -> Functions: LuaCall("PetFollow()")
        /// \enduml
        /// </remarks>
        public void FollowPlayer() => Functions.LuaCall("PetFollow()");

        /// <summary>
        /// Determines if the pet is happy.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "IsHappy() : bool" as A
        /// participant "Functions" as B
        /// A -> B: LuaCallWithResult(getPetHappiness)
        /// B --> A: result
        /// A -> A: Trim and compare result with "3"
        /// \enduml
        /// </remarks>
        public bool IsHappy()
        {
            const string getPetHappiness = "happiness, damagePercentage, loyaltyRate = GetPetHappiness(); {0} = happiness;";
            var result = Functions.LuaCallWithResult(getPetHappiness);
            return result[0].Trim().Equals("3");
        }

        /// <summary>
        /// Determines if the specified pet spell can be used.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "CanUse Function" as CanUse
        /// participant "LuaCallWithResult Function" as LuaCall
        /// 
        /// CanUse -> LuaCall: getPetSpellCd1 + parPetSpell + getPetSpellCd2
        /// activate LuaCall
        /// LuaCall --> CanUse: result
        /// deactivate LuaCall
        /// 
        /// CanUse -> CanUse: Check if result[0].Trim().Equals("0")
        /// \enduml
        /// </remarks>
        public bool CanUse(string parPetSpell)
        {
            const string getPetSpellCd1 = "{0} = 0; for index = 1,11,1 do {1} = GetPetActionInfo(index); if {1} == '";
            const string getPetSpellCd2 = "' then startTime, duration, enable = GetPetActionCooldown(index); PetSpellEnabled = duration; end end";

            var result = Functions.LuaCallWithResult(getPetSpellCd1 + parPetSpell + getPetSpellCd2);

            return result[0].Trim().Equals("0");
        }

        /// <summary>
        /// Casts a specified pet spell.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Cast(parPetSpell)|
        /// :castPetSpell1|
        /// :castPetSpell2|
        /// :LuaCall(castPetSpell1 + parPetSpell + castPetSpell2)|
        /// \enduml
        /// </remarks>
        public void Cast(string parPetSpell)
        {
            const string castPetSpell1 = "for index = 1,11,1 do curName = GetPetActionInfo(index); if curName == '";
            const string castPetSpell2 = "' then CastPetAction(index); break end end";

            Functions.LuaCall(castPetSpell1 + parPetSpell + castPetSpell2);
        }
    }
}
