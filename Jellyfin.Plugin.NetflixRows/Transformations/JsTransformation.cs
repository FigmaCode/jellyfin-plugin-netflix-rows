using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Transformations;

/// <summary>
/// JavaScript transformation for injecting Netflix Rows functionality.
/// </summary>
public static class JsTransformation
{
    /// <summary>
    /// Transforms JavaScript files to inject Netflix Rows functionality.
    /// </summary>
    /// <param name="data">Transformation data containing file contents.</param>
    /// <returns>Modified file contents.</returns>
    public static string TransformJs(string data)
    {
        try
        {
            var transformData = JsonSerializer.Deserialize<TransformData>(data);
            if (transformData?.Contents == null)
            {
                return data;
            }

            var jsCode = GetNetflixRowsJs();
            
            // Inject our Netflix Rows JavaScript at the end of the main file
            var modifiedContents = transformData.Contents + Environment.NewLine + jsCode;

            return JsonSerializer.Serialize(new { contents = modifiedContents });
        }
        catch (Exception)
        {
            return data;
        }
    }

    private static string GetNetflixRowsJs()
    {
        // Try to load from embedded resource first
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.NetflixRows.Web.netflixRows.js";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
        catch
        {
            // Fall back to minimal JavaScript if resource loading fails
        }
        
        // Minimal fallback - just inject a script tag to load external file
        return GetMinimalJs();
    }

    private static string GetMinimalJs()
    {
        // Return minimal JavaScript that loads the external file
        return @"
(function() {
    console.log('Netflix Rows Plugin - Loading external script');
    
    // Try to load from external script
    var script = document.createElement('script');
    script.src = '/NetflixRows/Script';
    script.async = true;
    script.onerror = function() {
        console.warn('Could not load Netflix Rows external script, using fallback');
        initFallback();
    };
    document.head.appendChild(script);
    
    function initFallback() {
        console.log('Netflix Rows fallback initialization');
        
        function createBasicRows() {
            var homeView = document.querySelector('.homeView');
            if (!homeView || document.getElementById('netflix-rows-container')) return;
            
            var container = document.createElement('div');
            container.id = 'netflix-rows-container';
            container.innerHTML = '<div style=\"padding: 20px; text-align: center; color: #fff;\">Netflix Rows Plugin Active<br><small>Configure in Admin Dashboard</small></div>';
            
            var existing = homeView.querySelector('.sections, .homePageContent');
            if (existing) {
                existing.parentNode.insertBefore(container, existing);
            } else {
                homeView.appendChild(container);
            }
        }
        
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', createBasicRows);
        } else {
            createBasicRows();
        }
        
        var observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1 && (node.classList.contains('homeView') || node.querySelector('.homeView'))) {
                        setTimeout(createBasicRows, 100);
                    }
                });
            });
        });
        
        observer.observe(document.body, { childList: true, subtree: true });
    }
})();";
    }

    /// <summary>
    /// Transform data structure.
    /// </summary>
    public class TransformData
    {
        /// <summary>
        /// Gets or sets the file contents.
        /// </summary>
        public string? Contents { get; set; }
    }
}}
    
    init();
})();
""";
    }

    /// <summary>
    /// Transform data structure.
    /// </summary>
    public class TransformData
    {
        /// <summary>
        /// Gets or sets the file contents.
        /// </summary>
        public string? Contents { get; set; }
    }
}