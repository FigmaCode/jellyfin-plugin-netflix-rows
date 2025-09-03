using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.NetflixRows.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Entities;

namespace Jellyfin.Plugin.NetflixRows.Controllers;

/// <summary>
/// Netflix Rows API Controller.
/// </summary>
[ApiController]
[Route("NetflixRows")]
[Produces("application/json")]
public class NetflixRowsController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly Jellyfin.Data.IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<NetflixRowsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetflixRowsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="Jellyfin.Data.IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{NetflixRowsController}"/> interface.</param>
    public NetflixRowsController(
        ILibraryManager libraryManager,
        Jellyfin.Data.IUserManager userManager,
        IDtoService dtoService,
        ILogger<NetflixRowsController> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to verify controller is working.
    /// </summary>
    /// <returns>Test message.</returns>
    [HttpGet("Test")]
    [AllowAnonymous]
    public ActionResult<string> Test()
    {
        _logger.LogInformation("Netflix Rows Test endpoint called");
        return "Netflix Rows Controller is working!";
    }

    /// <summary>
    /// Gets "My List" items for the current user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Number of items to return.</param>
    /// <returns>Query result with favorite items.</returns>
    [HttpGet("MyList")]
    [Authorize]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetMyList(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Invalid user ID: {UserId}", userId);
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var actualLimit = Math.Min(limit, config.MyListLimit);

            var query = new InternalItemsQuery(user)
            {
                IsFavorite = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                Limit = actualLimit
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            _logger.LogDebug("Retrieved {Count} items for My List for user {UserId}", dtos.Length, userId);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting My List for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Gets recently added items.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Number of items to return.</param>
    /// <returns>Query result with recently added items.</returns>
    [HttpGet("RecentlyAdded")]
    [Authorize]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetRecentlyAdded(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var cutoffDate = DateTime.UtcNow.AddDays(-config.RecentlyAddedDays);

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                MinDateCreated = cutoffDate,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            _logger.LogDebug("Retrieved {Count} recently added items for user {UserId}", dtos.Length, userId);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently added items for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Gets random picks.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Number of items to return.</param>
    /// <returns>Query result with random items.</returns>
    [HttpGet("RandomPicks")]
    [Authorize]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetRandomPicks(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            _logger.LogDebug("Retrieved {Count} random picks for user {UserId}", dtos.Length, userId);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random picks for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Gets items not watched for a long time.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Number of items to return.</param>
    /// <returns>Query result with long not watched items.</returns>
    [HttpGet("LongNotWatched")]
    [Authorize]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetLongNotWatched(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var cutoffDate = DateTime.UtcNow.AddMonths(-config.LongNotWatchedMonths);

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                IsPlayed = false,
                Recursive = true
            };

            var items = _libraryManager.GetItemsResult(query);
            
            var filteredItems = items.Items
                .Where(item => item.DateCreated <= cutoffDate)
                .OrderBy(item => item.DateCreated)
                .Take(Math.Min(limit, config.MaxItemsPerRow))
                .ToArray();

            var dtoOptions = new DtoOptions(true);
            var dtos = filteredItems.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            _logger.LogDebug("Retrieved {Count} long not watched items for user {UserId}", dtos.Length, userId);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = filteredItems.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting long not watched items for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Gets items by genre.
    /// </summary>
    /// <param name="genre">Genre name.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Number of items to return.</param>
    /// <returns>Query result with items of the specified genre.</returns>
    [HttpGet("Genre/{genre}")]
    [Authorize]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetGenre(
        string genre,
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            
            if (config.BlacklistedGenres.Contains(genre, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Genre is blacklisted");
            }

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Genres = new[] { genre },
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var items = _libraryManager.GetItemsResult(query);
            
            if (items.TotalRecordCount < config.MinGenreItems)
            {
                return Ok(new QueryResult<BaseItemDto>
                {
                    Items = Array.Empty<BaseItemDto>(),
                    TotalRecordCount = 0
                });
            }

            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            _logger.LogDebug("Retrieved {Count} items for genre {Genre} for user {UserId}", dtos.Length, genre, userId);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting genre {Genre} items for user {UserId}", genre, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Gets available genres.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>List of available genres.</returns>
    [HttpGet("Genres")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<string>>> GetGenres([FromQuery] Guid userId)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true
            };

            var items = _libraryManager.GetItemsResult(query);
            var genres = items.Items
                .SelectMany(item => item.Genres ?? Array.Empty<string>())
                .Where(genre => !config.BlacklistedGenres.Contains(genre, StringComparer.OrdinalIgnoreCase))
                .GroupBy(genre => genre, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() >= config.MinGenreItems)
                .Select(group => group.Key)
                .OrderBy(genre => genre)
                .ToList();

            _logger.LogDebug("Retrieved {Count} genres for user {UserId}", genres.Count, userId);
            return Ok(genres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting genres for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Gets plugin configuration.
    /// </summary>
    /// <returns>Plugin configuration.</returns>
    [HttpGet("Config")]
    [AllowAnonymous]  // Allow anonymous access for testing
    public ActionResult<PluginConfiguration> GetConfig()
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            _logger.LogDebug("Configuration requested, returning config with {Count} enabled genres", 
                config.EnabledGenres?.Count ?? 0);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Updates plugin configuration.
    /// </summary>
    /// <param name="config">New configuration.</param>
    /// <returns>Action result.</returns>
    [HttpPost("Config")]
    [Authorize]
    public ActionResult UpdateConfig([FromBody] PluginConfiguration config)
    {
        try
        {
            if (Plugin.Instance != null && config != null)
            {
                _logger.LogInformation("Updating Netflix Rows configuration");
                Plugin.Instance.UpdateConfiguration(config);
                Plugin.Instance.SaveConfiguration();
                _logger.LogInformation("Configuration updated successfully");
                return Ok();
            }
            
            return BadRequest("Plugin instance not available or invalid config");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Serves the Netflix Rows JavaScript file.
    /// </summary>
    /// <returns>JavaScript content.</returns>
    [HttpGet("Script")]
    [AllowAnonymous]
    public ActionResult GetScript()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.NetflixRows.Web.netflixRows.js";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                _logger.LogDebug("Serving Netflix Rows script, size: {Size} bytes", content.Length);
                return Content(content, "application/javascript");
            }
            
            _logger.LogWarning("Netflix Rows script resource not found");
            return NotFound("Script not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving Netflix Rows script");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }
}