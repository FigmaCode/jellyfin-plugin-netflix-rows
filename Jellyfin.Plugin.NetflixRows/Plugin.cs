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
/// Netflix Rows Plugin for Jellyfin - Transforms the home screen with Netflix-style horizontal content rows.
/// </summary>
/// <remarks>
/// This plugin provides a Netflix-like streaming experience by:
/// <list type="bullet">
/// <item><description>Creating horizontal scrolling content rows</description></item>
/// <item><description>Organizing content by categories (My List, Recently Added, Genres, etc.)</description></item>
/// <item><description>Integrating with Jellyfin's File Transformation and Home Screen Sections plugins</description></item>
/// <item><description>Providing responsive design for all device types</description></item>
/// <item><description>Supporting theme integration and accessibility</description></item>
/// </list>
/// 
/// <para><strong>Architecture Overview:</strong></para>
/// <para>
/// The plugin follows a modular architecture with three main components:
/// </para>
/// <list type="number">
/// <item><description><strong>File Transformation Integration:</strong> Injects CSS and JavaScript for Netflix-style UI</description></item>
/// <item><description><strong>API Controllers:</strong> Provide content endpoints for different row types</description></item>
/// <item><description><strong>Configuration System:</strong> Manages user preferences and display settings</description></item>
/// </list>
/// 
/// <para><strong>Dependencies:</strong></para>
/// <list type="bullet">
/// <item><description>File Transformation Plugin (required for UI styling)</description></item>
/// <item><description>Home Screen Sections Plugin (optional, for enhanced integration)</description></item>
/// </list>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// <para>
/// This plugin is optimized for performance with:
/// </para>
/// <list type="bullet">
/// <item><description>LoggerMessage delegates for high-performance logging</description></item>
/// <item><description>Async operations with ConfigureAwait(false)</description></item>
/// <item><description>Efficient resource disposal patterns</description></item>
/// <item><description>Lazy loading for content sections</description></item>
/// </list>
/// </remarks>
/// <example>
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Plugin automatically registers during Jellyfin startup
/// // Access configuration via Admin Dashboard → Plugins → Netflix Rows
/// 
/// // API endpoints are available at:
/// // GET /NetflixRows/Test - Health check
/// // GET /NetflixRows/Config - Current configuration
/// // POST /NetflixRows/MyListSection - My List content
/// // POST /NetflixRows/RecentlyAddedSection - Recently added content
/// </code>
/// </example>
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
    /// Gets the current plugin instance for global access throughout the application.
    /// </summary>
    /// <value>
    /// The singleton instance of the Netflix Rows plugin, or <c>null</c> if the plugin hasn't been initialized yet.
    /// </value>
    /// <remarks>
    /// This static property provides access to the plugin instance from anywhere in the application,
    /// particularly useful for:
    /// <list type="bullet">
    /// <item><description>Accessing configuration settings from controllers</description></item>
    /// <item><description>Checking plugin availability from other components</description></item>
    /// <item><description>Debugging and diagnostic purposes</description></item>
    /// </list>
    /// 
    /// <para><strong>Thread Safety:</strong> This property is thread-safe for reads but should only be set during plugin initialization.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Access plugin configuration from anywhere
    /// var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
    /// 
    /// // Check if plugin is available
    /// if (Plugin.Instance != null)
    /// {
    ///     // Plugin is loaded and available
    /// }
    /// </code>
    /// </example>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class with required dependencies.
    /// </summary>
    /// <param name="applicationPaths">
    /// Application paths interface providing access to Jellyfin's directory structure.
    /// Used for locating configuration files and plugin resources.
    /// </param>
    /// <param name="xmlSerializer">
    /// XML serialization interface for reading and writing plugin configuration.
    /// Handles serialization of <see cref="PluginConfiguration"/> objects.
    /// </param>
    /// <param name="logger">
    /// Logger instance for recording plugin activities, errors, and diagnostic information.
    /// Uses high-performance LoggerMessage delegates for optimal performance.
    /// </param>
    /// <remarks>
    /// <para><strong>Initialization Process:</strong></para>
    /// <list type="number">
    /// <item><description>Sets the singleton Instance property for global access</description></item>
    /// <item><description>Initializes logging with performance-optimized LoggerMessage delegates</description></item>
    /// <item><description>Asynchronously registers File Transformation for CSS/JS injection</description></item>
    /// <item><description>Asynchronously registers sections with Home Screen Sections plugin</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// The constructor uses fire-and-forget async operations for plugin integrations.
    /// Failures in these operations are logged but don't prevent plugin initialization,
    /// allowing the plugin to function in degraded mode if dependencies are unavailable.
    /// </para>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// <para>
    /// Plugin registration operations run asynchronously to avoid blocking Jellyfin startup.
    /// This ensures fast server startup times even if external plugins are slow to respond.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Plugin is automatically instantiated by Jellyfin during server startup
    /// // Manual instantiation is not typically required, but would look like:
    /// 
    /// var plugin = new Plugin(applicationPaths, xmlSerializer, logger);
    /// // Plugin.Instance is now available globally
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required parameters (<paramref name="applicationPaths"/>, 
    /// <paramref name="xmlSerializer"/>, or <paramref name="logger"/>) are <c>null</c>.
    /// </exception>
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

    /// <summary>
    /// Gets the human-readable name of the plugin as displayed in the Jellyfin admin interface.
    /// </summary>
    /// <value>The display name "Netflix Rows" for the plugin.</value>
    /// <remarks>
    /// This name appears in:
    /// <list type="bullet">
    /// <item><description>Admin Dashboard → Plugins → My Plugins</description></item>
    /// <item><description>Plugin configuration pages</description></item>
    /// <item><description>Log entries and error messages</description></item>
    /// <item><description>Plugin repository listings</description></item>
    /// </list>
    /// </remarks>
    public override string Name => "Netflix Rows";

    /// <summary>
    /// Gets the unique identifier for this plugin instance.
    /// </summary>
    /// <value>A unique GUID that identifies this plugin across all Jellyfin installations.</value>
    /// <remarks>
    /// <para><strong>Important:</strong> This GUID must remain constant across all versions of the plugin
    /// to ensure proper plugin updates and configuration persistence.</para>
    /// 
    /// <para>This identifier is used for:</para>
    /// <list type="bullet">
    /// <item><description>Plugin registration and discovery</description></item>
    /// <item><description>Configuration file management</description></item>
    /// <item><description>Update detection and compatibility checking</description></item>
    /// <item><description>Dependency resolution between plugins</description></item>
    /// </list>
    /// 
    /// <para><strong>Format:</strong> Standard GUID format (8-4-4-4-12 hexadecimal digits)</para>
    /// </remarks>
    public override Guid Id => Guid.Parse("12345678-1234-5678-9abc-123456789012");

    /// <summary>
    /// Gets a comprehensive description of the plugin's functionality and features.
    /// </summary>
    /// <value>
    /// A detailed description explaining how the plugin transforms Jellyfin into a Netflix-like experience.
    /// </value>
    /// <remarks>
    /// This description is displayed in:
    /// <list type="bullet">
    /// <item><description>Plugin catalog listings</description></item>
    /// <item><description>Plugin repository metadata</description></item>
    /// <item><description>Admin dashboard plugin details</description></item>
    /// <item><description>Installation and update dialogs</description></item>
    /// </list>
    /// 
    /// <para>The description should be concise yet informative, highlighting key features that differentiate
    /// this plugin from others in the ecosystem.</para>
    /// </remarks>
    public override string Description => "Transform your Jellyfin home screen into a Netflix-like experience with dynamic rows, genres, and a custom watchlist.";

    /// <summary>
    /// Provides configuration pages for the plugin's admin interface.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <see cref="PluginPageInfo"/> objects describing the available configuration pages.
    /// </returns>
    /// <remarks>
    /// <para><strong>Configuration Page Features:</strong></para>
    /// <list type="bullet">
    /// <item><description>Enable/disable different content sections (My List, Recently Added, etc.)</description></item>
    /// <item><description>Configure content limits and display preferences</description></item>
    /// <item><description>Customize genre selections and display names</description></item>
    /// <item><description>Adjust performance and responsive design settings</description></item>
    /// </list>
    /// 
    /// <para><strong>Technical Implementation:</strong></para>
    /// <para>
    /// The configuration page is served as an embedded HTML resource from the plugin assembly.
    /// It uses JavaScript to interact with the plugin's API endpoints for real-time configuration updates.
    /// </para>
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// <para>
    /// Access to configuration pages is restricted to administrators and requires proper authentication
    /// through Jellyfin's built-in security system.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configuration page is automatically registered and accessible at:
    /// // http://your-server:8096/web/configurationpage?name=NetflixRowsConfigPage
    /// 
    /// // The page provides a user-friendly interface for settings like:
    /// var configExample = new PluginConfiguration
    /// {
    ///     EnableMyList = true,
    ///     EnableRecentlyAdded = true,
    ///     MaxItemsPerRow = 25,
    ///     EnabledGenres = new[] { "Action", "Comedy", "Drama" }
    /// };
    /// </code>
    /// </example>
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