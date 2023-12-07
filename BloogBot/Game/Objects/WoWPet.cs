using BloogBot.Game.Enums;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a World of Warcraft pet.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a pet in the World of Warcraft game.
    /// </summary>
    class WoWPet : WoWUnit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WoWPet"/> class.
        /// </summary>
        internal WoWPet(
                     IntPtr pointer,
                     ulong guid,
                     ObjectType objectType) : base(pointer, guid, objectType)
        {
            // TODO
            //RefreshSpells();
        }

        /// <summary>
        /// Dictionary that stores pet spells for each pet name.
        /// </summary>
        readonly IDictionary<string, int[]> petSpells = new Dictionary<string, int[]>();

        /// <summary>
        /// Checks if the pet knows a specific spell by its name.
        /// </summary>
        public bool KnowsSpell(string name) => petSpells.ContainsKey(name);

        /// <summary>
        /// Calls the Lua function "PetAttack()" to initiate a pet attack.
        /// </summary>
        public void Attack() => LuaCall("PetAttack()");

        /// <summary>
        /// Refreshes the list of spells for the pet.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// activate "RefreshSpells"
        /// "RefreshSpells" -> "petSpells": Clear()
        /// loop 1024 times
        ///     "RefreshSpells" -> "MemoryManager": ReadInt((IntPtr)(MemoryAddresses.WoWPet_SpellsBase + 4 * i))
        ///     "MemoryManager" --> "RefreshSpells": currentSpellId
        ///     "RefreshSpells" -> "Functions": GetSpellDBEntry(currentSpellId)
        ///     "Functions" --> "RefreshSpells": spell
        ///     "RefreshSpells" -> "petSpells": ContainsKey(spell.Name)
        ///     "petSpells" --> "RefreshSpells": bool
        ///     alt petSpells contains spell.Name
        ///         "RefreshSpells" -> "petSpells": Update spell.Name with new currentSpellId
        ///     else
        ///         "RefreshSpells" -> "petSpells": Add spell.Name with currentSpellId
        ///     end
        /// end
        /// deactivate "RefreshSpells"
        /// \enduml
        /// </remarks>
        public void RefreshSpells()
        {
            petSpells.Clear();
            for (var i = 0; i < 1024; i++)
            {
                var currentSpellId = MemoryManager.ReadInt((IntPtr)(MemoryAddresses.WoWPet_SpellsBase + 4 * i));
                if (currentSpellId == 0) break;
                var spell = Functions.GetSpellDBEntry(currentSpellId);

                if (petSpells.ContainsKey(spell.Name))
                    petSpells[spell.Name] = new List<int>(petSpells[spell.Name])
                    {
                        currentSpellId
                    }.ToArray();
                else
                    petSpells.Add(spell.Name, new[] { currentSpellId });
            }
        }
    }
}
