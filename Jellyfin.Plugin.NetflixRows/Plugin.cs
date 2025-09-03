using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Jellyfin.Plugin.NetflixRows.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetflixRows;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

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
        
        // Log configuration path for debugging
        var configPath = ConfigurationFilePath;
        _logger.LogInformation("Netflix Rows config file path: {ConfigPath}", configPath);
        
        // Register transformations for web client modifications
        RegisterWebTransformations();
        
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
                Name = "Netflix Rows Configuration",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }

    private void RegisterWebTransformations()
    {
        try
        {
            _logger.LogInformation("Attempting to register web transformations");
            
            var assembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation", StringComparison.OrdinalIgnoreCase) ?? false);

            if (assembly != null)
            {
                _logger.LogInformation("File Transformation plugin found: {AssemblyName}", assembly.FullName);
                
                var pluginInterfaceType = assembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
                if (pluginInterfaceType != null)
                {
                    _logger.LogInformation("PluginInterface type found, registering transformations");

                    // Register JavaScript transformation
                    var jsPayload = new
                    {
                        id = Guid.NewGuid().ToString(),
                        fileNamePattern = @".*main.*\.js$",
                        callbackAssembly = GetType().Assembly.FullName,
                        callbackClass = "Jellyfin.Plugin.NetflixRows.Transformations.JsTransformation",
                        callbackMethod = "TransformJs"
                    };

                    // Register CSS transformation
                    var cssPayload = new
                    {
                        id = Guid.NewGuid().ToString(),
                        fileNamePattern = @".*\.css$",
                        callbackAssembly = GetType().Assembly.FullName,
                        callbackClass = "Jellyfin.Plugin.NetflixRows.Transformations.CssTransformation",
                        callbackMethod = "TransformCss"
                    };

                    var registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
                    if (registerMethod != null)
                    {
                        registerMethod.Invoke(null, new object[] { JsonSerializer.Serialize(jsPayload) });
                        registerMethod.Invoke(null, new object[] { JsonSerializer.Serialize(cssPayload) });
                        _logger.LogInformation("Web transformations registered successfully");
                    }
                    else
                    {
                        _logger.LogWarning("RegisterTransformation method not found on PluginInterface");
                    }
                }
                else
                {
                    _logger.LogWarning("File Transformation PluginInterface type not found");
                }
            }
            else
            {
                _logger.LogWarning("File Transformation plugin not found. Netflix Rows will work with limited functionality.");
                _logger.LogInformation("Available assemblies: {Assemblies}", 
                    string.Join(", ", AssemblyLoadContext.All.SelectMany(x => x.Assemblies).Select(a => a.GetName().Name)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register web transformations");
        }
    }

    /// <summary>
    /// Called when the server is starting up.
    /// </summary>
    public void OnServerStartup()
    {
        _logger.LogInformation("Netflix Rows Plugin server startup complete");
        _logger.LogInformation("Current configuration: {Config}", JsonSerializer.Serialize(Configuration));
    }
}