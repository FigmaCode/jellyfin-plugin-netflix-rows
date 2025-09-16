using System;
using System.Collections.Generic;
using System.IO;
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

    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, Version?, Exception?> LogPluginInitializing = 
        LoggerMessage.Define<Version?>(LogLevel.Information, new EventId(1, nameof(LogPluginInitializing)), 
            "Netflix Rows Plugin v{Version} initializing...");
    
    private static readonly Action<ILogger, Exception?> LogPluginInitialized = 
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogPluginInitialized)), 
            "Netflix Rows Plugin initialized successfully");
    
    private static readonly Action<ILogger, Exception?> LogGettingConfigPages = 
        LoggerMessage.Define(LogLevel.Information, new EventId(3, nameof(LogGettingConfigPages)), 
            "Getting plugin configuration pages");
    
    private static readonly Action<ILogger, Exception?> LogRegisteringFileTransformations = 
        LoggerMessage.Define(LogLevel.Information, new EventId(4, nameof(LogRegisteringFileTransformations)), 
            "[NetflixRows] Registering File Transformations...");
    
    private static readonly Action<ILogger, Exception?> LogFileTransformationNotFound = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(5, nameof(LogFileTransformationNotFound)), 
            "[NetflixRows] File Transformation plugin not found. Install it from https://www.iamparadox.dev/jellyfin/plugins/manifest.json");
    
    private static readonly Action<ILogger, string?, Exception?> LogFileTransformationFound = 
        LoggerMessage.Define<string?>(LogLevel.Information, new EventId(6, nameof(LogFileTransformationFound)), 
            "[NetflixRows] File Transformation plugin found: {AssemblyName}");
    
    private static readonly Action<ILogger, Exception?> LogPluginInterfaceNotFound = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(7, nameof(LogPluginInterfaceNotFound)), 
            "[NetflixRows] File Transformation PluginInterface type not found");
    
    private static readonly Action<ILogger, Exception?> LogRegisterTransformationNotFound = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(8, nameof(LogRegisterTransformationNotFound)), 
            "[NetflixRows] RegisterTransformation method not found");
    
    private static readonly Action<ILogger, Exception?> LogCssTransformationRegistered = 
        LoggerMessage.Define(LogLevel.Information, new EventId(9, nameof(LogCssTransformationRegistered)), 
            "[NetflixRows] CSS transformation registered successfully");
    
    private static readonly Action<ILogger, Exception?> LogFailedToRegisterFileTransformations = 
        LoggerMessage.Define(LogLevel.Error, new EventId(10, nameof(LogFailedToRegisterFileTransformations)), 
            "[NetflixRows] Failed to register file transformations");
    
    private static readonly Action<ILogger, Exception?> LogRegisteringNetflixSections = 
        LoggerMessage.Define(LogLevel.Information, new EventId(11, nameof(LogRegisteringNetflixSections)), 
            "[NetflixRows] Registering Netflix sections with Home Screen Sections plugin...");
    
    private static readonly Action<ILogger, Exception?> LogHomeSectionsNotFound = 
        LoggerMessage.Define(LogLevel.Warning, new EventId(12, nameof(LogHomeSectionsNotFound)), 
            "[NetflixRows] Home Screen Sections plugin assembly not found. Please ensure it's installed and enabled.");
    
    private static readonly Action<ILogger, System.Net.HttpStatusCode, Exception?> LogHomeSectionsNotAvailableHttp = 
        LoggerMessage.Define<System.Net.HttpStatusCode>(LogLevel.Warning, new EventId(13, nameof(LogHomeSectionsNotAvailableHttp)), 
            "[NetflixRows] Home Screen Sections plugin not available via HTTP either (Status: {StatusCode}).");
    
    private static readonly Action<ILogger, Exception?> LogHomeSectionsDetected = 
        LoggerMessage.Define(LogLevel.Information, new EventId(14, nameof(LogHomeSectionsDetected)), 
            "[NetflixRows] Home Screen Sections plugin detected via HTTP endpoint.");
    
    private static readonly Action<ILogger, Exception?> LogCannotConnectHomeSections = 
        LoggerMessage.Define(LogLevel.Error, new EventId(15, nameof(LogCannotConnectHomeSections)), 
            "[NetflixRows] Cannot connect to Home Screen Sections plugin. Please ensure it's installed and enabled.");
    
    private static readonly Action<ILogger, string?, Exception?> LogHomeSectionsAssemblyFound = 
        LoggerMessage.Define<string?>(LogLevel.Information, new EventId(16, nameof(LogHomeSectionsAssemblyFound)), 
            "[NetflixRows] Home Screen Sections plugin assembly found: {AssemblyName}");
    
    private static readonly Action<ILogger, int, int, Exception?> LogSectionRegistrationRetry = 
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(17, nameof(LogSectionRegistrationRetry)), 
            "[NetflixRows] Retry {Retry}/{MaxRetries} for section registration");
    
    private static readonly Action<ILogger, string, Exception?> LogAttemptingToRegisterSection = 
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(18, nameof(LogAttemptingToRegisterSection)), 
            "[NetflixRows] Attempting to register section: {SectionData}");
    
    private static readonly Action<ILogger, Exception?> LogSuccessfullyRegisteredSection = 
        LoggerMessage.Define(LogLevel.Information, new EventId(19, nameof(LogSuccessfullyRegisteredSection)), 
            "[NetflixRows] Successfully registered section");
    
    private static readonly Action<ILogger, int, System.Net.HttpStatusCode, string, Exception?> LogServerNotReady = 
        LoggerMessage.Define<int, System.Net.HttpStatusCode, string>(LogLevel.Warning, new EventId(20, nameof(LogServerNotReady)), 
            "[NetflixRows] Server not ready yet (attempt {Attempt}): {StatusCode} - {Response}");
    
    private static readonly Action<ILogger, System.Net.HttpStatusCode, string, Exception?> LogFailedToRegisterSection = 
        LoggerMessage.Define<System.Net.HttpStatusCode, string>(LogLevel.Warning, new EventId(21, nameof(LogFailedToRegisterSection)), 
            "[NetflixRows] Failed to register section: {StatusCode} - {Response}");
    
    private static readonly Action<ILogger, int, object, Exception?> LogErrorRegisteringSection = 
        LoggerMessage.Define<int, object>(LogLevel.Error, new EventId(22, nameof(LogErrorRegisteringSection)), 
            "[NetflixRows] Error registering section (attempt {Attempt}): {Section}");
    
    private static readonly Action<ILogger, int, Exception?> LogFailedToRegisterAfterRetries = 
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(23, nameof(LogFailedToRegisterAfterRetries)), 
            "[NetflixRows] Failed to register section after {MaxRetries} attempts");
    
    private static readonly Action<ILogger, int, int, Exception?> LogSuccessfullyRegisteredNetflixSections = 
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(24, nameof(LogSuccessfullyRegisteredNetflixSections)), 
            "[NetflixRows] Successfully registered {Registered}/{Total} Netflix sections");
    
    private static readonly Action<ILogger, Exception?> LogFailedToRegisterNetflixSections = 
        LoggerMessage.Define(LogLevel.Error, new EventId(25, nameof(LogFailedToRegisterNetflixSections)), 
            "[NetflixRows] Failed to register Netflix sections");

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
        
        LogPluginInitializing(_logger, GetType().Assembly.GetName().Version, null);
        
        // Register with File Transformation for CSS styling
        _ = Task.Run(RegisterFileTransformationsAsync);
        
        // Register sections with Home Screen Sections plugin
        _ = Task.Run(RegisterNetflixSectionsAsync);
        
        LogPluginInitialized(_logger, null);
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
        LogGettingConfigPages(_logger, null);
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
            await Task.Delay(5000).ConfigureAwait(false);
            
            LogRegisteringFileTransformations(_logger, null);
            
            // Find File Transformation assembly using reflection as per documentation
            Assembly? fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation", StringComparison.OrdinalIgnoreCase) ?? false);

            if (fileTransformationAssembly == null)
            {
                LogFileTransformationNotFound(_logger, null);
                return;
            }

            LogFileTransformationFound(_logger, fileTransformationAssembly.FullName, null);

            Type? pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            if (pluginInterfaceType == null)
            {
                LogPluginInterfaceNotFound(_logger, null);
                return;
            }

            MethodInfo? registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
            if (registerMethod == null)
            {
                LogRegisterTransformationNotFound(_logger, null);
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
            LogCssTransformationRegistered(_logger, null);
            
        }
        catch (Exception ex) when (ex is ReflectionTypeLoadException or FileNotFoundException or TypeLoadException or MethodAccessException)
        {
            LogFailedToRegisterFileTransformations(_logger, ex);
        }
    }

    private async Task RegisterNetflixSectionsAsync()
    {
        try
        {
            // Wait much longer for server and plugins to be fully ready
            await Task.Delay(20000).ConfigureAwait(false); // Increased from 10 to 20 seconds
            
            LogRegisteringNetflixSections(_logger, null);
            
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
                LogHomeSectionsNotFound(_logger, null);
                
                // Also try HTTP endpoint as fallback
                using var testClient = new HttpClient();
                try
                {
                    var testResponse = await testClient.GetAsync(new Uri($"{baseUrl}/HomeScreen/")).ConfigureAwait(false);
                    if (!testResponse.IsSuccessStatusCode)
                    {
                        LogHomeSectionsNotAvailableHttp(_logger, testResponse.StatusCode, null);
                        return;
                    }
                    LogHomeSectionsDetected(_logger, null);
                }
                catch (Exception testEx) when (testEx is HttpRequestException or TaskCanceledException or InvalidOperationException)
                {
                    LogCannotConnectHomeSections(_logger, testEx);
                    return;
                }
            }
            else
            {
                LogHomeSectionsAssemblyFound(_logger, homeSectionsAssembly.FullName, null);
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
                foreach (var genre in config.EnabledGenres)
                {
                    sections.Add(new
                    {
                        id = $"netflix-genre-{genre.ToUpperInvariant()}",
                        displayText = genre, // Use genre name directly
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
                        using var content = new StringContent(json, Encoding.UTF8, "application/json");
                        
                        if (retryCount > 0)
                        {
                            LogSectionRegistrationRetry(_logger, retryCount + 1, maxRetries, null);
                            await Task.Delay(5000 * retryCount).ConfigureAwait(false); // Exponential backoff
                        }
                        
                        LogAttemptingToRegisterSection(_logger, json, null);
                        
                        var response = await httpClient.PostAsync(new Uri($"{baseUrl}/HomeScreen/RegisterSection"), content).ConfigureAwait(false);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            LogSuccessfullyRegisteredSection(_logger, null);
                            registeredCount++;
                            registered = true;
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            LogServerNotReady(_logger, retryCount + 1, response.StatusCode, responseContent, null);
                            retryCount++;
                        }
                        else
                        {
                            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            LogFailedToRegisterSection(_logger, response.StatusCode, responseContent, null);
                            break; // Don't retry for other errors
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
                    {
                        LogErrorRegisteringSection(_logger, retryCount + 1, section, ex);
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(3000).ConfigureAwait(false);
                        }
                    }
                }
                
                if (!registered)
                {
                    LogFailedToRegisterAfterRetries(_logger, maxRetries, null);
                }
            }
            
            LogSuccessfullyRegisteredNetflixSections(_logger, registeredCount, sections.Count, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or TaskCanceledException)
        {
            LogFailedToRegisterNetflixSections(_logger, ex);
        }
    }
}