// Services/IRowService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.NetflixRows.Services;

/// <summary>
/// Interface for row service.
/// </summary>
public interface IRowService
{
    /// <summary>
    /// Gets all Netflix rows for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of Netflix rows.</returns>
    Task<IEnumerable<NetflixRowDto>> GetRowsAsync(Guid userId);

    /// <summary>
    /// Gets items for a specific row.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="rowType">The row type.</param>
    /// <param name="genre">The genre (if applicable).</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="limit">The limit.</param>
    /// <returns>A query result with items.</returns>
    Task<QueryResult<BaseItemDto>> GetRowItemsAsync(Guid userId, string rowType, string? genre, int startIndex, int limit);
}