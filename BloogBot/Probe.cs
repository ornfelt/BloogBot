using System;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes for handling bot probes.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// The Probe class is responsible for managing callback and killswitch actions.
    /// </summary>
    public class Probe
    {
        /// <summary>
        /// Initializes a new instance of the Probe class with the specified callback and killswitch actions.
        /// </summary>
        public Probe(Action callback, Action killswitch)
        {
            Callback = callback;
            Killswitch = killswitch;
        }

        /// <summary>
        /// Gets or sets the callback action.
        /// </summary>
        public Action Callback { get; }

        /// <summary>
        /// Gets or sets the killswitch action.
        /// </summary>
        public Action Killswitch { get; }

        /// <summary>
        /// Gets or sets the current state.
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        public string CurrentPosition { get; set; }

        /// <summary>
        /// Gets or sets the current zone.
        /// </summary>
        public string CurrentZone { get; set; }

        /// <summary>
        /// Gets or sets the target name.
        /// </summary>
        public string TargetName { get; set; }

        /// <summary>
        /// Gets or sets the target class.
        /// </summary>
        public string TargetClass { get; set; }

        /// <summary>
        /// Gets or sets the target creature type.
        /// </summary>
        public string TargetCreatureType { get; set; }

        /// <summary>
        /// Gets or sets the target position.
        /// </summary>
        public string TargetPosition { get; set; }

        /// <summary>
        /// Gets or sets the target range.
        /// </summary>
        public string TargetRange { get; set; }

        /// <summary>
        /// Gets or sets the target faction ID.
        /// </summary>
        public string TargetFactionId { get; set; }

        /// <summary>
        /// Gets or sets the target that is currently casting.
        /// </summary>
        public string TargetIsCasting { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the target is channeling.
        /// </summary>
        public string TargetIsChanneling { get; set; }

        /// <summary>
        /// Gets or sets the update latency.
        /// </summary>
        public string UpdateLatency { get; set; }

        /// <summary>
        /// Gets or sets the list of blacklisted mob IDs.
        /// </summary>
        public IList<ulong> BlacklistedMobIds { get; set; }
    }
}
