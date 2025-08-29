// Services/RowService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.NetflixRows.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetflixRows.Services;

/// <summary>
/// Service for managing Netflix-style rows.
/// </summary>
public class RowService : IRowService
{
    private readonly ILogger<RowService> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RowService"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{RowService}"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    public RowService(
        ILogger<RowService> logger,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDtoService dtoService)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NetflixRowDto>> GetRowsAsync(Guid userId)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var user = _userManager.GetUserById(userId);
        var rows = new List<NetflixRowDto>();

        if (user == null)
        {
            return rows;
        }

        // My List (immer zuerst wenn aktiviert)
        if (config.EnabledRowTypes.GetValueOrDefault("MyList", false))
        {
            var myListRow = await CreateMyListRowAsync(user);
            if (myListRow != null)
            {
                rows.Add(myListRow);
            }
        }

        // Recently Added
        if (config.EnabledRowTypes.GetValueOrDefault("RecentlyAdded", false))
        {
            var recentRow = await CreateRecentlyAddedRowAsync(user);
            if (recentRow != null)
            {
                rows.Add(recentRow);
            }
        }

        // Random Picks
        if (config.EnabledRowTypes.GetValueOrDefault("RandomPicks", false))
        {
            var randomRow = await CreateRandomPicksRowAsync(user);
            if (randomRow != null)
            {
                rows.Add(randomRow);
            }
        }

        // Genre Rows
        if (config.EnabledRowTypes.GetValueOrDefault("Genres", false))
        {
            var genreRows = await CreateGenreRowsAsync(user, config);
            rows.AddRange(genreRows);
        }

        // Long Not Watched
        if (config.EnabledRowTypes.GetValueOrDefault("LongNotWatched", false))
        {
            var longNotWatchedRow = await CreateLongNotWatchedRowAsync(user);
            if (longNotWatchedRow != null)
            {
                rows.Add(longNotWatchedRow);
            }
        }

