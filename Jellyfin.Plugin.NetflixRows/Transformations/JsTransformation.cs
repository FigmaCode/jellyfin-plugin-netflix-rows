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
            var modifiedContents = transformData.Contents + Environment.NewLine + jsCode;

            return JsonSerializer.Serialize(new { contents = modifiedContents });
        }
        catch (JsonException)
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
        {
            // Fall back to simple JavaScript if resource loading fails
        }
        
        // Simple fallback that creates a basic test
        return GetBasicTestJs();
    }

    private static string GetBasicTestJs()
    {
        var lines = new[]
        {
            "console.log('Netflix Rows Plugin - Basic Test Active');",
            "",
            "// Basic test to verify plugin is working",
            "setTimeout(function() {",
            "  var homeView = document.querySelector('.homeView, [data-type=\"home\"]');",
            "  if (homeView) {",
            "    var testDiv = document.createElement('div');",
            "    testDiv.id = 'netflix-rows-test';",
            "    testDiv.style.cssText = 'background: #e50914; color: white; padding: 10px; margin: 10px; text-align: center; border-radius: 5px;';",
            "    testDiv.innerHTML = 'Netflix Rows Plugin Active - Check console for details';",
            "    homeView.insertBefore(testDiv, homeView.firstChild);",
            "    console.log('Netflix Rows: Test element added to home view');",
            "  } else {",
            "    console.log('Netflix Rows: Home view not found');",
            "  }",
            "}, 2000);",
            "",
            "// Try to load full script from server",
            "var script = document.createElement('script');",
            "script.src = '/NetflixRows/Script';",
            "script.async = true;",
            "script.onload = function() { console.log('Netflix Rows: External script loaded'); };",
            "script.onerror = function() { console.log('Netflix Rows: External script failed to load'); };",
            "document.head.appendChild(script);"
        };
        
        return string.Join(Environment.NewLine, lines);
    }

}

/// <summary>
/// Transform data structure for JavaScript transformations.
/// </summary>
internal class TransformData
{
    /// <summary>
    /// Gets or sets the file contents.
    /// </summary>
    public string? Contents { get; set; }
}