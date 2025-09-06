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
            
            // Enhanced fallback script with more functionality
            var fallbackScript = @"
console.log('[NetflixRows] Script loaded successfully');

// Netflix Rows Plugin Main Script
(function() {
    'use strict';
    
    // Configuration
    var API_BASE = window.location.origin + '/NetflixRows';
    var currentUserId = getCurrentUserId();
    
    console.log('[NetflixRows] Initializing with user ID:', currentUserId);
    
    // Get current user ID from Jellyfin
    function getCurrentUserId() {
        if (window.ApiClient && window.ApiClient.getCurrentUserId) {
            return window.ApiClient.getCurrentUserId();
        }
        return null;
    }
    
    // Create test indicator
    function showTestIndicator() {
        var testDiv = document.createElement('div');
        testDiv.id = 'netflix-rows-indicator';
        testDiv.innerHTML = '🎬 Netflix Rows Plugin Active';
        testDiv.style.cssText = `
            position: fixed;
            top: 10px;
            right: 10px;
            background: #e50914;
            color: white;
            padding: 10px 15px;
            border-radius: 8px;
            font-weight: bold;
            z-index: 10000;
            box-shadow: 0 4px 8px rgba(0,0,0,0.3);
            font-family: Arial, sans-serif;
            font-size: 14px;
        `;
        
        document.body.appendChild(testDiv);
        
        // Auto-remove after 5 seconds
        setTimeout(function() {
            if (testDiv.parentNode) {
                testDiv.remove();
            }
        }, 5000);
    }
    
    // Test API connectivity
    function testApiConnectivity() {
        fetch(API_BASE + '/Test')
            .then(function(response) {
                if (response.ok) {
                    return response.text();
                }
                throw new Error('API test failed: ' + response.status);
            })
            .then(function(message) {
                console.log('[NetflixRows] API test successful:', message);
                showTestIndicator();
            })
            .catch(function(error) {
                console.error('[NetflixRows] API test failed:', error);
            });
    }
    
    // Initialize when DOM is ready
    function initialize() {
        console.log('[NetflixRows] DOM ready, initializing...');
        testApiConnectivity();
        
        // Check if we're on the home page
        if (window.location.hash.includes('home') || window.location.pathname === '/') {
            console.log('[NetflixRows] On home page, setting up rows...');
            setupNetflixRows();
        }
    }
    
    // Setup Netflix-style rows
    function setupNetflixRows() {
        if (!currentUserId) {
            console.warn('[NetflixRows] No user ID available');
            return;
        }
        
        // Find home view container
        var homeView = document.querySelector('.homeView, [data-type=\"home\"], .homePage');
        if (!homeView) {
            console.log('[NetflixRows] Home view not found, retrying in 2 seconds...');
            setTimeout(setupNetflixRows, 2000);
            return;
        }
        
        console.log('[NetflixRows] Home view found, creating rows...');
        createNetflixRowsContainer(homeView);
    }
    
    // Create Netflix rows container
    function createNetflixRowsContainer(homeView) {
        // Remove existing Netflix rows
        var existingRows = document.getElementById('netflix-rows-container');
        if (existingRows) {
            existingRows.remove();
        }
        
        // Create main container
        var container = document.createElement('div');
        container.id = 'netflix-rows-container';
        container.style.cssText = `
            margin: 20px 0;
            padding: 0 20px;
            background: transparent;
        `;
        
        // Insert at the beginning of home view
        homeView.insertBefore(container, homeView.firstChild);
        
        // Load different row types
        if (currentUserId) {
            loadMyListRow(container);
            loadRecentlyAddedRow(container);
            loadRandomPicksRow(container);
        }
    }
    
    // Load My List row
    function loadMyListRow(container) {
        console.log('[NetflixRows] Loading My List row...');
        fetch(API_BASE + '/MyList?userId=' + currentUserId + '&limit=20')
            .then(function(response) {
                if (response.ok) {
                    return response.json();
                }
                throw new Error('My List API failed');
            })
            .then(function(data) {
                if (data.Items && data.Items.length > 0) {
                    createRow(container, 'My List', data.Items);
                }
            })
            .catch(function(error) {
                console.error('[NetflixRows] My List failed:', error);
            });
    }
    
    // Load Recently Added row
    function loadRecentlyAddedRow(container) {
        console.log('[NetflixRows] Loading Recently Added row...');
        fetch(API_BASE + '/RecentlyAdded?userId=' + currentUserId + '&limit=20')
            .then(function(response) {
                if (response.ok) {
                    return response.json();
                }
                throw new Error('Recently Added API failed');
            })
            .then(function(data) {
                if (data.Items && data.Items.length > 0) {
                    createRow(container, 'Recently Added', data.Items);
                }
            })
            .catch(function(error) {
                console.error('[NetflixRows] Recently Added failed:', error);
            });
    }
    
    // Load Random Picks row
    function loadRandomPicksRow(container) {
        console.log('[NetflixRows] Loading Random Picks row...');
        fetch(API_BASE + '/RandomPicks?userId=' + currentUserId + '&limit=20')
            .then(function(response) {
                if (response.ok) {
                    return response.json();
                }
                throw new Error('Random Picks API failed');
            })
            .then(function(data) {
                if (data.Items && data.Items.length > 0) {
                    createRow(container, 'Random Picks', data.Items);
                }
            })
            .catch(function(error) {
                console.error('[NetflixRows] Random Picks failed:', error);
            });
    }
    
    // Create a Netflix-style row
    function createRow(container, title, items) {
        console.log('[NetflixRows] Creating row:', title, 'with', items.length, 'items');
        
        var rowDiv = document.createElement('div');
        rowDiv.className = 'netflix-row';
        rowDiv.style.cssText = `
            margin-bottom: 30px;
        `;
        
        // Row title
        var titleDiv = document.createElement('h2');
        titleDiv.textContent = title;
        titleDiv.style.cssText = `
            color: white;
            font-size: 1.5em;
            margin-bottom: 15px;
            font-weight: bold;
        `;
        
        // Row items container
        var itemsContainer = document.createElement('div');
        itemsContainer.style.cssText = `
            display: flex;
            overflow-x: auto;
            gap: 10px;
            padding-bottom: 10px;
        `;
        
        // Add items
        items.forEach(function(item) {
            var itemDiv = createRowItem(item);
            itemsContainer.appendChild(itemDiv);
        });
        
        rowDiv.appendChild(titleDiv);
        rowDiv.appendChild(itemsContainer);
        container.appendChild(rowDiv);
    }
    
    // Create individual row item
    function createRowItem(item) {
        var itemDiv = document.createElement('div');
        itemDiv.className = 'netflix-item';
        itemDiv.style.cssText = `
            min-width: 200px;
            height: 300px;
            background: #333;
            border-radius: 8px;
            cursor: pointer;
            transition: transform 0.3s ease;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        `;
        
        // Hover effect
        itemDiv.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.05)';
        });
        
        itemDiv.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
        });
        
        // Click handler
        itemDiv.addEventListener('click', function() {
            if (item.Id) {
                window.location.hash = '#/details?id=' + item.Id;
            }
        });
        
        // Item image
        if (item.ImageTags && item.ImageTags.Primary) {
            var img = document.createElement('img');
            img.src = '/Items/' + item.Id + '/Images/Primary?maxHeight=300&quality=80';
            img.style.cssText = `
                width: 100%;
                height: 200px;
                object-fit: cover;
            `;
            itemDiv.appendChild(img);
        }
        
        // Item title
        var titleDiv = document.createElement('div');
        titleDiv.textContent = item.Name || 'Unknown Title';
        titleDiv.style.cssText = `
            padding: 10px;
            color: white;
            font-size: 14px;
            font-weight: bold;
            text-align: center;
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
        `;
        
        itemDiv.appendChild(titleDiv);
        return itemDiv;
    }
    
    // Initialize when ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
    
    // Also initialize on hash change (for SPA navigation)
    window.addEventListener('hashchange', function() {
        if (window.location.hash.includes('home')) {
            setTimeout(setupNetflixRows, 500);
        }
    });
    
})();
";
            
            _logger.LogInformation("[NetflixRows] Serving enhanced fallback script");
            return Content(fallbackScript, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Error serving script");
            return StatusCode(500, "Internal server error");
        }
    }
}