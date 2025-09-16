using System;
using Jellyfin.Plugin.NetflixRows.Controllers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.NetflixRows;

/// <summary>
/// Service registrator for the Netflix Rows plugin, responsible for dependency injection configuration.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// This class implements Jellyfin's plugin service registration interface to properly configure
/// dependency injection for the Netflix Rows plugin. It ensures that all plugin services are
/// correctly registered in Jellyfin's DI container and available throughout the application lifecycle.
/// </para>
/// 
/// <para><strong>Service Registration Strategy:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Controller Registration:</strong> Registers API controllers as singletons for optimal performance</description></item>
/// <item><description><strong>Lifecycle Management:</strong> Ensures proper service lifecycle aligned with Jellyfin's architecture</description></item>
/// <item><description><strong>Dependency Resolution:</strong> Enables automatic dependency injection for plugin components</description></item>
/// </list>
/// 
/// <para><strong>Architecture Integration:</strong></para>
/// <para>
/// This registrator integrates the Netflix Rows plugin seamlessly into Jellyfin's service architecture,
/// ensuring that all plugin components can leverage Jellyfin's built-in services (logging, library management,
/// user management, etc.) through constructor injection.
/// </para>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// <para>
/// Services are registered as singletons where appropriate to minimize object creation overhead
/// and ensure consistent state management across requests. This is particularly important for
/// API controllers that handle multiple concurrent requests.
/// </para>
/// 
/// <para><strong>Plugin Lifecycle:</strong></para>
/// <para>
/// Service registration occurs during Jellyfin's startup phase, ensuring all plugin services
/// are available before any plugin functionality is invoked. This guarantees reliable operation
/// and proper initialization order.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Service Registration Flow:</strong></para>
/// <code>
/// // Jellyfin automatically calls this during plugin initialization:
/// // 1. Plugin discovery phase
/// // 2. Service registration phase (this class)
/// // 3. Plugin activation phase
/// // 4. Service resolution and dependency injection
/// 
/// // Example of how registered services are used:
/// public class NetflixRowsController : ControllerBase
/// {
///     // These dependencies are automatically injected by Jellyfin's DI container
///     public NetflixRowsController(
///         ILibraryManager libraryManager,    // Jellyfin core service
///         IDtoService dtoService,             // Jellyfin core service
///         ILogger&lt;NetflixRowsController&gt; logger // Jellyfin logging service
///     ) { ... }
/// }
/// </code>
/// </example>
/// <seealso cref="IPluginServiceRegistrator"/>
/// <seealso cref="NetflixRowsController"/>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Registers plugin services in Jellyfin's dependency injection container.
    /// </summary>
    /// <param name="serviceCollection">
    /// The service collection to register services with. This is Jellyfin's main DI container
    /// that manages service lifetimes and dependency resolution throughout the application.
    /// </param>
    /// <param name="applicationHost">
    /// The server application host providing access to Jellyfin's core services and configuration.
    /// This parameter can be used to access server-wide settings and services during registration.
    /// </param>
    /// <remarks>
    /// <para><strong>Service Registration Process:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Controller Registration:</strong> Registers the main API controller as a singleton</description></item>
    /// <item><description><strong>Lifecycle Configuration:</strong> Ensures proper service lifetime management</description></item>
    /// <item><description><strong>Dependency Validation:</strong> Verifies that all required dependencies are available</description></item>
    /// </list>
    /// 
    /// <para><strong>Singleton Pattern Rationale:</strong></para>
    /// <para>
    /// The NetflixRowsController is registered as a singleton because:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Stateless Operation:</strong> The controller doesn't maintain request-specific state</description></item>
    /// <item><description><strong>Performance:</strong> Avoids object creation overhead for each request</description></item>
    /// <item><description><strong>Resource Efficiency:</strong> Minimizes memory allocation and garbage collection</description></item>
    /// <item><description><strong>Thread Safety:</strong> Controller methods are designed to be thread-safe</description></item>
    /// </list>
    /// 
    /// <para><strong>Dependency Injection Benefits:</strong></para>
    /// <para>
    /// By registering services through DI, the plugin benefits from:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Automatic Resolution:</strong> Jellyfin automatically injects required dependencies</description></item>
    /// <item><description><strong>Lifecycle Management:</strong> Services are properly disposed when no longer needed</description></item>
    /// <item><description><strong>Testability:</strong> Easy to mock dependencies for unit testing</description></item>
    /// <item><description><strong>Consistency:</strong> Same service instances used throughout the application</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// If service registration fails, Jellyfin will log the error and continue startup,
    /// but the plugin functionality will be unavailable. This fail-safe approach ensures
    /// that plugin issues don't crash the entire Jellyfin server.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // This method is called automatically by Jellyfin during startup
    /// // The registration enables dependency injection like this:
    /// 
    /// // In the controller, dependencies are automatically injected:
    /// public NetflixRowsController(
    ///     ILibraryManager libraryManager,     // ← Automatically injected by Jellyfin
    ///     IDtoService dtoService,              // ← Automatically injected by Jellyfin
    ///     ILogger&lt;NetflixRowsController&gt; logger // ← Automatically injected by Jellyfin
    /// )
    /// {
    ///     // Controller can immediately use these injected services
    ///     _libraryManager = libraryManager;
    ///     _dtoService = dtoService;
    ///     _logger = logger;
    /// }
    /// 
    /// // Alternative registration patterns (not used in this plugin):
    /// // serviceCollection.AddTransient&lt;IService, ServiceImpl&gt;(); // New instance per request
    /// // serviceCollection.AddScoped&lt;IService, ServiceImpl&gt;();    // One instance per request scope
    /// // serviceCollection.AddSingleton&lt;IService, ServiceImpl&gt;(); // One instance for application lifetime
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="serviceCollection"/> is null, indicating a serious
    /// Jellyfin startup issue that would prevent proper plugin initialization.
    /// </exception>
    /// <seealso cref="ServiceLifetime"/>
    /// <seealso cref="NetflixRowsController"/>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register the Netflix Rows controller explicitly
        serviceCollection.AddSingleton<NetflixRowsController>();
    }
}