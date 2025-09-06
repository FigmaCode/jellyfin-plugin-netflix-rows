using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.NetflixRows.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.NetflixRows;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{Plugin}"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logger = logger;
        
        _logger.LogInformation("Netflix Rows Plugin v{Version} initializing...", GetType().Assembly.GetName().Version);
        
        // Register with File Transformation for CSS styling
        _ = Task.Run(RegisterFileTransformationsAsync);
        
        // Register sections with Home Screen Sections plugin
        _ = Task.Run(RegisterNetflixSectionsAsync);
        
        _logger.LogInformation("Netflix Rows Plugin initialized successfully");
    }

    /// <inheritdoc />
    public override string Name => "Netflix Rows";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("12345678-1234-5678-9abc-123456789012");

    /// <inheritdoc />
    public override string Description => "Transform your Jellyfin home screen into a Netflix-like experience with dynamic rows, genres, and a custom watchlist.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        _logger.LogInformation("Getting plugin configuration pages");
        return new[]
        {
            new PluginPageInfo
            {
                Name = "NetflixRowsConfigPage",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }

    private async Task RegisterFileTransformationsAsync()
    {
        try
        {
            // Add a delay to ensure File Transformation plugin is fully loaded
            await Task.Delay(5000);
            
            _logger.LogInformation("[NetflixRows] Registering File Transformations...");
            
            // Find File Transformation assembly using reflection as per documentation
            Assembly? fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation") ?? false);

            if (fileTransformationAssembly == null)
            {
                _logger.LogWarning("[NetflixRows] File Transformation plugin not found. Install it from https://www.iamparadox.dev/jellyfin/plugins/manifest.json");
                return;
            }

            _logger.LogInformation("[NetflixRows] File Transformation plugin found: {AssemblyName}", fileTransformationAssembly.FullName);

            Type? pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            if (pluginInterfaceType == null)
            {
                _logger.LogWarning("[NetflixRows] File Transformation PluginInterface type not found");
                return;
            }

            MethodInfo? registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
            if (registerMethod == null)
            {
                _logger.LogWarning("[NetflixRows] RegisterTransformation method not found");
                return;
            }

            // Register CSS transformation for Netflix styling - payload must be JObject as per docs
            // Use specific pattern to only target main CSS files, not chunk CSS files
            var cssPayload = new JObject
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["fileNamePattern"] = @"main\.jellyfin\..*\.css$", // Only target main jellyfin CSS file
                ["callbackAssembly"] = GetType().Assembly.FullName,
                ["callbackClass"] = "Jellyfin.Plugin.NetflixRows.Transformations.CssTransformation",
                ["callbackMethod"] = "TransformCss"
            };

            registerMethod.Invoke(null, new object[] { cssPayload });
            _logger.LogInformation("[NetflixRows] CSS transformation registered successfully");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Failed to register file transformations");
        }
    }

    private async Task RegisterNetflixSectionsAsync()
    {
        try
        {
            // Wait much longer for server and plugins to be fully ready
            await Task.Delay(20000); // Increased from 10 to 20 seconds
            
            _logger.LogInformation("[NetflixRows] Registering Netflix sections with Home Screen Sections plugin...");
            
            var config = Configuration;
            var baseUrl = "http://localhost:8096"; // Local server URL
            
            // Check if Home Screen Sections plugin is available
            var assemblies = AssemblyLoadContext.All.SelectMany(x => x.Assemblies).ToList();
            var homeSectionsAssembly = assemblies.FirstOrDefault(x => 
                (x.FullName?.Contains("HomeScreenSections", StringComparison.OrdinalIgnoreCase) == true) ||
                (x.FullName?.Contains("Home.Sections", StringComparison.OrdinalIgnoreCase) == true) ||
                (x.FullName?.Contains("HomeSections", StringComparison.OrdinalIgnoreCase) == true));

            if (homeSectionsAssembly == null)
            {
                _logger.LogWarning("[NetflixRows] Home Screen Sections plugin assembly not found. Please ensure it's installed and enabled.");
                
                // Also try HTTP endpoint as fallback
                using var testClient = new HttpClient();
                try
                {
                    var testResponse = await testClient.GetAsync($"{baseUrl}/HomeScreen/");
                    if (!testResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("[NetflixRows] Home Screen Sections plugin not available via HTTP either (Status: {StatusCode}).", testResponse.StatusCode);
                        return;
                    }
                    _logger.LogInformation("[NetflixRows] Home Screen Sections plugin detected via HTTP endpoint.");
                }
                catch (Exception testEx)
                {
                    _logger.LogError(testEx, "[NetflixRows] Cannot connect to Home Screen Sections plugin. Please ensure it's installed and enabled.");
                    return;
                }
            }
            else
            {
                _logger.LogInformation("[NetflixRows] Home Screen Sections plugin assembly found: {AssemblyName}", homeSectionsAssembly.FullName);
            }
            
            var sections = new List<object>();
            
            // My List section
            if (config.EnableMyList)
            {
                sections.Add(new
                {
                    id = "netflix-my-list",
                    displayText = "My List",
                    limit = 1,
                    route = (string?)null,
                    additionalData = "MyList",
                    resultsEndpoint = "/NetflixRows/MyListSection"
                });
            }
            
            // Recently Added section
            if (config.EnableRecentlyAdded)
            {
                sections.Add(new
                {
                    id = "netflix-recently-added",
                    displayText = "Recently Added",
                    limit = 1,
                    route = (string?)null,
                    additionalData = "RecentlyAdded",
                    resultsEndpoint = "/NetflixRows/RecentlyAddedSection"
                });
            }
            
            // Random Picks section
            if (config.EnableRandomPicks)
            {
                sections.Add(new
                {
                    id = "netflix-random-picks",
                    displayText = "Random Picks",
                    limit = 1,
                    route = (string?)null,
                    additionalData = "RandomPicks",
                    resultsEndpoint = "/NetflixRows/RandomPicksSection"
                });
            }
            
            // Genre sections
            if (config.EnabledGenres != null)
            {
                // Use default display names for genres
                var genreDisplayNames = new Dictionary<string, string>
                {
                    { "Action", "Adrenalinkick" },
                    { "Comedy", "Zum Lachen" },
                    { "Drama", "Gefühlvoll" },
                    { "Horror", "Gruselig" },
                    { "Sci-Fi", "Science Fiction" },
                    { "Anime", "Anime" }
                };
                
                foreach (var genre in config.EnabledGenres)
                {
                    var displayName = genreDisplayNames.GetValueOrDefault(genre, genre);
                    sections.Add(new
                    {
                        id = $"netflix-genre-{genre.ToLowerInvariant()}",
                        displayText = displayName,
                        limit = 1,
                        route = (string?)null,
                        additionalData = genre,
                        resultsEndpoint = "/NetflixRows/GenreSection"
                    });
                }
            }

            // Register each section with retry logic
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var registeredCount = 0;
            
            foreach (var section in sections)
            {
                bool registered = false;
                int retryCount = 0;
                const int maxRetries = 5;
                
                while (!registered && retryCount < maxRetries)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(section);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        
                        if (retryCount > 0)
                        {
                            _logger.LogInformation("[NetflixRows] Retry {Retry}/{MaxRetries} for section registration", retryCount + 1, maxRetries);
                            await Task.Delay(5000 * retryCount); // Exponential backoff
                        }
                        
                        _logger.LogInformation("[NetflixRows] Attempting to register section: {SectionData}", json);
                        
                        var response = await httpClient.PostAsync($"{baseUrl}/HomeScreen/RegisterSection", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("[NetflixRows] Successfully registered section");
                            registeredCount++;
                            registered = true;
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            _logger.LogWarning("[NetflixRows] Server not ready yet (attempt {Attempt}): {StatusCode} - {Response}", 
                                retryCount + 1, response.StatusCode, responseContent);
                            retryCount++;
                        }
                        else
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            _logger.LogWarning("[NetflixRows] Failed to register section: {StatusCode} - {Response}", 
                                response.StatusCode, responseContent);
                            break; // Don't retry for other errors
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[NetflixRows] Error registering section (attempt {Attempt}): {Section}", retryCount + 1, section);
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(3000);
                        }
                    }
                }
                
                if (!registered)
                {
                    _logger.LogError("[NetflixRows] Failed to register section after {MaxRetries} attempts", maxRetries);
                }
            }
            
            _logger.LogInformation("[NetflixRows] Successfully registered {Registered}/{Total} Netflix sections", 
                registeredCount, sections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetflixRows] Failed to register Netflix sections");
        }
    }
}