using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetflixRows.Frontend;

/// <summary>
/// Transforms the home page HTML to inject Netflix rows.
/// </summary>
public class HomeTransformation
{
    /// <summary>
    /// Transform home page content.
    /// </summary>
    /// <param name="input">Input JSON with contents.</param>
    /// <returns>Modified content.</returns>
    public static string TransformHome(string input)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TransformationData>(input);
            if (data?.Contents == null) return input;

            var content = data.Contents;

            // Inject Netflix Rows container right after the main content area
            var injectionPoint = content.IndexOf("<div class=\"homePage\"", StringComparison.OrdinalIgnoreCase);
            if (injectionPoint >= 0)
            {
                var insertPoint = content.IndexOf('>', injectionPoint) + 1;
                var netflixRowsHtml = @"
                    <div id=""netflix-rows-container"" class=""netflix-rows-container"">
                        <div id=""netflix-rows-loading"" class=""netflix-loading"">
                            <div class=""loading-spinner""></div>
                            <span>Loading Netflix Rows...</span>
                        </div>
                        <div id=""netflix-rows-content"" style=""display: none;""></div>
                    </div>";

                content = content.Insert(insertPoint, netflixRowsHtml);
            }

            return JsonSerializer.Serialize(new TransformationData { Contents = content });
        }
        catch (Exception)
        {
            return input;
        }
    }
}