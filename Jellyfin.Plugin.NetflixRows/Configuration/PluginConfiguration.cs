using System.Collections.Generic;
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
        MaxRows = 8;
        MinItemsPerRow = 10;
        MaxItemsPerRow = 25;
        RecentlyAddedDays = 30;
        LongNotWatchedMonths = 6;
        EnabledGenres = new List<string> { "Action", "Comedy", "Drama", "Animation", "Anime" };
        BlacklistedGenres = new List<string>();
        EnabledRowTypes = new Dictionary<string, bool>
        {
            { "MyList", true },
            { "RecentlyAdded", true },
            { "RandomPicks", true },
            { "LongNotWatched", false },
            { "Genres", true }
        };
        MyListLimit = 50;
        ReplaceHeartWithPlus = true;
    }

    /// <summary>
    /// Gets or sets the maximum number of rows to display.
    /// </summary>
    public int MaxRows { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of items per row.
    /// </summary>
    public int MinItemsPerRow { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items per row.
    /// </summary>
    public int MaxItemsPerRow { get; set; }

    /// <summary>
    /// Gets or sets the number of days for recently added items.
    /// </summary>
    public int RecentlyAddedDays { get; set; }

    /// <summary>
    /// Gets or sets the number of months for long not watched items.
    /// </summary>
    public int LongNotWatchedMonths { get; set; }

    /// <summary>
    /// Gets or sets the enabled genres.
    /// </summary>
    public List<string> EnabledGenres { get; set; }

    /// <summary>
    /// Gets or sets the blacklisted genres.
    /// </summary>
    public List<string> BlacklistedGenres { get; set; }

    /// <summary>
    /// Gets or sets the enabled row types.
    /// </summary>
    public Dictionary<string, bool> EnabledRowTypes { get; set; }

    /// <summary>
    /// Gets or sets the limit for my list items.
    /// </summary>
    public int MyListLimit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to replace heart with plus icon.
    /// </summary>
    public bool ReplaceHeartWithPlus { get; set; }

    /// <summary>
    /// Gets or sets the minimum items required to show a genre row.
    /// </summary>
    public int MinGenreItems { get; set; } = 5;
}