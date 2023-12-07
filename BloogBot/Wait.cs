using System;
using System.Collections.Concurrent;

/// <summary>
/// This class provides methods for waiting for a specific condition to be met.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a static class that provides methods for waiting.
    /// </summary>
    /// <summary>
    /// Represents a static class that provides methods for waiting.
    /// </summary>
    public static class Wait
    {
        /// <summary>
        /// Represents a thread-safe collection of items stored in a dictionary.
        /// </summary>
        static readonly ConcurrentDictionary<string, Item> Items = new ConcurrentDictionary<string, Item>();
        /// <summary>
        /// Represents a static object used for locking purposes.
        /// </summary>
        static readonly object _lock = new object();

        /// <summary>
        /// Checks if an item with the specified name exists in the Items dictionary. If it doesn't exist, a new item is created and added to the dictionary. 
        /// If the item exists and the elapsed time since it was added is greater than or equal to the specified milliseconds, the item is removed from the dictionary.
        /// </summary>
        /// <param name="parName">The name of the item to check.</param>
        /// <param name="parMs">The number of milliseconds to compare against the elapsed time since the item was added.</param>
        /// <param name="trueOnNonExist">Optional. Specifies whether to return true if the item doesn't exist in the dictionary. Default is false.</param>
        /// <returns>True if the item exists and the elapsed time is greater than or equal to the specified milliseconds; otherwise, false.</returns>
        /// <remarks>
        /// \startuml
        /// participant "For Method" as For
        /// participant "Item Dictionary" as Items
        /// participant "Item" as Item
        /// 
        /// For -> Items: TryGetValue(parName)
        /// activate Items
        /// Items --> For: Return tmpItem
        /// deactivate Items
        /// 
        /// alt tmpItem does not exist
        ///     For -> Item: new Item()
        ///     activate Item
        ///     Item --> For: Return new Item
        ///     deactivate Item
        ///     For -> Items: TryAdd(parName, tmpItem)
        ///     activate Items
        ///     Items --> For: Return trueOnNonExist
        ///     deactivate Items
        /// else tmpItem exists
        ///     For -> For: Calculate elapsed
        ///     alt elapsed is true
        ///         For -> Items: TryRemove(parName, tmpItem)
        ///         activate Items
        ///         Items --> For: Return elapsed
        ///         deactivate Items
        ///     else elapsed is false
        ///         For --> For: Return elapsed
        ///     end
        /// end
        /// \enduml
        /// </remarks>
        public static bool For(string parName, int parMs, bool trueOnNonExist = false)
        {
            lock (_lock)
            {
                if (!Items.TryGetValue(parName, out Item tmpItem))
                {
                    tmpItem = new Item();
                    Items.TryAdd(parName, tmpItem);
                    return trueOnNonExist;
                }
                var elapsed = (DateTime.UtcNow - tmpItem.Added).TotalMilliseconds >= parMs;
                if (elapsed)
                {
                    Items.TryRemove(parName, out tmpItem);
                }
                return elapsed;
            }
        }

        /// <summary>
        /// Removes an item from the collection based on the specified name.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "Remove Method" as R
        /// participant "Items Dictionary" as D
        /// C -> R: Remove(parName)
        /// activate R
        /// R -> D: TryRemove(parName)
        /// D --> R: tmp
        /// deactivate R
        /// \enduml
        /// </remarks>
        public static void Remove(string parName)
        {
            lock (_lock)
            {
                Items.TryRemove(parName, out Item tmp);
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "RemoveAll Function" as R
        /// participant "Items List" as I
        /// 
        /// R -> I: Clear()
        /// \enduml
        /// </remarks>
        public static void RemoveAll()
        {
            lock (_lock)
            {
                Items.Clear();
            }
        }

        /// <summary>
        /// Represents an item with a specific added date and time.
        /// </summary>
        class Item
        {
            /// <summary>
            /// Gets the date and time when the item was added in UTC format.
            /// </summary>
            internal DateTime Added { get; } = DateTime.UtcNow;
        }
    }
}
