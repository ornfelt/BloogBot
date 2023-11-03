using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// This namespace contains classes for managing bot settings.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents the settings for the bot.
    /// </summary>
    /// <summary>
    /// Represents the settings for the bot.
    /// </summary>
    public class BotSettings
    {
        /// <summary>
        /// Gets or sets the type of the database.
        /// </summary>
        public string DatabaseType { get; set; }
        /// <summary>
        /// Gets or sets the path of the database.
        /// </summary>
        public string DatabasePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Discord bot is enabled.
        /// </summary>
        public bool DiscordBotEnabled { get; set; }

        /// <summary>
        /// Gets or sets the Discord bot token.
        /// </summary>
        public string DiscordBotToken { get; set; }

        /// <summary>
        /// Gets or sets the Discord guild ID.
        /// </summary>
        public string DiscordGuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord role ID.
        /// </summary>
        public string DiscordRoleId { get; set; }

        /// <summary>
        /// Gets or sets the Discord channel ID.
        /// </summary>
        public string DiscordChannelId { get; set; }

        /// <summary>
        /// Gets or sets the food.
        /// </summary>
        public string Food { get; set; }

        /// <summary>
        /// Gets or sets the drink.
        /// </summary>
        public string Drink { get; set; }

        /// <summary>
        /// Gets or sets the included names for targeting.
        /// </summary>
        public string TargetingIncludedNames { get; set; }

        /// <summary>
        /// Gets or sets the excluded names for targeting.
        /// </summary>
        public string TargetingExcludedNames { get; set; }

        /// <summary>
        /// Gets or sets the minimum level range.
        /// </summary>
        public int LevelRangeMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum level range.
        /// </summary>
        public int LevelRangeMax { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is a beast.
        /// </summary>
        public bool CreatureTypeBeast { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is Dragonkin.
        /// </summary>
        public bool CreatureTypeDragonkin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is a demon.
        /// </summary>
        public bool CreatureTypeDemon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature is of elemental type.
        /// </summary>
        public bool CreatureTypeElemental { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is humanoid.
        /// </summary>
        public bool CreatureTypeHumanoid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature type is undead.
        /// </summary>
        public bool CreatureTypeUndead { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the creature is of type Giant.
        /// </summary>
        public bool CreatureTypeGiant { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the unit reaction is hostile.
        /// </summary>
        public bool UnitReactionHostile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the unit reaction is unfriendly.
        /// </summary>
        public bool UnitReactionUnfriendly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the unit reaction is neutral.
        /// </summary>
        public bool UnitReactionNeutral { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the loot is poor.
        /// </summary>
        public bool LootPoor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the common loot is available.
        /// </summary>
        public bool LootCommon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the loot is uncommon.
        /// </summary>
        public bool LootUncommon { get; set; }

        /// <summary>
        /// Gets or sets the excluded names for loot.
        /// </summary>
        public string LootExcludedNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is sold as poor quality.
        /// </summary>
        public bool SellPoor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the common item is being sold.
        /// </summary>
        public bool SellCommon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is available for sale as an uncommon item.
        /// </summary>
        public bool SellUncommon { get; set; }

        /// <summary>
        /// Gets or sets the excluded names for selling.
        /// </summary>
        public string SellExcludedNames { get; set; }

        /// <summary>
        /// Gets or sets the GrindingHotspotId.
        /// </summary>
        public int? GrindingHotspotId { get; set; }

        /// <summary>
        /// Gets or sets the current travel path ID.
        /// </summary>
        public int? CurrentTravelPathId { get; set; }

        /// <summary>
        /// Gets or sets the current bot name.
        /// </summary>
        public string CurrentBotName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the teleport killswitch.
        /// </summary>
        public bool UseTeleportKillswitch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stuck-in-position killswitch is enabled.
        /// </summary>
        public bool UseStuckInPositionKillswitch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the stuck-in-state killswitch.
        /// </summary>
        public bool UseStuckInStateKillswitch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the player targeting killswitch.
        /// </summary>
        public bool UsePlayerTargetingKillswitch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the player proximity killswitch.
        /// </summary>
        public bool UsePlayerProximityKillswitch { get; set; }

        /// <summary>
        /// Gets or sets the power level of the player's name.
        /// </summary>
        public string PowerlevelPlayerName { get; set; }

        /// <summary>
        /// Gets or sets the targeting warning timer.
        /// </summary>
        public int TargetingWarningTimer { get; set; }

        /// <summary>
        /// Gets or sets the targeting stop timer.
        /// </summary>
        public int TargetingStopTimer { get; set; }

        /// <summary>
        /// Gets or sets the proximity warning timer.
        /// </summary>
        public int ProximityWarningTimer { get; set; }

        /// <summary>
        /// Gets or sets the proximity stop timer.
        /// </summary>
        public int ProximityStopTimer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether verbose logging should be used.
        /// </summary>
        public bool UseVerboseLogging { get; set; }

        /// <summary>
        /// Gets or sets the Hotspot GrindingHotspot.
        /// </summary>
        [JsonIgnore]
        public Hotspot GrindingHotspot { get; set; }

        /// <summary>
        /// Gets or sets the current travel path.
        /// </summary>
        [JsonIgnore]
        public TravelPath CurrentTravelPath { get; set; }

        /// <summary>
        /// Gets the list of creature types.
        /// </summary>
        [JsonIgnore]
        public IList<string> CreatureTypes
        {
            get
            {
                var creatureTypes = new List<string>();

                if (CreatureTypeBeast) creatureTypes.Add("Beast");
                if (CreatureTypeDragonkin) creatureTypes.Add("Dragonkin");
                if (CreatureTypeDemon) creatureTypes.Add("Demon");
                if (CreatureTypeElemental) creatureTypes.Add("Elemental");
                if (CreatureTypeHumanoid) creatureTypes.Add("Humanoid");
                if (CreatureTypeUndead) creatureTypes.Add("Undead");
                if (CreatureTypeGiant) creatureTypes.Add("Giant");

                return creatureTypes;
            }
        }

        /// <summary>
        /// Gets the list of unit reactions.
        /// </summary>
        [JsonIgnore]
        public IList<string> UnitReactions
        {
            get
            {
                var unitReactions = new List<string>();

                if (UnitReactionHostile) unitReactions.Add("Hostile");
                if (UnitReactionUnfriendly) unitReactions.Add("Unfriendly");
                if (UnitReactionNeutral) unitReactions.Add("Neutral");

                return unitReactions;
            }
        }
    }
}
