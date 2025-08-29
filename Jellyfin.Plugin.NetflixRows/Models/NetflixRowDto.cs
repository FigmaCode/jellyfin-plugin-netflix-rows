csharp
using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.NetflixRows.Services;

/// <summary>
/// DTO representing a Netflix-style row.
/// </summary>
public class NetflixRowDto
{
    /// <summary>
    /// Gets or sets the row ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the row title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the row type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the genre (if applicable).
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>
    /// Gets or sets the total item count.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the preview items for the row.
    /// </summary>
    public List<BaseItemDto> PreviewItems { get; set; } = new();
}