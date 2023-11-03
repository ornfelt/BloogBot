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
