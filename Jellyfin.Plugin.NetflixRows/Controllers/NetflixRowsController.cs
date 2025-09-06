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

namespace Jellyfin.Plugin.NetflixRows.Controllers;

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

    public NetflixRowsController(
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ILogger<NetflixRowsController> logger)
    {
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _logger = logger;
        
        _logger.LogInformation("[NetflixRows] Controller initialized");
    }

    [HttpGet("Test")]
    [AllowAnonymous]
    public ActionResult<string> Test()
    {
        _logger.LogInformation("[NetflixRows] Test endpoint called");
        return Ok("Netflix Rows Controller is working! " + DateTime.Now);
    }

    [HttpGet("Config")]
    public ActionResult<PluginConfiguration> GetConfig()
    {
        try
        {
            _logger.LogInformation("[NetflixRows] Config requested");
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            _logger.LogInformation("[NetflixRows] Returning config: {Config}", System.Text.Json.JsonSerializer.Serialize(config));
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting configuration");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpPost("Config")]
    public ActionResult UpdateConfig([FromBody] PluginConfiguration config)
    {
        try
        {
            _logger.LogInformation("[NetflixRows] Config update requested");
            if (Plugin.Instance != null && config != null)
            {
                Plugin.Instance.UpdateConfiguration(config);
                Plugin.Instance.SaveConfiguration();
                _logger.LogInformation("[NetflixRows] Configuration updated successfully");
                return Ok();
            }
            
            _logger.LogWarning("[NetflixRows] Plugin instance or config is null");
            return BadRequest("Plugin instance not available or invalid config");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error updating configuration");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpGet("MyList")]
    public ActionResult<QueryResult<BaseItemDto>> GetMyList(
        [FromQuery] int limit = 25)
    {
        try
        {
            _logger.LogInformation("[NetflixRows] MyList requested");

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var actualLimit = Math.Min(limit, config.MyListLimit);

            var query = new InternalItemsQuery
            {
                IsFavorite = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                Limit = actualLimit
            };

            var items = _libraryManager.GetItemsResult(query);
            var dtoOptions = new DtoOptions(true);
            var dtos = items.Items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions)).ToArray();

            _logger.LogInformation("[NetflixRows] Retrieved {Count} items for My List", dtos.Length);

            return Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting My List");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpGet("RecentlyAdded")]
    public ActionResult<QueryResult<BaseItemDto>> GetRecentlyAdded(
        [FromQuery] int limit = 25)
    {
        try
        {
            _logger.LogInformation("[NetflixRows] RecentlyAdded requested");

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

            _logger.LogInformation("[NetflixRows] Retrieved {Count} recently added items", dtos.Length);

            return Ok(new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = items.TotalRecordCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting recently added items");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting random picks");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpGet("Script")]
    [AllowAnonymous]
    public async Task<ActionResult> GetScript()
    {
        try
        {
            _logger.LogInformation("[NetflixRows] Script requested");
            
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.NetflixRows.Web.netflixRows.js";
            
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogError("Could not find embedded resource: {ResourceName}", resourceName);
                return NotFound("Script resource not found");
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            _logger.LogInformation("Netflix Rows script served successfully, {Length} characters", content.Length);
            return Content(content, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error serving script");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the My List section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for My List.</returns>
    [HttpGet("MyListSection")]
    public ActionResult<object> GetMyListSection()
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableMyList)
            {
                return NotFound("My List section is disabled");
            }

            var items = GetNetflixItems(config.MyListCount, item => 
                item.UserData?.IsFavorite == true);

            return Ok(new
            {
                displayName = "My List",
                items = items.Select(FormatItemForSection).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting My List section");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the Recently Added section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for Recently Added.</returns>
    [HttpGet("RecentlyAddedSection")]
    public ActionResult<object> GetRecentlyAddedSection()
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableRecentlyAdded)
            {
                return NotFound("Recently Added section is disabled");
            }

            var items = GetNetflixItems(config.RecentlyAddedCount, item => true)
                .OrderByDescending(x => x.DateCreated)
                .Take(config.RecentlyAddedCount);

            return Ok(new
            {
                displayName = "Recently Added",
                items = items.Select(FormatItemForSection).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Recently Added section");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the Random Picks section for Home Screen Sections.
    /// </summary>
    /// <returns>Section data for Random Picks.</returns>
    [HttpGet("RandomPicksSection")]
    public ActionResult<object> GetRandomPicksSection()
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableRandomPicks)
            {
                return NotFound("Random Picks section is disabled");
            }

            var items = GetNetflixItems(config.RandomPicksCount * 3, item => true)
                .OrderBy(x => Guid.NewGuid())
                .Take(config.RandomPicksCount);

            return Ok(new
            {
                displayName = "Random Picks",
                items = items.Select(FormatItemForSection).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Random Picks section");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a genre section for Home Screen Sections.
    /// </summary>
    /// <param name="genre">The genre name.</param>
    /// <returns>Section data for the specified genre.</returns>
    [HttpGet("GenreSection/{genre}")]
    public ActionResult<object> GetGenreSection([FromRoute] string genre)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (config.EnabledGenres?.Contains(genre) != true)
            {
                return NotFound($"Genre section '{genre}' is disabled");
            }

            var items = GetNetflixItems(config.GenreRowCounts?.GetValueOrDefault(genre, 20) ?? 20, 
                item => item.Genres.Any(g => g.Equals(genre, StringComparison.OrdinalIgnoreCase)));

            var displayName = config.GenreDisplayNames?.GetValueOrDefault(genre, genre) ?? genre;

            return Ok(new
            {
                displayName = displayName,
                items = items.Select(FormatItemForSection).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting genre section for '{genre}'");
            return StatusCode(500, "Internal server error");
        }
    }

    private IEnumerable<BaseItem> GetNetflixItems(int count, Func<BaseItem, bool> filter)
    {
        try
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Limit = count * 2 // Get more to filter from
            };

            var result = _libraryManager.GetItemsResult(query);
            return result.Items.Where(filter).Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Netflix items");
            return Enumerable.Empty<BaseItem>();
        }
    }

    private object FormatItemForSection(BaseItem item)
    {
        return new
        {
            id = item.Id.ToString(),
            name = item.Name,
            overview = item.Overview,
            type = item.GetType().Name,
            backdropImageUrl = $"/Items/{item.Id}/Images/Backdrop",
            primaryImageUrl = $"/Items/{item.Id}/Images/Primary",
            year = item.ProductionYear,
            rating = item.CommunityRating,
            runtime = item.RunTimeTicks.HasValue ? TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalMinutes : null,
            genres = item.Genres.ToList()
        };
    }
}