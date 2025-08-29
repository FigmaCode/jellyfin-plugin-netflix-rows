using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.NetflixRows.Configuration;
using Jellyfin.Plugin.NetflixRows.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetflixRows.Api;

/// <summary>
/// Netflix Rows API controller.
/// </summary>
[ApiController]
[Route("NetflixRows")]
[Authorize]
public class NetflixRowsController : ControllerBase
{
    private readonly ILogger<NetflixRowsController> _logger;
    private readonly IRowService _rowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetflixRowsController"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{NetflixRowsController}"/> interface.</param>
    /// <param name="rowService">Instance of the <see cref="IRowService"/> interface.</param>
    public NetflixRowsController(ILogger<NetflixRowsController> logger, IRowService rowService)
    {
        _logger = logger;
        _rowService = rowService;
    }

    /// <summary>
    /// Gets all Netflix rows for the home screen.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of Netflix rows.</returns>
    [HttpGet("Rows")]
    public async Task<ActionResult<IEnumerable<NetflixRowDto>>> GetRows([FromQuery] [Required] Guid userId)
    {
        try
        {
            var rows = await _rowService.GetRowsAsync(userId);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Netflix rows for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets items for a specific row.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="rowType">The row type.</param>
    /// <param name="genre">The genre (if applicable).</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="limit">The limit.</param>
    /// <returns>A list of items for the row.</returns>
    [HttpGet("Row/{rowType}/Items")]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetRowItems(
        [FromQuery] [Required] Guid userId,
        [FromRoute] [Required] string rowType,
        [FromQuery] string? genre = null,
        [FromQuery] int startIndex = 0,
        [FromQuery] int limit = 20)
    {
        try
        {
            var items = await _rowService.GetRowItemsAsync(userId, rowType, genre, startIndex, limit);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting row items for user {UserId}, rowType {RowType}", userId, rowType);
            return StatusCode(500, "Internal server error");
        }
    }
}