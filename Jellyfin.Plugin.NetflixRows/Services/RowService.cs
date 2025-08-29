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
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;

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
    public Task<IEnumerable<NetflixRowDto>> GetRowsAsync(Guid userId)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var user = _userManager.GetUserById(userId);
        var rows = new List<NetflixRowDto>();

        if (user == null)
        {
            return Task.FromResult<IEnumerable<NetflixRowDto>>(rows);
        }

        try
        {
            // My List (immer zuerst wenn aktiviert)
            if (config.EnabledRowTypes.GetValueOrDefault("MyList", false))
            {
                var myListRow = CreateMyListRow(user);
                if (myListRow != null)
                {
                    rows.Add(myListRow);
                }
            }

            // Recently Added
            if (config.EnabledRowTypes.GetValueOrDefault("RecentlyAdded", false))
            {
                var recentRow = CreateRecentlyAddedRow(user);
                if (recentRow != null)
                {
                    rows.Add(recentRow);
                }
            }

            // Random Picks
            if (config.EnabledRowTypes.GetValueOrDefault("RandomPicks", false))
            {
                var randomRow = CreateRandomPicksRow(user);
                if (randomRow != null)
                {
                    rows.Add(randomRow);
                }
            }

            // Genre Rows
            if (config.EnabledRowTypes.GetValueOrDefault("Genres", false))
            {
                var genreRows = CreateGenreRows(user, config);
                rows.AddRange(genreRows);
            }

            return Task.FromResult<IEnumerable<NetflixRowDto>>(rows.Take(config.MaxRows));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rows for user {UserId}", userId);
            return Task.FromResult<IEnumerable<NetflixRowDto>>(new List<NetflixRowDto>());
        }
    }

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetRowItemsAsync(Guid userId, string rowType, string? genre, int startIndex, int limit)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Task.FromResult(new QueryResult<BaseItemDto>());
        }

        try
        {
            return Task.FromResult(rowType.ToLowerInvariant() switch
            {
                "mylist" => GetMyListItems(user, startIndex, limit),
                "recentlyadded" => GetRecentlyAddedItems(user, startIndex, limit),
                "randompicks" => GetRandomPicksItems(user, startIndex, limit),
                "genre" when !string.IsNullOrEmpty(genre) => GetGenreItems(user, genre, startIndex, limit),
                _ => new QueryResult<BaseItemDto>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting row items for user {UserId}, rowType {RowType}", userId, rowType);
            return Task.FromResult(new QueryResult<BaseItemDto>());
        }
    }

    private NetflixRowDto? CreateMyListRow(User user)
    {
        try
        {
            var query = new InternalItemsQuery(user)
            {
                IsFavorite = true,
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                Limit = 6
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
                PreviewItems = ConvertToBaseItemDtos(result.Items.Take(6), user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MyList row");
            return null;
        }
    }

    private NetflixRowDto? CreateRecentlyAddedRow(User user)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var cutoffDate = DateTime.UtcNow.AddDays(-config.RecentlyAddedDays);

            var query = new InternalItemsQuery(user)
            {
                MinDateCreated = cutoffDate,
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
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
                PreviewItems = ConvertToBaseItemDtos(result.Items.Take(6), user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RecentlyAdded row");
            return null;
        }
    }

    private NetflixRowDto? CreateRandomPicksRow(User user)
    {
        try
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series }
            };

            var result = _libraryManager.GetItemsResult(query);
            if (result.TotalRecordCount == 0)
            {
                return null;
            }

            // Manual random selection since OrderBy is problematic
            var random = new Random();
            var randomItems = result.Items
                .OrderBy(x => random.Next())
                .Take(6)
                .ToList();

            return new NetflixRowDto
            {
                Id = "randompicks",
                Title = "Zufällige Auswahl",
                Type = "RandomPicks",
                ItemCount = result.TotalRecordCount,
                PreviewItems = ConvertToBaseItemDtos(randomItems, user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RandomPicks row");
            return null;
        }
    }

    private List<NetflixRowDto> CreateGenreRows(User user, PluginConfiguration config)
    {
        var rows = new List<NetflixRowDto>();

        foreach (var genre in config.EnabledGenres)
        {
            try
            {
                if (config.BlacklistedGenres.Contains(genre))
                    continue;

                var query = new InternalItemsQuery(user)
                {
                    Genres = new[] { genre },
                    Recursive = true,
                    IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series }
                };

                var result = _libraryManager.GetItemsResult(query);

                if (result.TotalRecordCount >= config.MinGenreItems)
                {
                    // Manual random selection
                    var random = new Random();
                    var randomItems = result.Items
                        .OrderBy(x => random.Next())
                        .Take(6)
                        .ToList();

                    rows.Add(new NetflixRowDto
                    {
                        Id = $"genre-{genre.ToLowerInvariant()}",
                        Title = genre,
                        Type = "Genre",
                        Genre = genre,
                        ItemCount = result.TotalRecordCount,
                        PreviewItems = ConvertToBaseItemDtos(randomItems, user)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating genre row for {Genre}", genre);
            }
        }

        return rows;
    }

    private QueryResult<BaseItemDto> GetMyListItems(User user, int startIndex, int limit)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            
            var query = new InternalItemsQuery(user)
            {
                IsFavorite = true,
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                StartIndex = startIndex,
                Limit = Math.Min(limit, config.MyListLimit)
            };

            var result = _libraryManager.GetItemsResult(query);
            var dtos = ConvertToBaseItemDtos(result.Items, user);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount,
                StartIndex = startIndex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MyList items");
            return new QueryResult<BaseItemDto>();
        }
    }

    private QueryResult<BaseItemDto> GetRecentlyAddedItems(User user, int startIndex, int limit)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var cutoffDate = DateTime.UtcNow.AddDays(-config.RecentlyAddedDays);

            var query = new InternalItemsQuery(user)
            {
                MinDateCreated = cutoffDate,
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                StartIndex = startIndex,
                Limit = Math.Min(limit, config.MaxItemsPerRow)
            };

            var result = _libraryManager.GetItemsResult(query);
            var dtos = ConvertToBaseItemDtos(result.Items, user);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount,
                StartIndex = startIndex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently added items");
            return new QueryResult<BaseItemDto>();
        }
    }

    private QueryResult<BaseItemDto> GetRandomPicksItems(User user, int startIndex, int limit)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series }
            };

            var result = _libraryManager.GetItemsResult(query);
            
            // Manual random selection and pagination
            var random = new Random();
            var randomItems = result.Items
                .OrderBy(x => random.Next())
                .Skip(startIndex)
                .Take(Math.Min(limit, config.MaxItemsPerRow))
                .ToList();

            var dtos = ConvertToBaseItemDtos(randomItems, user);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount,
                StartIndex = startIndex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random picks items");
            return new QueryResult<BaseItemDto>();
        }
    }

    private QueryResult<BaseItemDto> GetGenreItems(User user, string genre, int startIndex, int limit)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var query = new InternalItemsQuery(user)
            {
                Genres = new[] { genre },
                Recursive = true,
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series }
            };

            var result = _libraryManager.GetItemsResult(query);
            
            // Manual random selection and pagination
            var random = new Random();
            var randomItems = result.Items
                .OrderBy(x => random.Next())
                .Skip(startIndex)
                .Take(Math.Min(limit, config.MaxItemsPerRow))
                .ToList();

            var dtos = ConvertToBaseItemDtos(randomItems, user);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount,
                StartIndex = startIndex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting genre items for {Genre}", genre);
            return new QueryResult<BaseItemDto>();
        }
    }

    private List<BaseItemDto> ConvertToBaseItemDtos(IEnumerable<BaseItem> items, User user)
    {
        try
        {
            var dtoOptions = new DtoOptions()
            {
                EnableImages = true,
                Fields = new[]
                {
                    ItemFields.PrimaryImageAspectRatio,
                    ItemFields.MediaSourceCount
                }
            };

            var dtos = new List<BaseItemDto>();
            foreach (var item in items)
            {
                try
                {
                    var dto = _dtoService.GetBaseItemDto(item, dtoOptions, user);
                    dtos.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting item {ItemId} to DTO", item.Id);
                }
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting items to DTOs");
            return new List<BaseItemDto>();
        }
    }
}