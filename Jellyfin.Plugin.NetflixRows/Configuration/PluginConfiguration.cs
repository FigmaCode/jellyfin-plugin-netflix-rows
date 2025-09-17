using System.Collections.Generic;
using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NetflixRows.Configuration;

/// <summary>
/// Comprehensive configuration settings for the Netflix Rows plugin.
/// </summary>
/// <remarks>
/// <para><strong>Overview:</strong></para>
/// <para>
/// This configuration class manages all user-customizable settings for the Netflix Rows plugin,
/// enabling administrators to fine-tune the streaming experience for their Jellyfin instance.
/// </para>
/// 
/// <para><strong>Configuration Categories:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Row Management:</strong> Control which content sections are displayed</description></item>
/// <item><description><strong>Content Limits:</strong> Set maximum items per row and overall limits</description></item>
/// <item><description><strong>Genre Configuration:</strong> Customize genre-based content organization</description></item>
/// <item><description><strong>Performance Options:</strong> Optimize loading behavior and resource usage</description></item>
/// <item><description><strong>UI Customization:</strong> Adjust visual elements and user interface behavior</description></item>
/// </list>
/// 
/// <para><strong>Persistence:</strong></para>
/// <para>
/// Configuration is automatically persisted to disk by Jellyfin's configuration system.
/// Changes take effect immediately without requiring a server restart.
/// </para>
/// 
/// <para><strong>Default Values:</strong></para>
/// <para>
/// The configuration initializes with sensible defaults optimized for most Jellyfin installations,
/// balancing functionality with performance across different device types and library sizes.
/// </para>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// This configuration class uses thread-safe Collection&lt;T&gt; for list properties to ensure
/// safe concurrent access during configuration updates and content retrieval operations.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Basic Configuration Example:</strong></para>
/// <code>
/// var config = new PluginConfiguration
/// {
///     // Enable core sections
///     EnableMyList = true,
///     EnableRecentlyAdded = true,
///     EnableRandomPicks = true,
///     
///     // Set content limits
///     MaxItemsPerRow = 25,
///     MyListLimit = 50,
///     
///     // Configure genres
///     EnabledGenres = { "Action", "Comedy", "Drama", "Sci-Fi" },
///     
///     // Performance optimization
///     LazyLoadRows = true
/// };
/// </code>
/// 
/// <para><strong>Advanced Configuration Example:</strong></para>
/// <code>
/// var advancedConfig = new PluginConfiguration
/// {
///     // Customize display behavior
///     MaxRows = 10,
///     RandomRowOrder = true,
///     ReplaceHeartWithPlus = false,
///     
///     // Fine-tune content filtering
///     RecentlyAddedDays = 14,
///     LongNotWatchedMonths = 3,
///     MinGenreItems = 10,
///     
///     // Genre management
///     EnabledGenres = { "Action", "Adventure", "Animation", "Comedy", "Crime" },
///     BlacklistedGenres = { "Adult", "Erotic" }
/// };
/// </code>
/// </example>
/// <seealso cref="BasePluginConfiguration"/>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with optimal default settings.
    /// </summary>
    /// <remarks>
    /// <para><strong>Default Configuration Strategy:</strong></para>
    /// <para>
    /// The default configuration is designed to provide an immediate, high-quality Netflix-like experience
    /// for most Jellyfin installations without requiring extensive customization.
    /// </para>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Row Limits:</strong> Balanced to provide good content discovery without overwhelming slower devices</description></item>
    /// <item><description><strong>Lazy Loading:</strong> Enabled by default to improve initial page load times</description></item>
    /// <item><description><strong>Genre Selection:</strong> Limited to popular genres to avoid UI clutter</description></item>
    /// <item><description><strong>Content Limits:</strong> Set to reasonable defaults that work well with typical media libraries</description></item>
    /// </list>
    /// 
    /// <para><strong>User Experience Defaults:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Core Sections Enabled:</strong> My List, Recently Added, Random Picks for immediate value</description></item>
    /// <item><description><strong>Heart to Plus Icon:</strong> Enabled for more intuitive "add to list" behavior</description></item>
    /// <item><description><strong>Content Discovery:</strong> Balanced between showing new content and long-term library exploration</description></item>
    /// </list>
    /// 
    /// <para><strong>Customization Ready:</strong></para>
    /// <para>
    /// All defaults can be easily modified through the admin configuration interface,
    /// allowing administrators to tailor the experience to their specific needs and user preferences.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default configuration provides these settings:
    /// // - Max 8 rows displayed
    /// // - 10-25 items per row
    /// // - My List, Recently Added, Random Picks enabled
    /// // - Popular genres: Action, Anime, Comedy
    /// // - Recently Added = last 30 days
    /// // - Long Not Watched = 6+ months
    /// // - Lazy loading enabled for performance
    /// 
    /// var config = new PluginConfiguration();
    /// // Ready to use with sensible defaults
    /// </code>
    /// </example>
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
        
        // Auto-remove watched items configuration
        AutoRemoveWatchedFromMyList = false;
        WatchedThresholdPercentage = 95;
        RequireCompleteSeriesWatch = true;
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
    /// Gets or sets the maximum number of content items displayed in each horizontal row.
    /// </summary>
    /// <value>
    /// The maximum number of items per row. Valid range: 1-100. Default: 25.
    /// </value>
    /// <remarks>
    /// <para><strong>Performance Impact:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Lower Values (10-20):</strong> Better for mobile devices and slower connections</description></item>
    /// <item><description><strong>Medium Values (20-35):</strong> Optimal for most desktop and tablet users</description></item>
    /// <item><description><strong>Higher Values (35-50):</strong> Best for large screens and fast connections</description></item>
    /// <item><description><strong>Very High Values (50+):</strong> May impact performance, use cautiously</description></item>
    /// </list>
    /// 
    /// <para><strong>User Experience Considerations:</strong></para>
    /// <para>
    /// Higher values provide more content discovery options but may overwhelm users.
    /// Lower values create a cleaner interface but may limit content exploration.
    /// </para>
    /// 
    /// <para><strong>Responsive Behavior:</strong></para>
    /// <para>
    /// The actual number of visible items will automatically adjust based on screen size,
    /// but this setting controls the total number of items loaded and available for scrolling.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mobile-optimized setting
    /// config.MaxItemsPerRow = 15;
    /// 
    /// // Desktop-optimized setting
    /// config.MaxItemsPerRow = 30;
    /// 
    /// // Large screen/high-performance setting
    /// config.MaxItemsPerRow = 50;
    /// </code>
    /// </example>
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
    /// Gets the collection of genre names that will be displayed as individual content rows.
    /// </summary>
    /// <value>
    /// A thread-safe collection of genre strings. Each genre creates a dedicated horizontal row.
    /// </value>
    /// <remarks>
    /// <para><strong>Genre Row Creation:</strong></para>
    /// <para>
    /// Each genre in this collection generates a separate horizontal row containing content
    /// that matches the specified genre. Genres must exactly match those defined in your
    /// Jellyfin media library metadata.
    /// </para>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Too Many Genres:</strong> Can slow down page loading and overwhelm users</description></item>
    /// <item><description><strong>Popular Genres:</strong> Action, Comedy, Drama typically have the most content</description></item>
    /// <item><description><strong>Specialized Genres:</strong> Anime, Documentary, Horror may have limited content</description></item>
    /// </list>
    /// 
    /// <para><strong>Content Filtering:</strong></para>
    /// <para>
    /// Only genres with at least <see cref="MinGenreItems"/> items will be displayed.
    /// Empty genre rows are automatically hidden to maintain a clean interface.
    /// </para>
    /// 
    /// <para><strong>Common Genre Names:</strong></para>
    /// <list type="bullet">
    /// <item><description>Action, Adventure, Animation, Comedy, Crime</description></item>
    /// <item><description>Documentary, Drama, Family, Fantasy, Horror</description></item>
    /// <item><description>Music, Mystery, Romance, Sci-Fi, Thriller</description></item>
    /// <item><description>War, Western, Anime, Biography, History</description></item>
    /// </list>
    /// 
    /// <para><strong>Thread Safety:</strong></para>
    /// <para>
    /// This collection is thread-safe and can be safely modified during runtime.
    /// Changes take effect immediately for new page loads.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic genre configuration
    /// config.EnabledGenres.Clear();
    /// config.EnabledGenres.Add("Action");
    /// config.EnabledGenres.Add("Comedy");
    /// config.EnabledGenres.Add("Drama");
    /// 
    /// // Comprehensive genre setup
    /// var popularGenres = new[] {
    ///     "Action", "Adventure", "Animation", "Comedy", "Crime",
    ///     "Documentary", "Drama", "Family", "Fantasy", "Horror",
    ///     "Music", "Mystery", "Romance", "Sci-Fi", "Thriller"
    /// };
    /// foreach (var genre in popularGenres)
    /// {
    ///     config.EnabledGenres.Add(genre);
    /// }
    /// 
    /// // Specialized collection for anime fans
    /// config.EnabledGenres.Add("Anime");
    /// config.EnabledGenres.Add("Animation");
    /// config.EnabledGenres.Add("Adventure");
    /// </code>
    /// </example>
    /// <seealso cref="BlacklistedGenres"/>
    /// <seealso cref="MinGenreItems"/>
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
    /// Gets or sets a value indicating whether content rows should be loaded on-demand for optimal performance.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable lazy loading (recommended); <c>false</c> to load all content immediately. Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para><strong>Lazy Loading Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Faster Initial Load:</strong> Home page appears quickly without waiting for all content</description></item>
    /// <item><description><strong>Reduced Server Load:</strong> Only visible content is fetched initially</description></item>
    /// <item><description><strong>Better Mobile Experience:</strong> Significant improvement on slower mobile connections</description></item>
    /// <item><description><strong>Scalable Performance:</strong> Works well with large media libraries</description></item>
    /// </list>
    /// 
    /// <para><strong>When to Disable:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Small Libraries:</strong> If you have a limited number of items, immediate loading may be preferable</description></item>
    /// <item><description><strong>High-Speed Networks:</strong> On very fast local networks, the loading delay might be unnecessary</description></item>
    /// <item><description><strong>Debug/Testing:</strong> For troubleshooting content loading issues</description></item>
    /// </list>
    /// 
    /// <para><strong>Technical Implementation:</strong></para>
    /// <para>
    /// When enabled, rows are loaded as they come into the viewport during scrolling.
    /// Content that's not immediately visible is fetched in the background, providing
    /// a smooth browsing experience without sacrificing performance.
    /// </para>
    /// 
    /// <para><strong>User Experience:</strong></para>
    /// <para>
    /// With lazy loading enabled, users see the page load quickly and can immediately
    /// interact with the first few rows while additional content loads seamlessly.
    /// This creates a responsive, modern streaming platform experience.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Recommended for most installations
    /// config.LazyLoadRows = true;
    /// 
    /// // For small libraries or high-speed local networks
    /// config.LazyLoadRows = false;
    /// 
    /// // Dynamic configuration based on library size
    /// if (totalMediaItems > 1000)
    /// {
    ///     config.LazyLoadRows = true;  // Large library benefits from lazy loading
    /// }
    /// else
    /// {
    ///     config.LazyLoadRows = false; // Small library can load immediately
    /// }
    /// </code>
    /// </example>
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

    /// <summary>
    /// Gets or sets a value indicating whether watched items should be automatically removed from My List.
    /// </summary>
    /// <value>
    /// <c>true</c> to automatically hide watched content from My List; <c>false</c> to show all favorites regardless of watch status. Default: <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para><strong>Smart Filtering Behavior:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Movies:</strong> Hidden when watch percentage exceeds <see cref="WatchedThresholdPercentage"/></description></item>
    /// <item><description><strong>Series:</strong> Hidden when all episodes are watched (if <see cref="RequireCompleteSeriesWatch"/> is true)</description></item>
    /// <item><description><strong>User Control:</strong> Users can re-favorite items to add them back to My List</description></item>
    /// </list>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Active Discovery:</strong> Keep My List focused on unwatched content</description></item>
    /// <item><description><strong>Clean Interface:</strong> Automatically maintain a curated watchlist</description></item>
    /// <item><description><strong>Continued Watching:</strong> Show only content that needs completion</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Impact:</strong></para>
    /// <para>
    /// When enabled, the system loads additional items to ensure the requested number of visible
    /// items after filtering. This may slightly increase initial load time but improves user experience.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable auto-removal with default settings
    /// config.AutoRemoveWatchedFromMyList = true;
    /// config.WatchedThresholdPercentage = 95;  // 95% completion = watched
    /// config.RequireCompleteSeriesWatch = true; // All episodes must be watched
    /// 
    /// // Strict setting - remove at 100% completion only
    /// config.WatchedThresholdPercentage = 100;
    /// 
    /// // Lenient setting - remove at 85% completion
    /// config.WatchedThresholdPercentage = 85;
    /// </code>
    /// </example>
    public bool AutoRemoveWatchedFromMyList { get; set; }

    /// <summary>
    /// Gets or sets the percentage threshold at which movies are considered "watched" and removed from My List.
    /// </summary>
    /// <value>
    /// The watch percentage threshold (0-100). Default: 95.
    /// </value>
    /// <remarks>
    /// <para><strong>Threshold Selection Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>95% (Recommended):</strong> Accounts for users who don't watch credits</description></item>
    /// <item><description><strong>90%:</strong> More aggressive removal, good for users who often skip endings</description></item>
    /// <item><description><strong>100%:</strong> Only remove completely finished content</description></item>
    /// <item><description><strong>85%:</strong> Remove content that's substantially watched</description></item>
    /// </list>
    /// 
    /// <para><strong>Movie vs Series Behavior:</strong></para>
    /// <para>
    /// This threshold applies only to movies and individual episodes. For series, the completion
    /// is determined by whether all episodes have been watched, regardless of this percentage.
    /// </para>
    /// 
    /// <para><strong>User Experience Considerations:</strong></para>
    /// <para>
    /// A threshold below 100% accounts for real-world viewing behavior where users may not
    /// watch credits or may stop slightly before the end. This creates a more natural
    /// "watched" experience.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Conservative approach - only remove 100% completed
    /// config.WatchedThresholdPercentage = 100;
    /// 
    /// // Netflix-like behavior - remove at 95%
    /// config.WatchedThresholdPercentage = 95;
    /// 
    /// // Aggressive cleaning - remove at 85%
    /// config.WatchedThresholdPercentage = 85;
    /// 
    /// // Validation example
    /// if (config.WatchedThresholdPercentage &lt; 1 || config.WatchedThresholdPercentage &gt; 100)
    /// {
    ///     config.WatchedThresholdPercentage = 95; // Reset to default
    /// }
    /// </code>
    /// </example>
    public int WatchedThresholdPercentage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether series require all episodes to be watched before removal from My List.
    /// </summary>
    /// <value>
    /// <c>true</c> to require complete series watch for removal; <c>false</c> to remove based on current episode progress. Default: <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para><strong>Series Completion Logic:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Complete Series (true):</strong> All episodes must be watched to remove from My List</description></item>
    /// <item><description><strong>Current Progress (false):</strong> Remove based on current episode watch percentage</description></item>
    /// </list>
    /// 
    /// <para><strong>Recommended Setting:</strong></para>
    /// <para>
    /// <c>true</c> is recommended because users typically want to keep series in My List until
    /// they've finished the entire show, even if they've completed the current episode.
    /// This prevents premature removal of ongoing series.
    /// </para>
    /// 
    /// <para><strong>Use Case Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Binge Watchers:</strong> Keep series until completely finished</description></item>
    /// <item><description><strong>Episode-by-Episode:</strong> Remove when current episode is done</description></item>
    /// <item><description><strong>Seasonal Viewing:</strong> Maintain series across multiple viewing sessions</description></item>
    /// </list>
    /// 
    /// <para><strong>Technical Implementation:</strong></para>
    /// <para>
    /// When true, the system checks if all episodes in the series have UserData.Played = true.
    /// When false, it only checks the current episode's watch percentage against the threshold.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Recommended: Keep series until completely finished
    /// config.RequireCompleteSeriesWatch = true;
    /// 
    /// // Alternative: Remove based on current episode only
    /// config.RequireCompleteSeriesWatch = false;
    /// 
    /// // Combined with threshold for movies
    /// config.AutoRemoveWatchedFromMyList = true;
    /// config.WatchedThresholdPercentage = 95;
    /// config.RequireCompleteSeriesWatch = true;
    /// // Result: Movies removed at 95%, series only when completely finished
    /// </code>
    /// </example>
    public bool RequireCompleteSeriesWatch { get; set; }
}