using System.Collections.Generic;
using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NetflixRows.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // Default configuration for MVP
        MaxRows = 8;
        MinItemsPerRow = 10;
        MaxItemsPerRow = 25;
        
        EnableMyList = true;
        EnableRecentlyAdded = true;
        EnableRandomPicks = true;
        EnableLongNotWatched = true;
        
        EnabledGenres = new Collection<string> { "Action", "Anime", "Comedy" };
        BlacklistedGenres = new Collection<string>();
        
        RecentlyAddedDays = 30;
        LongNotWatchedMonths = 6;
        MinGenreItems = 5;
        MyListLimit = 50;
        
        RandomRowOrder = false;
        LazyLoadRows = true;
        ReplaceHeartWithPlus = true;
    }

    /// <summary>
    /// Gets or sets the maximum number of rows to display.
    /// </summary>
    public int MaxRows { get; set; }

    /// <summary>
    /// Gets or sets the minimum items per row.
    /// </summary>
    public int MinItemsPerRow { get; set; }

    /// <summary>
    /// Gets or sets the maximum items per row.
    /// </summary>
    public int MaxItemsPerRow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether "My List" row is enabled.
    /// </summary>
    public bool EnableMyList { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether "Recently Added" row is enabled.
    /// </summary>
    public bool EnableRecentlyAdded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether "Random Picks" row is enabled.
    /// </summary>
    public bool EnableRandomPicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether "Long Not Watched" row is enabled.
    /// </summary>
    public bool EnableLongNotWatched { get; set; }

    /// <summary>
    /// Gets the list of enabled genres.
    /// </summary>
    public Collection<string> EnabledGenres { get; }

    /// <summary>
    /// Gets the list of blacklisted genres.
    /// </summary>
    public Collection<string> BlacklistedGenres { get; }

    /// <summary>
    /// Gets or sets the number of days for "Recently Added" definition.
    /// </summary>
    public int RecentlyAddedDays { get; set; }

    /// <summary>
    /// Gets or sets the number of months for "Long Not Watched" definition.
    /// </summary>
    public int LongNotWatchedMonths { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of items a genre must have to be displayed.
    /// </summary>
    public int MinGenreItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items in "My List".
    /// </summary>
    public int MyListLimit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether row order should be randomized.
    /// </summary>
    public bool RandomRowOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether rows should be lazy loaded.
    /// </summary>
    public bool LazyLoadRows { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether heart buttons should be replaced with plus icons.
    /// </summary>
    public bool ReplaceHeartWithPlus { get; set; }

    /// <summary>
    /// Gets or sets the number of items in My List row.
    /// </summary>
    public int MyListCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the number of items in Recently Added row.
    /// </summary>
    public int RecentlyAddedCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the number of items in Random Picks row.
    /// </summary>
    public int RandomPicksCount { get; set; } = 20;
}