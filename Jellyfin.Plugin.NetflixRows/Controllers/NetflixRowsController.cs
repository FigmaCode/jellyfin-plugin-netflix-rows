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
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<NetflixRowsController> _logger;

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
    [AllowAnonymous]
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
    [AllowAnonymous] // Temporär für Testing
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
    [AllowAnonymous] // Temporär für Testing
    public ActionResult<QueryResult<BaseItemDto>> GetMyList(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            _logger.LogInformation("[NetflixRows] MyList requested for user: {UserId}", userId);
            
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                _logger.LogWarning("[NetflixRows] Invalid user ID: {UserId}", userId);
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
    [AllowAnonymous] // Temporär für Testing
    public ActionResult<QueryResult<BaseItemDto>> GetRecentlyAdded(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 25)
    {
        try
        {
            _logger.LogInformation("[NetflixRows] RecentlyAdded requested for user: {UserId}", userId);
            
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
    [AllowAnonymous]
    public ActionResult<QueryResult<BaseItemDto>> GetRandomPicks(
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
    public ActionResult GetScript()
    {
        try
        {
            _logger.LogInformation("[NetflixRows] Script requested");
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.NetflixRows.Web.netflixRows.js";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                _logger.LogInformation("[NetflixRows] Serving script, size: {Size} bytes", content.Length);
                return Content(content, "application/javascript");
            }
            
            // Fallback: return basic test script
            var fallbackScript = @"
console.log('[NetflixRows] Fallback script loaded');
setTimeout(function() {
    var testDiv = document.createElement('div');
    testDiv.innerHTML = 'Netflix Rows Plugin Test - Fallback Script Active';
    testDiv.style.cssText = 'background: red; color: white; padding: 10px; position: fixed; top: 10px; right: 10px; z-index: 9999;';
    document.body.appendChild(testDiv);
    setTimeout(function() { testDiv.remove(); }, 5000);
}, 1000);
";
            
            _logger.LogWarning("[NetflixRows] Script resource not found, using fallback");
            return Content(fallbackScript, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error serving script");
            return StatusCode(500, "Internal server error");
        }
    }
}