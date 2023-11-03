// Friday owns this file!

using BeastMasterHunterBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This namespace contains classes related to managing the pet of the Beastmaster Hunter bot.
/// </summary>
namespace BeastmasterHunterBot
{
    /// <summary>
    /// Represents the state of the pet manager.
    /// </summary>
    /// <summary>
    /// Represents the state of the pet manager.
    /// </summary>
    class PetManagerState : IBotState
    {
        /// <summary>
        /// Represents the constant string "Call Pet".
        /// </summary>
        const string CallPet = "Call Pet";
        /// <summary>
        /// Represents the constant string "Revive Pet".
        /// </summary>
        const string RevivePet = "Revive Pet";
        /// <summary>
        /// Represents the action of feeding a pet.
        /// </summary>
        const string FeedPet = "Feed Pet";


        /// <summary>
        /// Represents a readonly stack of IBotState objects.
        /// </summary>
        readonly Stack<IBotState> botStates;
        /// <summary>
        /// Represents a read-only dependency container.
        /// </summary>
        readonly IDependencyContainer container;
        /// <summary>
        /// Represents a readonly instance of the LocalPlayer class.
        /// </summary>
        readonly LocalPlayer player;
        /// <summary>
        /// Represents a local pet that cannot be modified.
        /// </summary>
        readonly LocalPet pet;

        /// <summary>
        /// Initializes a new instance of the <see cref="PetManagerState"/> class.
        /// </summary>
        /// <param name="botStates">The stack of bot states.</param>
        /// <param name="container">The dependency container.</param>
        public PetManagerState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            pet = ObjectManager.Pet;
        }

        /// <summary>
        /// Updates the player's state by checking if they are currently casting a spell. If not, it checks if the player knows the spell "CallPet" and if the pet object is not null. If either condition is true, the player stands, pops the current state from the stack, pushes a new "BuffSelfState" onto the stack, and returns. Otherwise, it calls the Lua function "CastSpellByName" with the parameter "CallPet".
        /// </summary>
        public void Update()
        {
            if (player.IsCasting)
                return;

            if (!player.KnowsSpell(CallPet) || ObjectManager.Pet != null)
            {
                player.Stand();
                botStates.Pop();
                botStates.Push(new BuffSelfState(botStates, container));
                return;
            }

            player.LuaCall($"CastSpellByName('{CallPet}')");
        }

        /// <summary>
        /// Feeds the pet with the specified food name.
        /// </summary>
        public void Feed(string parFoodName)
        {
            if (true /*Inventory.Instance.GetItemCount(parFoodName) != 0*/)
            {
                const string checkFeedPet = "{0} = 0; if CursorHasSpell() then CanFeedMyPet = 1 end;";
                var result = player.LuaCallWithResults(checkFeedPet);
                if (result[0].Trim().Contains("0"))
                {
                    const string feedPet = "CastSpellByName('Feed Pet'); TargetUnit('Pet');";
                    player.LuaCall(feedPet);
                }
                const string usePetFood1 =
                    "for bag = 0,4 do for slot = 1,GetContainerNumSlots(bag) do local item = GetContainerItemLink(bag,slot) if item then if string.find(item, '";
                const string usePetFood2 = "') then PickupContainerItem(bag,slot) break end end end end";
                player.LuaCall(usePetFood1 + parFoodName.Replace("'", "\\'") + usePetFood2);
            }
            player.LuaCall("ClearCursor()");
        }
    }
}