        return rows.Take(config.MaxRows);
    }

    /// <inheritdoc />
    public async Task<QueryResult<BaseItemDto>> GetRowItemsAsync(Guid userId, string rowType, string? genre, int startIndex, int limit)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return new QueryResult<BaseItemDto>();
        }

        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        return rowType.ToLowerInvariant() switch
        {
            "mylist" => await GetMyListItemsAsync(user, startIndex, limit),
            "recentlyadded" => await GetRecentlyAddedItemsAsync(user, startIndex, limit),
            "randompicks" => await GetRandomPicksItemsAsync(user, startIndex, limit),
            "genre" when !string.IsNullOrEmpty(genre) => await GetGenreItemsAsync(user, genre, startIndex, limit),
            "longnotwatched" => await GetLongNotWatchedItemsAsync(user, startIndex, limit),
            _ => new QueryResult<BaseItemDto>()
        };
    }

    private async Task<NetflixRowDto?> CreateMyListRowAsync(User user)
    {
        var query = new InternalItemsQuery(user)
        {
            IsFavorite = true,
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
            Limit = 6 // Preview items
        };

        var result = _libraryManager.GetItemsResult(query);
        if (result.TotalRecordCount == 0)
        {
            return null;
        }

        return new NetflixRowDto
        {
            Id = "mylist",
            Title = "Meine Liste",
            Type = "MyList",
            ItemCount = result.TotalRecordCount,
            PreviewItems = await ConvertToBaseItemDtos(result.Items, user)
        };
    }

    private async Task<NetflixRowDto?> CreateRecentlyAddedRowAsync(User user)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var cutoffDate = DateTime.UtcNow.AddDays(-config.RecentlyAddedDays);

        var query = new InternalItemsQuery(user)
        {
            MinDateCreated = cutoffDate,
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
            Limit = 6
        };

        var result = _libraryManager.GetItemsResult(query);
        if (result.TotalRecordCount == 0)
        {
            return null;
        }

        return new NetflixRowDto
        {
            Id = "recentlyadded",
            Title = "Kürzlich hinzugefügt",
            Type = "RecentlyAdded",
            ItemCount = result.TotalRecordCount,
            PreviewItems = await ConvertToBaseItemDtos(result.Items, user)
        };
    }

    private async Task<NetflixRowDto?> CreateRandomPicksRowAsync(User user)
    {
        var query = new InternalItemsQuery(user)
        {
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
            Limit = 6
        };

        var result = _libraryManager.GetItemsResult(query);
        if (result.TotalRecordCount == 0)
        {
            return null;
        }

        return new NetflixRowDto
        {
            Id = "randompicks",
            Title = "Zufällige Auswahl",
            Type = "RandomPicks",
            ItemCount = result.TotalRecordCount,
            PreviewItems = await ConvertToBaseItemDtos(result.Items, user)
        };
    }

    private async Task<List<NetflixRowDto>> CreateGenreRowsAsync(User user, PluginConfiguration config)
    {
        var rows = new List<NetflixRowDto>();

        foreach (var genre in config.EnabledGenres)
        {
            if (config.BlacklistedGenres.Contains(genre))
                continue;

            var query = new InternalItemsQuery(user)
            {
                Genres = new[] { genre },
                Recursive = true,
                IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = 6
            };

            var result = _libraryManager.GetItemsResult(query);

            if (result.TotalRecordCount >= config.MinGenreItems)
            {
                rows.Add(new NetflixRowDto
                {
                    Id = $"genre-{genre.ToLowerInvariant()}",
                    Title = genre,
                    Type = "Genre",
                    Genre = genre,
                    ItemCount = result.TotalRecordCount,
                    PreviewItems = await ConvertToBaseItemDtos(result.Items, user)
                });
            }
        }

        return rows;
    }

    private async Task<NetflixRowDto?> CreateLongNotWatchedRowAsync(User user)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var cutoffDate = DateTime.UtcNow.AddMonths(-config.LongNotWatchedMonths);

        var query = new InternalItemsQuery(user)
        {
            MaxDateCreated = cutoffDate,
            IsPlayed = false,
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Ascending) },
            Limit = 6
        };

        var result = _libraryManager.GetItemsResult(query);
        if (result.TotalRecordCount == 0)
        {
            return null;
        }

        return new NetflixRowDto
        {
            Id = "longnotwatched",
            Title = "Lange nicht gesehen",
            Type = "LongNotWatched",
            ItemCount = result.TotalRecordCount,
            PreviewItems = await ConvertToBaseItemDtos(result.Items, user)
        };
    }

    private async Task<QueryResult<BaseItemDto>> GetMyListItemsAsync(User user, int startIndex, int limit)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        
        var query = new InternalItemsQuery(user)
        {
            IsFavorite = true,
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
            StartIndex = startIndex,
            Limit = Math.Min(limit, config.MyListLimit)
        };

        var result = _libraryManager.GetItemsResult(query);
        var dtos = await ConvertToBaseItemDtos(result.Items, user);

        return new QueryResult<BaseItemDto>
        {
            Items = dtos.ToArray(),
            TotalRecordCount = result.TotalRecordCount,
            StartIndex = startIndex
        };
    }

    private async Task<QueryResult<BaseItemDto>> GetRecentlyAddedItemsAsync(User user, int startIndex, int limit)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var cutoffDate = DateTime.UtcNow.AddDays(-config.RecentlyAddedDays);

        var query = new InternalItemsQuery(user)
        {
            MinDateCreated = cutoffDate,
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
            StartIndex = startIndex,
            Limit = Math.Min(limit, config.MaxItemsPerRow)
        };

        var result = _libraryManager.GetItemsResult(query);
        var dtos = await ConvertToBaseItemDtos(result.Items, user);

        return new QueryResult<BaseItemDto>
        {
            Items = dtos.ToArray(),
            TotalRecordCount = result.TotalRecordCount,
            StartIndex = startIndex
        };
    }

    private async Task<QueryResult<BaseItemDto>> GetRandomPicksItemsAsync(User user, int startIndex, int limit)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        var query = new InternalItemsQuery(user)
        {
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
            StartIndex = startIndex,
            Limit = Math.Min(limit, config.MaxItemsPerRow)
        };

        var result = _libraryManager.GetItemsResult(query);
        var dtos = await ConvertToBaseItemDtos(result.Items, user);

        return new QueryResult<BaseItemDto>
        {
            Items = dtos.ToArray(),
            TotalRecordCount = result.TotalRecordCount,
            StartIndex = startIndex
        };
    }

    private async Task<QueryResult<BaseItemDto>> GetGenreItemsAsync(User user, string genre, int startIndex, int limit)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        var query = new InternalItemsQuery(user)
        {
            Genres = new[] { genre },
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
            StartIndex = startIndex,
            Limit = Math.Min(limit, config.MaxItemsPerRow)
        };

        var result = _libraryManager.GetItemsResult(query);
        var dtos = await ConvertToBaseItemDtos(result.Items, user);

        return new QueryResult<BaseItemDto>
        {
            Items = dtos.ToArray(),
            TotalRecordCount = result.TotalRecordCount,
            StartIndex = startIndex
        };
    }

    private async Task<QueryResult<BaseItemDto>> GetLongNotWatchedItemsAsync(User user, int startIndex, int limit)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var cutoffDate = DateTime.UtcNow.AddMonths(-config.LongNotWatchedMonths);

        var query = new InternalItemsQuery(user)
        {
            MaxDateCreated = cutoffDate,
            IsPlayed = false,
            Recursive = true,
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Ascending) },
            StartIndex = startIndex,
            Limit = Math.Min(limit, config.MaxItemsPerRow)
        };

        var result = _libraryManager.GetItemsResult(query);
        var dtos = await ConvertToBaseItemDtos(result.Items, user);

        return new QueryResult<BaseItemDto>
        {
            Items = dtos.ToArray(),
            TotalRecordCount = result.TotalRecordCount,
            StartIndex = startIndex
        };
    }

    private async Task<List<BaseItemDto>> ConvertToBaseItemDtos(IReadOnlyList<BaseItem> items, User user)
    {
        var dtoOptions = new DtoOptions()
        {
            EnableImages = true,
            Fields = new[]
            {
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.BasicSyncInfo,
                ItemFields.MediaSourceCount
            }
        };

        var dtos = new List<BaseItemDto>();
        foreach (var item in items)
        {
            var dto = _dtoService.GetBaseItemDto(item, dtoOptions, user);
            dtos.Add(dto);
        }

        return dtos;
    }
}