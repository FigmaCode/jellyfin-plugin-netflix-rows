using System;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Frontend;

/// <summary>
/// Injects Netflix-style CSS.
/// </summary>
public class CSSTransformation
{
    /// <summary>
    /// Inject Netflix CSS styles.
    /// </summary>
    /// <param name="input">Input JSON with CSS contents.</param>
    /// <returns>Modified CSS.</returns>
    public static string InjectNetflixCSS(string input)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TransformationData>(input);
            if (data?.Contents == null) return input;

            var netflixCSS = @"
/* Netflix Rows Styles */
.netflix-rows-container {
    margin: 20px 0;
    width: 100%;
}

.netflix-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 40px;
    color: #ffffff;
}

.loading-spinner {
    width: 24px;
    height: 24px;
    border: 2px solid #333;
    border-top: 2px solid #00a4dc;
    border-radius: 50%;
    animation: netflix-spin 1s linear infinite;
    margin-right: 10px;
}

@keyframes netflix-spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.netflix-row {
    margin: 30px 0;
    position: relative;
}

.netflix-row-header {
    display: flex;
    align-items: center;
    margin-bottom: 15px;
    padding: 0 60px;
}

.netflix-row-title {
    font-size: 1.4em;
    font-weight: 700;
    color: #ffffff;
    margin: 0;
}

.netflix-row-container {
    position: relative;
    overflow: hidden;
}

.netflix-row-scroller {
    display: flex;
    gap: 4px;
    padding: 0 60px;
    scroll-behavior: smooth;
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
}

.netflix-row-scroller::-webkit-scrollbar {
    display: none;
}

.netflix-card {
    min-width: 200px;
    width: 200px;
    position: relative;
    cursor: pointer;
    transition: transform 0.3s ease;
}

.netflix-card:hover {
    transform: scale(1.05);
    z-index: 10;
}

.netflix-card-image {
    width: 100%;
    height: 113px;
    object-fit: cover;
    border-radius: 4px;
    background: #333;
}

.netflix-card-title {
    color: #ffffff;
    font-size: 0.9em;
    margin-top: 8px;
    font-weight: 500;
    text-overflow: ellipsis;
    overflow: hidden;
    white-space: nowrap;
}

.netflix-scroll-button {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    background: rgba(0, 0, 0, 0.7);
    border: none;
    color: white;
    width: 50px;
    height: 100px;
    cursor: pointer;
    z-index: 5;
    font-size: 20px;
    transition: background 0.3s ease;
}

.netflix-scroll-button:hover {
    background: rgba(0, 0, 0, 0.9);
}

.netflix-scroll-left {
    left: 5px;
}

.netflix-scroll-right {
    right: 5px;
}

.netflix-my-list-button {
    background: rgba(255, 255, 255, 0.2);
    border: 1px solid rgba(255, 255, 255, 0.5);
    color: white;
    padding: 4px 8px;
    border-radius: 2px;
    font-size: 12px;
    cursor: pointer;
    position: absolute;
    top: 8px;
    right: 8px;
    transition: all 0.3s ease;
}

.netflix-my-list-button:hover {
    background: rgba(255, 255, 255, 0.4);
}

.netflix-my-list-button.added {
    background: #46d369;
    border-color: #46d369;
}

@media (max-width: 768px) {
    .netflix-row-header,
    .netflix-row-scroller {
        padding: 0 20px;
    }
    
    .netflix-card {
        min-width: 150px;
        width: 150px;
    }
    
    .netflix-card-image {
        height: 85px;
    }
}";

            var content = data.Contents + netflixCSS;
            return JsonSerializer.Serialize(new TransformationData { Contents = content });
        }
        catch (Exception)
        {
            return input;
        }
    }
}