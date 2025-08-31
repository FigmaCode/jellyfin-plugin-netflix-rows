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
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.NetflixRows;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        
        // Register transformations for web client modifications
        RegisterWebTransformations();
        
        // Register custom home sections
        RegisterHomeSections();
        
        // Register configuration page
        RegisterConfigurationPage();
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
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }

    private void RegisterWebTransformations()
    {
        try
        {
            var assembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation") ?? false);

            if (assembly != null)
            {
                var pluginInterfaceType = assembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
                if (pluginInterfaceType != null)
                {
                    // Register CSS transformation
                    var cssPayload = new
                    {
                        id = Guid.NewGuid().ToString(),
                        fileNamePattern = @".*\.css$",
                        callbackAssembly = GetType().Assembly.FullName,
                        callbackClass = "Jellyfin.Plugin.NetflixRows.Transformations.CssTransformation",
                        callbackMethod = "TransformCss"
                    };

                    // Register JS transformation  
                    var jsPayload = new
                    {
                        id = Guid.NewGuid().ToString(),
                        fileNamePattern = @".*main.*\.js$",
                        callbackAssembly = GetType().Assembly.FullName,
                        callbackClass = "Jellyfin.Plugin.NetflixRows.Transformations.JsTransformation",
                        callbackMethod = "TransformJs"
                    };

                    pluginInterfaceType.GetMethod("RegisterTransformation")?.Invoke(null, new object[] { JsonSerializer.Serialize(cssPayload) });
                    pluginInterfaceType.GetMethod("RegisterTransformation")?.Invoke(null, new object[] { JsonSerializer.Serialize(jsPayload) });
                }
            }
        }
        catch (Exception ex)
        {
            // Log error if transformation registration fails
            System.Diagnostics.Debug.WriteLine($"Failed to register transformations: {ex.Message}");
        }
    }

    private void RegisterHomeSections()
    {
        try
        {
            var assembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".HomeScreen") ?? false);

            if (assembly != null)
            {
                // Register with Home Screen Sections plugin
                var config = Configuration;
                
                if (config.EnableMyList)
                {
                    RegisterSection("netflix-my-list", "Meine Liste", "/NetflixRows/MyList");
                }
                
                if (config.EnableRecentlyAdded)
                {
                    RegisterSection("netflix-recently-added", "Kürzlich hinzugefügt", "/NetflixRows/RecentlyAdded");
                }
                
                if (config.EnableRandomPicks)
                {
                    RegisterSection("netflix-random", "Zufallsauswahl", "/NetflixRows/RandomPicks");
                }
                
                if (config.EnableLongNotWatched)
                {
                    RegisterSection("netflix-long-not-watched", "Lange nicht gesehen", "/NetflixRows/LongNotWatched");
                }
                
                // Register genre sections
                foreach (var genre in config.EnabledGenres)
                {
                    var displayName = config.GenreDisplayNames.ContainsKey(genre) 
                        ? config.GenreDisplayNames[genre] 
                        : genre;
                    RegisterSection($"netflix-genre-{genre.ToLowerInvariant()}", displayName, $"/NetflixRows/Genre/{genre}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register home sections: {ex.Message}");
        }
    }

    private void RegisterSection(string id, string displayText, string endpoint)
    {
        // This would integrate with the Home Sections plugin
        // Implementation depends on the exact API of that plugin
    }

    private void RegisterConfigurationPage()
    {
        try
        {
            var assembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".PluginPages") ?? false);

            if (assembly != null)
            {
                // Register configuration page with Plugin Pages
                var pageData = new
                {
                    name = "Netflix Rows Settings",
                    route = "netflixrows-settings",
                    menuText = "Netflix Rows",
                    iconClass = "material-icons view_module",
                    htmlPath = "Configuration/configPage.html"
                };
                
                // Register with Plugin Pages if available
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register configuration page: {ex.Message}");
        }
    }
}