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
        
        // Ultra-minimal fallback - just create script tag to load external
        return GetScriptLoader();
    }

    private static string GetScriptLoader()
    {
        // Create JavaScript lines individually to avoid string escaping issues
        var lines = new[]
        {
            "console.log('Netflix Rows Plugin - Loading');",
            "var script = document.createElement('script');",
            "script.src = '/NetflixRows/Script';",
            "script.async = true;",
            "document.head.appendChild(script);"
        };
        
        return string.Join(Environment.NewLine, lines);
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