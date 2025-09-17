using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.NetflixRows.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Entities;

namespace Jellyfin.Plugin.NetflixRows.Controllers;

/// <summary>
/// Payload for Home Screen Section requests.
/// </summary>
public class HomeScreenSectionPayload
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets additional data for the section.
    /// </summary>
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Netflix Rows API Controller.
/// </summary>
[ApiController]
[Route("NetflixRows")] // Zurück zum ursprünglichen Routing
[Produces("application/json")]
public class NetflixRowsController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<NetflixRowsController> _logger;
    private readonly IUserManager _userManager;

    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, Exception?> LogControllerInitialized = 
        LoggerMessage.Define(LogLevel.Information, new EventId(101, nameof(LogControllerInitialized)), 
            "[NetflixRows] Controller initialized");
    
    private static readonly Action<ILogger, Exception?> LogTestEndpointCalled = 
        LoggerMessage.Define(LogLevel.Information, new EventId(102, nameof(LogTestEndpointCalled)), 
            "[NetflixRows] Test endpoint called");
    
    private static readonly Action<ILogger, Exception?> LogConfigRequested = 
        LoggerMessage.Define(LogLevel.Information, new EventId(103, nameof(LogConfigRequested)), 
            "[NetflixRows] Config requested");
    
    private static readonly Action<ILogger, string, Exception?> LogReturningConfig = 
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(104, nameof(LogReturningConfig)), 
            "[NetflixRows] Returning config: {Config}");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingConfiguration = 
        LoggerMessage.Define(LogLevel.Error, new EventId(105, nameof(LogErrorGettingConfiguration)), 
            "[NetflixRows] Error getting configuration");
    
    private static readonly Action<ILogger, Exception?> LogConfigUpdateRequested = 
        LoggerMessage.Define(LogLevel.Information, new EventId(106, nameof(LogConfigUpdateRequested)), 
            "[NetflixRows] Config update requested");
    
    private static readonly Action<ILogger, Exception?> LogConfigurationUpdatedSuccessfully = 
        LoggerMessage.Define(LogLevel.Information, new EventId(107, nameof(LogConfigurationUpdatedSuccessfully)), 
            "[NetflixRows] Configuration updated successfully");
    
    private static readonly Action<ILogger, Exception?> LogPluginInstanceOrConfigNull = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(108, nameof(LogPluginInstanceOrConfigNull)), 
            "[NetflixRows] Plugin instance or config is null");
    
    private static readonly Action<ILogger, Exception?> LogErrorUpdatingConfiguration = 
        LoggerMessage.Define(LogLevel.Error, new EventId(109, nameof(LogErrorUpdatingConfiguration)), 
            "[NetflixRows] Error updating configuration");
    
    private static readonly Action<ILogger, Exception?> LogMyListRequested = 
        LoggerMessage.Define(LogLevel.Information, new EventId(110, nameof(LogMyListRequested)), 
            "[NetflixRows] MyList requested");
    
    private static readonly Action<ILogger, int, Exception?> LogRetrievedMyListItems = 
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(111, nameof(LogRetrievedMyListItems)), 
            "[NetflixRows] Retrieved {Count} items for My List");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingMyList = 
        LoggerMessage.Define(LogLevel.Error, new EventId(112, nameof(LogErrorGettingMyList)), 
            "[NetflixRows] Error getting My List");
    
    private static readonly Action<ILogger, Exception?> LogRecentlyAddedRequested = 
        LoggerMessage.Define(LogLevel.Information, new EventId(113, nameof(LogRecentlyAddedRequested)), 
            "[NetflixRows] RecentlyAdded requested");
    
    private static readonly Action<ILogger, int, Exception?> LogRetrievedRecentlyAddedItems = 
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(114, nameof(LogRetrievedRecentlyAddedItems)), 
            "[NetflixRows] Retrieved {Count} recently added items");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingRecentlyAddedItems = 
        LoggerMessage.Define(LogLevel.Error, new EventId(115, nameof(LogErrorGettingRecentlyAddedItems)), 
            "[NetflixRows] Error getting recently added items");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingRandomPicks = 
        LoggerMessage.Define(LogLevel.Error, new EventId(116, nameof(LogErrorGettingRandomPicks)), 
            "[NetflixRows] Error getting random picks");
    
    private static readonly Action<ILogger, string, Exception?> LogErrorGettingGenre = 
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(117, nameof(LogErrorGettingGenre)), 
            "[NetflixRows] Error getting genre {Genre}");
    
    private static readonly Action<ILogger, Exception?> LogScriptRequested = 
        LoggerMessage.Define(LogLevel.Information, new EventId(118, nameof(LogScriptRequested)), 
            "[NetflixRows] Script requested");
    
    private static readonly Action<ILogger, string, Exception?> LogCouldNotFindEmbeddedResource = 
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(119, nameof(LogCouldNotFindEmbeddedResource)), 
            "Could not find embedded resource: {ResourceName}");
    
    private static readonly Action<ILogger, int, Exception?> LogScriptServedSuccessfully = 
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(120, nameof(LogScriptServedSuccessfully)), 
            "Netflix Rows script served successfully, {Length} characters");
    
    private static readonly Action<ILogger, Exception?> LogErrorServingScript = 
        LoggerMessage.Define(LogLevel.Error, new EventId(121, nameof(LogErrorServingScript)), 
            "[NetflixRows] Error serving script");
    
    private static readonly Action<ILogger, Guid?, string?, Exception?> LogMyListSectionCalled = 
        LoggerMessage.Define<Guid?, string?>(LogLevel.Error, new EventId(122, nameof(LogMyListSectionCalled)), 
            "[NetflixRows] *** CRITICAL DEBUG *** MyListSection POST endpoint WAS CALLED! UserId: {UserId}, AdditionalData: {AdditionalData}");
    
    private static readonly Action<ILogger, Exception?> LogMyListSectionDisabled = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(123, nameof(LogMyListSectionDisabled)), 
            "[NetflixRows] My List section is disabled in config");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingMyListSection = 
        LoggerMessage.Define(LogLevel.Error, new EventId(124, nameof(LogErrorGettingMyListSection)), 
            "[NetflixRows] Error getting My List section");
    
    private static readonly Action<ILogger, Guid?, string?, Exception?> LogRecentlyAddedSectionCalled = 
        LoggerMessage.Define<Guid?, string?>(LogLevel.Information, new EventId(125, nameof(LogRecentlyAddedSectionCalled)), 
            "[NetflixRows] RecentlyAddedSection POST endpoint called with UserId: {UserId}, AdditionalData: {AdditionalData}");
    
    private static readonly Action<ILogger, Exception?> LogRecentlyAddedSectionDisabled = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(126, nameof(LogRecentlyAddedSectionDisabled)), 
            "[NetflixRows] Recently Added section is disabled in config");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingRecentlyAddedSection = 
        LoggerMessage.Define(LogLevel.Error, new EventId(127, nameof(LogErrorGettingRecentlyAddedSection)), 
            "[NetflixRows] Error getting Recently Added section");
    
    private static readonly Action<ILogger, Guid?, string?, Exception?> LogRandomPicksSectionCalled = 
        LoggerMessage.Define<Guid?, string?>(LogLevel.Information, new EventId(128, nameof(LogRandomPicksSectionCalled)), 
            "[NetflixRows] RandomPicksSection POST endpoint called with UserId: {UserId}, AdditionalData: {AdditionalData}");
    
    private static readonly Action<ILogger, Exception?> LogRandomPicksSectionDisabled = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(129, nameof(LogRandomPicksSectionDisabled)), 
            "[NetflixRows] Random Picks section is disabled in config");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingRandomPicksSection = 
        LoggerMessage.Define(LogLevel.Error, new EventId(130, nameof(LogErrorGettingRandomPicksSection)), 
            "[NetflixRows] Error getting Random Picks section");
    
    private static readonly Action<ILogger, Guid?, string?, Exception?> LogGenreSectionCalled = 
        LoggerMessage.Define<Guid?, string?>(LogLevel.Information, new EventId(131, nameof(LogGenreSectionCalled)), 
            "[NetflixRows] GenreSection POST endpoint called with UserId: {UserId}, AdditionalData: {AdditionalData}");
    
    private static readonly Action<ILogger, string, Exception?> LogGenreSectionDisabled = 
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(132, nameof(LogGenreSectionDisabled)), 
            "[NetflixRows] Genre section '{Genre}' is disabled");
    
    private static readonly Action<ILogger, Exception?> LogErrorGettingGenreSection = 
        LoggerMessage.Define(LogLevel.Error, new EventId(133, nameof(LogErrorGettingGenreSection)), 
            "[NetflixRows] Error getting genre section");
    
    private static readonly Action<ILogger, int, int, Exception?> LogAutoRemovedWatchedItems = 
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(134, nameof(LogAutoRemovedWatchedItems)), 
            "[NetflixRows] Auto-removed {FilteredCount} watched items from My List (of {OriginalCount} total favorites)");

    /// <summary>
    /// Initializes a new instance of the <see cref="NetflixRowsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager for accessing media items.</param>
    /// <param name="dtoService">DTO service for converting media items.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager for user operations.</param>
    public NetflixRowsController(
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ILogger<NetflixRowsController> logger,
        IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _logger = logger;
        _userManager = userManager;
        
        LogControllerInitialized(_logger, null);
    }

    /// <summary>
    /// Test endpoint to verify the controller is working.
    /// </summary>
    /// <returns>Test message with current timestamp.</returns>
    [HttpGet("Test")]
    [AllowAnonymous]
    public ActionResult<string> Test()
    {
        LogTestEndpointCalled(_logger, null);
        return Ok("Netflix Rows Controller is working! " + DateTime.Now);
    }

    /// <summary>
    /// Gets the current plugin configuration.
    /// </summary>
    /// <returns>The plugin configuration.</returns>
    [HttpGet("Config")]
    public ActionResult<PluginConfiguration> GetConfig()
    {
        try
        {
            LogConfigRequested(_logger, null);
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            LogReturningConfig(_logger, System.Text.Json.JsonSerializer.Serialize(config), null);
            return Ok(config);
        }
        catch (System.Text.Json.JsonException ex)
        {
            LogErrorGettingConfiguration(_logger, ex);
            return StatusCode(500, "Configuration serialization error");
        }
        catch (InvalidOperationException ex)
        {
            LogErrorGettingConfiguration(_logger, ex);
            return StatusCode(500, "Plugin configuration error");
        }
        catch (Exception ex)
        {
            LogErrorGettingConfiguration(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Updates the plugin configuration.
    /// </summary>
    /// <param name="config">The new configuration to apply.</param>
    /// <returns>Result of the configuration update.</returns>
    [HttpPost("Config")]
    public ActionResult UpdateConfig([FromBody] PluginConfiguration config)
    {
        try
        {
            LogConfigUpdateRequested(_logger, null);
            if (Plugin.Instance != null && config != null)
            {
                Plugin.Instance.UpdateConfiguration(config);
                Plugin.Instance.SaveConfiguration();
                LogConfigurationUpdatedSuccessfully(_logger, null);
                return Ok();
            }
            
            LogPluginInstanceOrConfigNull(_logger, null);
            return BadRequest("Plugin instance not available or invalid config");
        }
        catch (ArgumentNullException ex)
        {
            LogErrorUpdatingConfiguration(_logger, ex);
            return BadRequest("Invalid configuration data");
        }
        catch (InvalidOperationException ex)
        {
            LogErrorUpdatingConfiguration(_logger, ex);
            return StatusCode(500, "Plugin configuration update failed");
        }
        catch (UnauthorizedAccessException ex)
        {
            LogErrorUpdatingConfiguration(_logger, ex);
            return StatusCode(403, "Access denied to configuration file");
        }
        catch (IOException ex)
        {
            LogErrorUpdatingConfiguration(_logger, ex);
            return StatusCode(500, "Configuration file access error");
        }
        catch (Exception ex)
        {
            LogErrorUpdatingConfiguration(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the user's favorite items (My List) with optional filtering of watched content.
    /// </summary>
    /// <param name="limit">Maximum number of items to return after filtering.</param>
    /// <param name="userId">Optional user ID, uses current user if not provided.</param>
    /// <returns>Query result containing the user's favorite items, optionally filtered to exclude watched content.</returns>
    /// <remarks>
    /// <para><strong>Auto-Remove Watched Feature:</strong></para>
    /// <para>
    /// When <see cref="PluginConfiguration.AutoRemoveWatchedFromMyList"/> is enabled, this method
    /// intelligently filters out watched content based on configurable thresholds:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Movies:</strong> Removed when watch percentage exceeds threshold</description></item>
    /// <item><description><strong>Series:</strong> Removed when all episodes are completed (configurable)</description></item>
    /// <item><description><strong>Performance:</strong> Loads extra items to ensure sufficient unwatched content</description></item>
    /// </list>
    /// 
    /// <para><strong>Filtering Logic:</strong></para>
    /// <para>
    /// The system uses Jellyfin's UserData.PlayedPercentage and UserData.Played properties
    /// to determine watch status. This integrates seamlessly with Jellyfin's existing
    /// progress tracking without requiring additional data storage.
    /// </para>
    /// </remarks>
    [HttpGet("MyList")]
    public ActionResult<QueryResult<BaseItemDto>> GetMyList(
        [FromQuery] int limit = 25, [FromQuery] Guid? userId = null)
    {
        try
        {
            LogMyListRequested(_logger, null);

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var actualLimit = Math.Min(limit, config.MyListLimit);
            
            // Get user context if provided
            var user = userId.HasValue ? _userManager.GetUserById(userId.Value) : null;

            // If auto-remove is enabled, load more items to account for filtering
            var queryLimit = config.AutoRemoveWatchedFromMyList ? actualLimit * 2 : actualLimit;

            var query = new InternalItemsQuery(user)
            {
                IsFavorite = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                Limit = Math.Min(queryLimit, config.MyListLimit)
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            // Apply watched filter if enabled
            if (config.AutoRemoveWatchedFromMyList)
            {
                var originalCount = dtos.Length;
                dtos = FilterWatchedItems(dtos, config).Take(actualLimit).ToArray();
                var filteredCount = originalCount - dtos.Length;
                
                if (filteredCount > 0)
                {
                    LogAutoRemovedWatchedItems(_logger, filteredCount, originalCount, null);
                }
            }

            LogRetrievedMyListItems(_logger, dtos.Length, null);

            return Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = dtos.Length
            });
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingMyList(_logger, ex);
            return BadRequest("Invalid user or query parameters");
        }
        catch (InvalidOperationException ex)
        {
            LogErrorGettingMyList(_logger, ex);
            return StatusCode(500, "Library operation failed");
        }
        catch (Exception ex)
        {
            LogErrorGettingMyList(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Filters out watched items from a collection based on plugin configuration.
    /// </summary>
    /// <param name="items">The collection of items to filter.</param>
    /// <param name="config">The plugin configuration containing filter settings.</param>
    /// <returns>An enumerable of items that are not considered watched.</returns>
    /// <remarks>
    /// <para><strong>Filtering Logic:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Movies:</strong> Filtered based on PlayedPercentage vs WatchedThresholdPercentage</description></item>
    /// <item><description><strong>Series:</strong> Filtered based on RequireCompleteSeriesWatch setting</description></item>
    /// <item><description><strong>Unknown Types:</strong> Not filtered, returned as-is</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// <para>
    /// This method is designed to be efficient for typical My List sizes. For very large
    /// favorite lists, consider implementing database-level filtering in future versions.
    /// </para>
    /// </remarks>
    private static IEnumerable<BaseItemDto> FilterWatchedItems(BaseItemDto[] items, PluginConfiguration config)
    {
        foreach (var item in items)
        {
            // Skip items without user data
            if (item.UserData == null)
            {
                yield return item;
                continue;
            }

            var isWatched = false;

            // Check watch status based on item type
            switch (item.Type)
            {
                case BaseItemKind.Movie:
                    // Movies: check percentage watched
                    var playedPercentage = item.UserData.PlayedPercentage ?? 0;
                    isWatched = playedPercentage >= config.WatchedThresholdPercentage;
                    break;

                case BaseItemKind.Series:
                    if (config.RequireCompleteSeriesWatch)
                    {
                        // Series: check if marked as played (all episodes watched)
                        isWatched = item.UserData.Played;
                    }
                    else
                    {
                        // Series: check current episode percentage like movies
                        var seriesPercentage = item.UserData.PlayedPercentage ?? 0;
                        isWatched = seriesPercentage >= config.WatchedThresholdPercentage;
                    }
                    break;

                default:
                    // For other types, don't filter - include in results
                    break;
            }

            // Include item if not watched
            if (!isWatched)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Gets recently added items.
    /// </summary>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>Query result containing recently added items.</returns>
    [HttpGet("RecentlyAdded")]
    public ActionResult<QueryResult<BaseItemDto>> GetRecentlyAdded(
        [FromQuery] int limit = 25)
    {
        try
        {
            LogRecentlyAddedRequested(_logger, null);

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var cutoffDate = DateTime.UtcNow.AddDays(-config.RecentlyAddedDays);

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                MinDateCreated = cutoffDate,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions)).ToArray();

            LogRetrievedRecentlyAddedItems(_logger, dtos.Length, null);

            return Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            });
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingRecentlyAddedItems(_logger, ex);
            return BadRequest("Invalid query parameters");
        }
        catch (InvalidOperationException ex)
        {
            LogErrorGettingRecentlyAddedItems(_logger, ex);
            return StatusCode(500, "Library operation failed");
        }
        catch (Exception ex)
        {
            LogErrorGettingRecentlyAddedItems(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets random picks from the media library.
    /// </summary>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>Query result containing random media items.</returns>
    [HttpGet("RandomPicks")]
    public ActionResult<QueryResult<BaseItemDto>> GetRandomPicks(
        [FromQuery] int limit = 25)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions)).ToArray();

            return Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            });
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingRandomPicks(_logger, ex);
            return BadRequest("Invalid query parameters");
        }
        catch (InvalidOperationException ex)
        {
            LogErrorGettingRandomPicks(_logger, ex);
            return StatusCode(500, "Library operation failed");
        }
        catch (Exception ex)
        {
            LogErrorGettingRandomPicks(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets items from a specific genre.
    /// </summary>
    /// <param name="genre">The genre name to filter by.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>Query result containing items from the specified genre.</returns>
    [HttpGet("Genre/{genre}")]
    public ActionResult<QueryResult<BaseItemDto>> GetGenre(
        [FromRoute] string genre,
        [FromQuery] int limit = 25)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Genres = new[] { genre },
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions)).ToArray();

            return Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            });
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingGenre(_logger, genre, ex);
            return BadRequest("Invalid genre or query parameters");
        }
        catch (InvalidOperationException ex)
        {
            LogErrorGettingGenre(_logger, genre, ex);
            return StatusCode(500, "Library operation failed");
        }
        catch (Exception ex)
        {
            LogErrorGettingGenre(_logger, genre, ex);
            throw;
        }
    }

    /// <summary>
    /// Serves the Netflix Rows JavaScript file.
    /// </summary>
    /// <returns>JavaScript content for Netflix Rows functionality.</returns>
    [HttpGet("Script")]
    [AllowAnonymous]
    public async Task<ActionResult> GetScript()
    {
        try
        {
            LogScriptRequested(_logger, null);
            
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.NetflixRows.Web.netflixRows.js";
            
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LogCouldNotFindEmbeddedResource(_logger, resourceName, null);
                return NotFound("Script resource not found");
            }

            await using (stream.ConfigureAwait(false))
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                
                LogScriptServedSuccessfully(_logger, content.Length, null);
                return Content(content, "application/javascript");
            }
        }
        catch (FileNotFoundException ex)
        {
            LogErrorServingScript(_logger, ex);
            return NotFound("Script resource not found");
        }
        catch (IOException ex)
        {
            LogErrorServingScript(_logger, ex);
            return StatusCode(500, "Error reading script resource");
        }
        catch (Exception ex)
        {
            LogErrorServingScript(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the My List section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for My List.</returns>
    [HttpPost("MyListSection")]
    public ActionResult<QueryResult<BaseItemDto>> GetMyListSection([FromBody] HomeScreenSectionPayload payload)
    {
        try
        {
            LogMyListSectionCalled(_logger, payload?.UserId, payload?.AdditionalData, null);
            
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableMyList)
            {
                LogMyListSectionDisabled(_logger, null);
                return Ok(new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>()));
            }

            return GetMyList(25, payload?.UserId);
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingMyListSection(_logger, ex);
            return BadRequest("Invalid section payload");
        }
        catch (Exception ex)
        {
            LogErrorGettingMyListSection(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the Recently Added section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for Recently Added.</returns>
    [HttpPost("RecentlyAddedSection")]
    public ActionResult<QueryResult<BaseItemDto>> GetRecentlyAddedSection([FromBody] HomeScreenSectionPayload payload)
    {
        try
        {
            LogRecentlyAddedSectionCalled(_logger, payload?.UserId, payload?.AdditionalData, null);
            
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableRecentlyAdded)
            {
                LogRecentlyAddedSectionDisabled(_logger, null);
                return Ok(new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>()));
            }

            return GetRecentlyAdded(25);
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingRecentlyAddedSection(_logger, ex);
            return BadRequest("Invalid section payload");
        }
        catch (Exception ex)
        {
            LogErrorGettingRecentlyAddedSection(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the Random Picks section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for Random Picks.</returns>
    [HttpPost("RandomPicksSection")]
    public ActionResult<QueryResult<BaseItemDto>> GetRandomPicksSection([FromBody] HomeScreenSectionPayload payload)
    {
        try
        {
            LogRandomPicksSectionCalled(_logger, payload?.UserId, payload?.AdditionalData, null);
                
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableRandomPicks)
            {
                LogRandomPicksSectionDisabled(_logger, null);
                return Ok(new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>()));
            }

            return GetRandomPicks(25);
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingRandomPicksSection(_logger, ex);
            return BadRequest("Invalid section payload");
        }
        catch (Exception ex)
        {
            LogErrorGettingRandomPicksSection(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets a genre section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for the specified genre from additionalData.</returns>
    [HttpPost("GenreSection")]
    public ActionResult<QueryResult<BaseItemDto>> GetGenreSection([FromBody] HomeScreenSectionPayload payload)
    {
        try
        {
            LogGenreSectionCalled(_logger, payload?.UserId, payload?.AdditionalData, null);
                
            var genre = payload?.AdditionalData ?? "Action";
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            
            if (config.EnabledGenres?.Contains(genre) != true)
            {
                LogGenreSectionDisabled(_logger, genre, null);
                return Ok(new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>()));
            }

            return GetGenre(genre, 25);
        }
        catch (ArgumentException ex)
        {
            LogErrorGettingGenreSection(_logger, ex);
            return BadRequest("Invalid section payload or genre");
        }
        catch (Exception ex)
        {
            LogErrorGettingGenreSection(_logger, ex);
            throw;
        }
    }


}