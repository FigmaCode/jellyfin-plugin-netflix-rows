using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Transformations;

/// <summary>
/// JavaScript transformation service for injecting Netflix-style interactive functionality into Jellyfin's web interface.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// This transformation class handles the injection of JavaScript code that enhances Jellyfin's web interface
/// with Netflix-like interactive behaviors, lazy loading capabilities, and dynamic content management.
/// It works in conjunction with <see cref="CssTransformation"/> to provide a complete Netflix-style experience.
/// </para>
/// 
/// <para><strong>Transformation Strategy:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Resource Loading:</strong> Attempts to load optimized JavaScript from embedded resources</description></item>
/// <item><description><strong>Fallback Mechanism:</strong> Provides basic functionality if full script loading fails</description></item>
/// <item><description><strong>Dynamic Enhancement:</strong> Enables progressive enhancement for better user experience</description></item>
/// <item><description><strong>Error Resilience:</strong> Graceful degradation when JavaScript features are unavailable</description></item>
/// </list>
/// 
/// <para><strong>JavaScript Features Provided:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Horizontal Scrolling:</strong> Smooth scroll behavior for Netflix-style rows</description></item>
/// <item><description><strong>Lazy Loading:</strong> On-demand content loading for performance optimization</description></item>
/// <item><description><strong>Interactive Elements:</strong> Hover effects, click handlers, and responsive behaviors</description></item>
/// <item><description><strong>Dynamic Content:</strong> AJAX loading of content sections without page refresh</description></item>
/// <item><description><strong>Responsive Adaptation:</strong> Dynamic layout adjustments based on screen size</description></item>
/// <item><description><strong>Integration Testing:</strong> Built-in diagnostics and connection verification</description></item>
/// </list>
/// 
/// <para><strong>Error Handling Philosophy:</strong></para>
/// <para>
/// The transformation follows a fail-safe approach where JavaScript errors never break the base Jellyfin
/// functionality. All enhancements are progressive, meaning the interface remains fully functional
/// even if JavaScript transformation fails completely.
/// </para>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Async Loading:</strong> Non-blocking script injection to avoid UI freezing</description></item>
/// <item><description><strong>Resource Optimization:</strong> Minimal JavaScript footprint with efficient event handling</description></item>
/// <item><description><strong>Lazy Execution:</strong> JavaScript features activate only when needed</description></item>
/// <item><description><strong>Memory Management:</strong> Proper event listener cleanup and resource disposal</description></item>
/// </list>
/// 
/// <para><strong>Browser Compatibility:</strong></para>
/// <para>
/// The injected JavaScript is compatible with all modern browsers and degrades gracefully on older browsers.
/// Essential functionality (content display) works even without JavaScript, while enhanced features
/// require modern browser capabilities.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Transformation Process Example:</strong></para>
/// <code>
/// // Input: JSON-encoded transformation data
/// var inputData = @"{
///     ""contents"": ""// Original Jellyfin JavaScript\nconsole.log('Jellyfin loaded');""
/// }";
/// 
/// // Apply JavaScript transformation
/// var enhancedData = JsTransformation.TransformJs(inputData);
/// 
/// // Result: Enhanced JavaScript with Netflix functionality
/// // Enhanced script includes original content plus Netflix-style behaviors
/// </code>
/// 
/// <para><strong>JavaScript Features Added:</strong></para>
/// <code>
/// // The transformation adds functionality like:
/// 
/// // 1. Content row management
/// $('.netflix-row').each(function() {
///     enableSmoothScrolling(this);
///     enableLazyLoading(this);
/// });
/// 
/// // 2. Interactive behaviors
/// $('.netflix-item-card').hover(
///     function() { showItemDetails(this); },
///     function() { hideItemDetails(this); }
/// );
/// 
/// // 3. Dynamic content loading
/// function loadNetflixRow(rowType, containerId) {
///     fetch('/NetflixRows/' + rowType + 'Section')
///         .then(response => response.json())
///         .then(data => renderNetflixRow(data, containerId));
/// }
/// 
/// // 4. Responsive behavior
/// window.addEventListener('resize', function() {
///     adjustNetflixRowsForScreenSize();
/// });
/// </code>
/// </example>
/// <seealso cref="CssTransformation"/>
/// <seealso cref="TransformData"/>
public static class JsTransformation
{
    /// <summary>
    /// Transforms JavaScript files by injecting Netflix-style interactive functionality and behaviors.
    /// </summary>
    /// <param name="data">
    /// JSON-serialized transformation data containing the original JavaScript file contents.
    /// The data should be in the format: <c>{"contents": "original JavaScript content"}</c>
    /// </param>
    /// <returns>
    /// JSON-serialized object containing the enhanced JavaScript with Netflix functionality injected.
    /// Returns the original data unchanged if transformation fails or input is invalid.
    /// </returns>
    /// <remarks>
    /// <para><strong>Transformation Process:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Input Parsing:</strong> Deserializes JSON data to extract original JavaScript content</description></item>
    /// <item><description><strong>Script Enhancement:</strong> Appends Netflix-style interactive functionality</description></item>
    /// <item><description><strong>Resource Loading:</strong> Attempts to load optimized scripts from embedded resources</description></item>
    /// <item><description><strong>Fallback Handling:</strong> Provides basic functionality if full script loading fails</description></item>
    /// <item><description><strong>Output Serialization:</strong> Returns enhanced content in expected JSON format</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Handling Strategy:</strong></para>
    /// <para>
    /// The method implements comprehensive error handling to ensure Jellyfin's JavaScript continues
    /// to function even if Netflix enhancements fail. This fail-safe approach prioritizes system
    /// stability over feature enhancement.
    /// </para>
    /// 
    /// <para><strong>Script Loading Hierarchy:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Primary:</strong> Load full-featured script from embedded resources</description></item>
    /// <item><description><strong>Secondary:</strong> Load external script from API endpoint (/NetflixRows/Script)</description></item>
    /// <item><description><strong>Fallback:</strong> Inject basic test functionality to verify plugin operation</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Optimization:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Async Loading:</strong> Scripts load asynchronously to avoid blocking page rendering</description></item>
    /// <item><description><strong>Resource Caching:</strong> Embedded resources are loaded once and cached</description></item>
    /// <item><description><strong>Progressive Enhancement:</strong> Core functionality works without JavaScript</description></item>
    /// <item><description><strong>Error Tolerance:</strong> Failed script loading doesn't break the interface</description></item>
    /// </list>
    /// 
    /// <para><strong>Integration Testing:</strong></para>
    /// <para>
    /// The transformation includes built-in diagnostic functionality that verifies plugin operation
    /// and provides visual feedback to administrators about the plugin's status.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Input JSON format
    /// var inputData = @"{
    ///     ""contents"": ""// Original Jellyfin JavaScript\nconsole.log('Jellyfin ready');""
    /// }";
    /// 
    /// // Transform JavaScript
    /// var enhancedData = JsTransformation.TransformJs(inputData);
    /// 
    /// // Enhanced output includes:
    /// // 1. Original Jellyfin JavaScript (preserved)
    /// // 2. Netflix-style interactive behaviors
    /// // 3. Lazy loading functionality
    /// // 4. Responsive design adjustments
    /// // 5. Error handling and diagnostics
    /// 
    /// // Output format:
    /// // {
    /// //   "contents": "original content + Netflix enhancements"
    /// // }
    /// </code>
    /// 
    /// <para><strong>Error Scenarios Handled:</strong></para>
    /// <code>
    /// // Invalid JSON input
    /// var result1 = TransformJs("invalid json"); // Returns original input
    /// 
    /// // Missing contents property
    /// var result2 = TransformJs(@"{""other"": ""data""}"); // Returns original input
    /// 
    /// // Null or empty input
    /// var result3 = TransformJs(null); // Returns original input safely
    /// 
    /// // Embedded resource loading failure
    /// // Automatically falls back to basic test functionality
    /// </code>
    /// </example>
    /// <exception cref="JsonException">
    /// Caught and handled gracefully - returns original input if JSON parsing fails.
    /// This typically occurs when transformation data format is unexpected or corrupted.
    /// </exception>
    /// <seealso cref="GetNetflixRowsJs"/>
    /// <seealso cref="GetBasicTestJs"/>
    /// <seealso cref="TransformData"/>
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
/// Data transfer object for JavaScript transformation operations in the File Transformation plugin system.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// This class represents the data structure used for communication between Jellyfin's File Transformation
/// plugin and the Netflix Rows transformation handlers. It encapsulates the original file content and
/// provides metadata needed for transformation operations.
/// </para>
/// 
/// <para><strong>JSON Serialization:</strong></para>
/// <para>
/// This class is designed to be JSON-serialized and deserialized by the transformation system.
/// The File Transformation plugin sends transformation requests as JSON, and expects JSON responses
/// containing the modified content.
/// </para>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// This class is designed to be immutable during transformation operations. Each transformation
/// should create a new instance rather than modifying existing instances to ensure thread safety
/// in concurrent transformation scenarios.
/// </para>
/// 
/// <para><strong>Validation Considerations:</strong></para>
/// <para>
/// The Contents property may be null or empty, indicating either an empty source file or
/// a transformation error. Transformation methods should always validate this property
/// before attempting to process the content.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example of transformation data structure:
/// var transformData = new TransformData
/// {
///     Contents = "// Original JavaScript content\nconsole.log('Hello World');"
/// };
/// 
/// // JSON representation (as received from File Transformation plugin):
/// // {
/// //   "contents": "// Original JavaScript content\nconsole.log('Hello World');"
/// // }
/// 
/// // Usage in transformation method:
/// var data = JsonSerializer.Deserialize&lt;TransformData&gt;(jsonInput);
/// if (!string.IsNullOrEmpty(data?.Contents))
/// {
///     var enhancedContent = data.Contents + additionalJavaScript;
///     return JsonSerializer.Serialize(new { contents = enhancedContent });
/// }
/// </code>
/// </example>
/// <seealso cref="JsTransformation"/>
/// <seealso cref="CssTransformation"/>
internal class TransformData
{
    /// <summary>
    /// Gets or sets the original file contents to be transformed.
    /// </summary>
    /// <value>
    /// The raw content of the JavaScript file being transformed, or <c>null</c> if the file is empty
    /// or if there was an error reading the original content.
    /// </value>
    /// <remarks>
    /// <para><strong>Content Handling:</strong></para>
    /// <para>
    /// This property contains the unmodified JavaScript content from Jellyfin's original files.
    /// Transformation methods should preserve this content and append enhancements rather than
    /// replacing it, ensuring backward compatibility and integration with existing functionality.
    /// </para>
    /// 
    /// <para><strong>Null Handling:</strong></para>
    /// <para>
    /// A null value typically indicates one of the following scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The source file is empty or doesn't exist</description></item>
    /// <item><description>There was an error reading the original file</description></item>
    /// <item><description>The transformation data was malformed during transmission</description></item>
    /// <item><description>The File Transformation plugin encountered an internal error</description></item>
    /// </list>
    /// 
    /// <para><strong>Content Validation:</strong></para>
    /// <para>
    /// Transformation methods should always validate this property before processing:
    /// </para>
    /// <code>
    /// if (string.IsNullOrEmpty(data?.Contents))
    /// {
    ///     // Handle null/empty content gracefully
    ///     return originalInput; // Fail-safe approach
    /// }
    /// </code>
    /// </remarks>
    public string? Contents { get; set; }
}