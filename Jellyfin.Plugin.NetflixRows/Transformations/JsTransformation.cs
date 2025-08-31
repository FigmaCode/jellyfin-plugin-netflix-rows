using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Transformations;

public static class JsTransformation
{
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
        catch (Exception)
        {
            return data;
        }
    }

    private static string GetNetflixRowsJs()
    {
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
            // Ignore and use fallback
        }
        
        return GetScriptLoader();
    }

    private static string GetScriptLoader()
    {
        var scriptLines = new string[]
        {
            "console.log('Netflix Rows Plugin - Loading');",
            "var script = document.createElement('script');",
            "script.src = '/NetflixRows/Script';",
            "script.async = true;",
            "document.head.appendChild(script);"
        };
        
        return string.Join(Environment.NewLine, scriptLines);
    }

    public class TransformData
    {
        public string? Contents { get; set; }
    }
}