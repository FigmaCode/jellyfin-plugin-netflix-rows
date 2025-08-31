using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<NetflixRowsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetflixRowsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{NetflixRowsController}"/> interface.</param>
    public NetflixRowsController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDtoService dtoService,
        ILogger<NetflixRowsController> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
        _logger = logger;
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
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
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
            var user = _userManager.GetUserById(userId);
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
            var user = _userManager.GetUserById(userId);
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
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var cutoffDate = DateTime.UtcNow.AddMonths(-config.LongNotWatchedMonths);

            // Get all items first and then filter by date
            var allItemsQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                IsPlayed = false,
                Recursive = true
            };

            var allItems = _libraryManager.GetItemsResult(allItemsQuery);
            
            // Filter by date created (since MaxDateCreated doesn't exist)
            var filteredItems = allItems.Items
                .Where(item => item.DateCreated <= cutoffDate)
                .OrderBy(item => item.DateCreated)
                .Take(Math.Min(limit, config.MaxItemsPerRow))
                .ToArray();

            var dtoOptions = new DtoOptions(true);
            var dtos = filteredItems.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

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
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return BadRequest("Invalid user ID");
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            
            // Check if genre is blacklisted
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
            
            // Check if genre has minimum required items
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
            var user = _userManager.GetUserById(userId);
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
    [Authorize]
    public ActionResult<PluginConfiguration> GetConfig()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
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
                return Content(content, "application/javascript");
            }
            
            return NotFound("Script not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving Netflix Rows script");
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
            if (Plugin.Instance != null)
            {
                Plugin.Instance.UpdateConfiguration(config);
                Plugin.Instance.SaveConfiguration();
                return Ok();
            }
            
            return BadRequest("Plugin instance not available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }
}