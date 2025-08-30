using System;
using System.Collections.Generic;
using System.Globalization;
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
/// The main plugin class.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

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

        // Initialize frontend transformation
        InitializeFrontendTransformation();
    }

    /// <inheritdoc />
    public override string Name => "Netflix Rows";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("6c0b8d8a-6b1c-4c5a-9b0a-1b2c3d4e5f6a");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = "Jellyfin.Plugin.NetflixRows.Configuration.configPage.html"
            }
        };
    }

    private void InitializeFrontendTransformation()
    {
        try
        {
            _logger.LogInformation("Initializing Netflix Rows frontend transformation");

            // Register main home screen transformation
            RegisterTransformation(new
            {
                id = Guid.NewGuid(),
                fileNamePattern = @"home\.html$",
                callbackAssembly = GetType().Assembly.FullName,
                callbackClass = "Jellyfin.Plugin.NetflixRows.Frontend.HomeTransformation",
                callbackMethod = "TransformHome"
            });

            // Register CSS injection
            RegisterTransformation(new
            {
                id = Guid.NewGuid(),
                fileNamePattern = @"bundle\.css$",
                callbackAssembly = GetType().Assembly.FullName,
                callbackClass = "Jellyfin.Plugin.NetflixRows.Frontend.CSSTransformation",
                callbackMethod = "InjectNetflixCSS"
            });

            // Register JavaScript injection
            RegisterTransformation(new
            {
                id = Guid.NewGuid(),
                fileNamePattern = @"bundle\.js$",
                callbackAssembly = GetType().Assembly.FullName,
                callbackClass = "Jellyfin.Plugin.NetflixRows.Frontend.JSTransformation",
                callbackMethod = "InjectNetflixJS"
            });

            _logger.LogInformation("Netflix Rows frontend transformations registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize frontend transformations");
        }
    }

    private void RegisterTransformation(object payload)
    {
        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);

            Assembly? fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation") ?? false);

            if (fileTransformationAssembly != null)
            {
                Type? pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
                if (pluginInterfaceType != null)
                {
                    pluginInterfaceType.GetMethod("RegisterTransformation")?.Invoke(null, new object?[] { payloadJson });
                    _logger.LogDebug("Registered transformation: {Pattern}", payload.GetType().GetProperty("fileNamePattern")?.GetValue(payload));
                }
                else
                {
                    _logger.LogWarning("FileTransformation PluginInterface not found");
                }
            }
            else
            {
                _logger.LogWarning("FileTransformation plugin not found. Please install File Transformation plugin first.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering transformation");
        }
    }
}