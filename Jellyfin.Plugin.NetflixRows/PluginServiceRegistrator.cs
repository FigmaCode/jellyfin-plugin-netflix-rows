using Jellyfin.Plugin.NetflixRows.Controllers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.NetflixRows;

/// <summary>
/// Register plugin services for dependency injection.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register the Netflix Rows controller
        serviceCollection.AddScoped<NetflixRowsController>();
    }
}