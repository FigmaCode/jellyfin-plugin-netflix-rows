using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetflixRows.Logging;

/// <summary>
/// Custom logger implementation for Netflix Rows plugin that writes to a dedicated log file.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// This logger provides isolated logging for the Netflix Rows plugin, making it easier to
/// debug configuration issues, API calls, and plugin behavior without searching through
/// the entire Jellyfin log.
/// </para>
/// 
/// <para><strong>Log File Location:</strong></para>
/// <para>
/// Logs are written to: <c>[JellyfinDataDirectory]/logs/netflixrows.log</c>
/// </para>
/// 
/// <para><strong>Features:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Timestamped Entries:</strong> Each log entry includes precise timestamps</description></item>
/// <item><description><strong>Log Level Filtering:</strong> Supports all standard log levels</description></item>
/// <item><description><strong>Thread-Safe:</strong> Safe for concurrent use across plugin components</description></item>
/// <item><description><strong>Auto-Rotation:</strong> Prevents log file from growing too large</description></item>
/// </list>
/// 
/// <para><strong>Usage Examples:</strong></para>
/// <code>
/// // In controllers or services
/// PluginLogger.LogInfo("Configuration loaded successfully");
/// PluginLogger.LogDebug("Processing MyList request for user {0}", userId);
/// PluginLogger.LogError("Failed to save configuration: {0}", ex.Message);
/// 
/// // For debugging configuration persistence
/// PluginLogger.LogDebug("CONFIG_SAVE: autoRemoveWatched = {0}", config.AutoRemoveWatchedFromMyList);
/// PluginLogger.LogDebug("CONFIG_LOAD: autoRemoveWatched = {0}", loadedConfig.AutoRemoveWatchedFromMyList);
/// </code>
/// </remarks>
public static class PluginLogger
{
    private static readonly object _lockObject = new object();
    private static readonly string _logDirectory = GetLogDirectory();
    private static readonly string _logFilePath = Path.Combine(_logDirectory, "netflixrows.log");
    private const int MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10MB
    
    /// <summary>
    /// Gets the log directory path, ensuring it exists.
    /// </summary>
    /// <returns>The log directory path.</returns>
    private static string GetLogDirectory()
    {
        try
        {
            // Try to get Jellyfin's data directory
            var jellyfinDataDir = Environment.GetEnvironmentVariable("JELLYFIN_DATA_DIR") 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jellyfin");
            
            var logDir = Path.Combine(jellyfinDataDir, "logs");
            
            // Ensure log directory exists
            Directory.CreateDirectory(logDir);
            
            return logDir;
        }
        catch (UnauthorizedAccessException)
        {
            // Fallback to current directory if Jellyfin data dir is not accessible
            var fallbackDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            try
            {
                Directory.CreateDirectory(fallbackDir);
                return fallbackDir;
            }
            catch (UnauthorizedAccessException)
            {
                return Directory.GetCurrentDirectory();
            }
        }
        catch (DirectoryNotFoundException)
        {
            return Directory.GetCurrentDirectory();
        }
        catch (IOException)
        {
            return Directory.GetCurrentDirectory();
        }
    }
    
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogInfo(string message, params object[]? args)
    {
        WriteLog(LogLevel.Information, message, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogDebug(string message, params object[]? args)
    {
        WriteLog(LogLevel.Debug, message, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogWarning(string message, params object[]? args)
    {
        WriteLog(LogLevel.Warning, message, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogError(string message, params object[]? args)
    {
        WriteLog(LogLevel.Error, message, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Logs a critical error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogCritical(string message, params object[]? args)
    {
        WriteLog(LogLevel.Critical, message, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Logs configuration-related debug information with a specific prefix for easy filtering.
    /// </summary>
    /// <param name="operation">The configuration operation (SAVE, LOAD, UPDATE, etc.).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogConfig(string operation, string message, params object[]? args)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var formattedMessage = $"CONFIG_{operation.ToUpper(CultureInfo.InvariantCulture)}: {message}";
        WriteLog(LogLevel.Debug, formattedMessage, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Logs API-related debug information with a specific prefix for easy filtering.
    /// </summary>
    /// <param name="endpoint">The API endpoint being called.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    public static void LogApi(string endpoint, string message, params object[]? args)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var formattedMessage = $"API_{endpoint.ToUpper(CultureInfo.InvariantCulture)}: {message}";
        WriteLog(LogLevel.Debug, formattedMessage, args ?? Array.Empty<object>());
    }
    
    /// <summary>
    /// Core logging method that writes formatted log entries to the file.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional format arguments.</param>
    private static void WriteLog(LogLevel level, string message, object[] args)
    {
        try
        {
            lock (_lockObject)
            {
                // Check if log rotation is needed
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length > MaxLogFileSizeBytes)
                    {
                        RotateLogFile();
                    }
                }
                
                // Format the message
                var formattedMessage = args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, message, args) : message;
                
                // Create log entry
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var logEntry = $"[{timestamp}] [{level.ToString().ToUpper(CultureInfo.InvariantCulture)}] [NetflixRows] {formattedMessage}";
                
                // Write to file
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Silent failure - we don't want logging failures to crash the plugin
        }
        catch (IOException)
        {
            // Silent failure - we don't want logging failures to crash the plugin
        }
        catch (ArgumentException)
        {
            // Silent failure - we don't want logging failures to crash the plugin
        }
    }
    
    /// <summary>
    /// Rotates the log file when it becomes too large.
    /// </summary>
    private static void RotateLogFile()
    {
        try
        {
            var backupPath = Path.ChangeExtension(_logFilePath, ".old.log");
            
            // Remove old backup if it exists
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            
            // Move current log to backup
            File.Move(_logFilePath, backupPath);
            
            // Log rotation info to new file
            LogInfo("Log file rotated. Previous log backed up to: " + backupPath);
        }
        catch (UnauthorizedAccessException)
        {
            // If rotation fails, try to truncate the file instead
            TruncateLogFile();
        }
        catch (IOException)
        {
            // If rotation fails, try to truncate the file instead
            TruncateLogFile();
        }
        catch (ArgumentException)
        {
            // If rotation fails, try to truncate the file instead
            TruncateLogFile();
        }
    }
    
    /// <summary>
    /// Truncates the log file when rotation fails.
    /// </summary>
    private static void TruncateLogFile()
    {
        try
        {
            File.WriteAllText(_logFilePath, "");
            LogWarning("Log file truncated due to rotation failure");
        }
        catch (UnauthorizedAccessException)
        {
            // Silent failure
        }
        catch (IOException)
        {
            // Silent failure
        }
        catch (ArgumentException)
        {
            // Silent failure
        }
    }
    
    /// <summary>
    /// Gets the current log file path for diagnostic purposes.
    /// </summary>
    public static string LogFilePath => _logFilePath;
    
    /// <summary>
    /// Clears the current log file.
    /// </summary>
    public static void ClearLog()
    {
        try
        {
            lock (_lockObject)
            {
                File.WriteAllText(_logFilePath, "");
                LogInfo("Log file cleared manually");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            LogError("Failed to clear log file: {0}", ex.Message);
        }
        catch (IOException ex)
        {
            LogError("Failed to clear log file: {0}", ex.Message);
        }
        catch (ArgumentException ex)
        {
            LogError("Failed to clear log file: {0}", ex.Message);
        }
    }
}