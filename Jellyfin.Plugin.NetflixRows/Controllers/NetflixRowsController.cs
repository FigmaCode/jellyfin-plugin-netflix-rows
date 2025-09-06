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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting genre {Genre}", genre);
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
    [HttpPost("MyListSection")]
    public ActionResult<QueryResult<BaseItemDto>> GetMyListSection([FromBody] HomeScreenSectionPayload payload)
    {
        try
        {
            _logger.LogError("[NetflixRows] *** CRITICAL DEBUG *** MyListSection POST endpoint WAS CALLED! UserId: {UserId}, AdditionalData: {AdditionalData}", 
                payload?.UserId, payload?.AdditionalData);
            
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableMyList)
            {
                _logger.LogWarning("[NetflixRows] My List section is disabled in config");
                return Ok(new QueryResult<BaseItemDto>(new BaseItemDto[0]));
            }

            return GetMyList(25);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting My List section");
            return StatusCode(500, "Internal server error");
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
            _logger.LogInformation("[NetflixRows] RecentlyAddedSection POST endpoint called with UserId: {UserId}, AdditionalData: {AdditionalData}", 
                payload?.UserId, payload?.AdditionalData);
            
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableRecentlyAdded)
            {
                _logger.LogWarning("[NetflixRows] Recently Added section is disabled in config");
                return Ok(new QueryResult<BaseItemDto>(new BaseItemDto[0]));
            }

            return GetRecentlyAdded(25);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting Recently Added section");
            return StatusCode(500, "Internal server error");
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
            _logger.LogInformation("[NetflixRows] RandomPicksSection POST endpoint called with UserId: {UserId}, AdditionalData: {AdditionalData}", 
                payload?.UserId, payload?.AdditionalData);
                
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableRandomPicks)
            {
                _logger.LogWarning("[NetflixRows] Random Picks section is disabled in config");
                return Ok(new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>()));
            }

            return GetRandomPicks(25);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting Random Picks section");
            return StatusCode(500, "Internal server error");
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
            _logger.LogInformation("[NetflixRows] GenreSection POST endpoint called with UserId: {UserId}, AdditionalData: {AdditionalData}", 
                payload?.UserId, payload?.AdditionalData);
                
            var genre = payload?.AdditionalData ?? "Action";
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            
            if (config.EnabledGenres?.Contains(genre) != true)
            {
                _logger.LogWarning("[NetflixRows] Genre section '{Genre}' is disabled", genre);
                return Ok(new QueryResult<BaseItemDto>(new BaseItemDto[0]));
            }

            return GetGenre(genre, 25);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error getting genre section");
            return StatusCode(500, "Internal server error");
        }
    }


}