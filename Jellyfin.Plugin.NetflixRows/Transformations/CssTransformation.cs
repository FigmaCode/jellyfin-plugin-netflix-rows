using System;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Transformations;

/// <summary>
/// CSS transformation for Netflix Rows styling.
/// </summary>
public static class CssTransformation
{
    /// <summary>
    /// Transforms CSS files to inject Netflix Rows styling.
    /// </summary>
    /// <param name="data">Transformation data containing file contents.</param>
    /// <returns>Modified file contents.</returns>
    public static string TransformCss(string data)
    {
        try
        {
            var transformData = JsonSerializer.Deserialize<TransformData>(data);
            if (transformData?.Contents == null)
            {
                return data;
            }

            var cssCode = GetNetflixRowsCss();
            
            // Inject our Netflix Rows CSS at the end of the file
            var modifiedContents = transformData.Contents + "\n" + cssCode;

            return JsonSerializer.Serialize(new { contents = modifiedContents });
        }
        catch (Exception)
        {
            return data;
        }
    }

    private static string GetNetflixRowsCss()
    {
        return @"
/* Netflix Rows Plugin - Injected CSS */

.netflix-rows-container {
    width: 100%;
    max-width: 100vw;
    overflow: hidden;
}

.netflix-rows {
    padding: 1rem;
    background: transparent;
}

/* Loading States */
.loading-indicator {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    text-align: center;
}

.loading-spinner {
    width: 3rem;
    height: 3rem;
    border: 3px solid rgba(var(--theme-primary-color-rgb, 0, 123, 255), 0.3);
    border-top: 3px solid rgba(var(--theme-primary-color-rgb, 0, 123, 255), 1);
    border-radius: 50%;
    animation: netflix-spin 1s linear infinite;
    margin-bottom: 1rem;
}

@keyframes netflix-spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.loading-placeholder {
    padding: 2rem;
    text-align: center;
    color: rgba(var(--theme-text-color-rgb, 255, 255, 255), 0.7);
}

/* Row Styling */
.netflix-row {
    margin-bottom: 2.5rem;
    position: relative;
}

.netflix-row-header {
    margin-bottom: 1rem;
    padding: 0 1rem;
}

.netflix-row-title {
    font-size: 1.4rem;
    font-weight: 600;
    color: var(--theme-text-color, #ffffff);
    margin: 0;
    line-height: 1.2;
}

.netflix-row-content {
    position: relative;
    overflow: hidden;
}

.netflix-row-scroller {
    overflow-x: auto;
    overflow-y: hidden;
    scrollbar-width: none;
    -ms-overflow-style: none;
    scroll-behavior: smooth;
}

.netflix-row-scroller::-webkit-scrollbar {
    display: none;
}

.netflix-row-items {
    display: flex;
    gap: 0.5rem;
    padding: 0 1rem;
    min-height: 200px;
}

/* Item Cards */
.netflix-item-card {
    flex: 0 0 auto;
    width: 150px;
    position: relative;
    transition: all 0.3s ease;
    cursor: pointer;
}

.netflix-item-card:hover {
    transform: scale(1.05);
    z-index: 10;
}

.netflix-item-link {
    display: block;
    text-decoration: none;
    color: inherit;
}

.netflix-item-image-container {
    position: relative;
    width: 100%;
    height: 225px;
    border-radius: 6px;
    overflow: hidden;
    background: rgba(var(--theme-card-background-rgb, 40, 40, 40), 1);
}

.netflix-item-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: opacity 0.3s ease;
}

.netflix-item-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(to top, rgba(0, 0, 0, 0.8) 0%, transparent 50%);
    opacity: 0;
    transition: opacity 0.3s ease;
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
    padding: 0.5rem;
}

.netflix-item-card:hover .netflix-item-overlay {
    opacity: 1;
}

.netflix-item-actions {
    display: flex;
    gap: 0.5rem;
    justify-content: flex-end;
}

.netflix-item-favorite {
    background: rgba(255, 255, 255, 0.9);
    border: none;
    border-radius: 50%;
    width: 2rem;
    height: 2rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;
    color: #333;
}

.netflix-item-favorite:hover {
    background: rgba(255, 255, 255, 1);
    transform: scale(1.1);
}

.netflix-item-favorite .material-icons {
    font-size: 1rem;
}

.netflix-item-info {
    padding: 0.5rem 0;
}

.netflix-item-title {
    font-size: 0.875rem;
    font-weight: 500;
    line-height: 1.3;
    margin: 0 0 0.25rem 0;
    color: var(--theme-text-color, #ffffff);
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.netflix-item-year {
    font-size: 0.75rem;
    color: rgba(var(--theme-text-color-rgb, 255, 255, 255), 0.7);
}

/* Error and Empty States */
.error, .no-items {
    padding: 2rem;
    text-align: center;
    color: rgba(var(--theme-text-color-rgb, 255, 255, 255), 0.7);
    font-style: italic;
}

.error {
    color: var(--theme-error-color, #ff4444);
}

/* Responsive Design */
@media (max-width: 768px) {
    .netflix-rows {
        padding: 0.5rem;
    }
    
    .netflix-row-header {
        padding: 0 0.5rem;
    }
    
    .netflix-row-title {
        font-size: 1.2rem;
    }
    
    .netflix-row-items {
        padding: 0 0.5rem;
        gap: 0.375rem;
    }
    
    .netflix-item-card {
        width: 120px;
    }
    
    .netflix-item-image-container {
        height: 180px;
    }
}

@media (max-width: 480px) {
    .netflix-item-card {
        width: 100px;
    }
    
    .netflix-item-image-container {
        height: 150px;
    }
    
    .netflix-item-title {
        font-size: 0.8rem;
    }
}

/* Large screens */
@media (min-width: 1200px) {
    .netflix-item-card {
        width: 180px;
    }
    
    .netflix-item-image-container {
        height: 270px;
    }
}

@media (min-width: 1600px) {
    .netflix-item-card {
        width: 200px;
    }
    
    .netflix-item-image-container {
        height: 300px;
    }
}

/* Theme Integration */
.theme-dark .netflix-rows,
[data-theme='dark'] .netflix-rows {
    /* Dark theme is default */
}

.theme-light .netflix-rows,
[data-theme='light'] .netflix-rows {
    .netflix-item-image-container {
        background: rgba(240, 240, 240, 1);
    }
    
    .netflix-item-overlay {
        background: linear-gradient(to top, rgba(0, 0, 0, 0.6) 0%, transparent 50%);
    }
    
    .loading-spinner {
        border-color: rgba(0, 0, 0, 0.3);
        border-top-color: var(--theme-primary-color, #007bff);
    }
}

/* Custom scrollbar for desktop */
@media (hover: hover) and (pointer: fine) {
    .netflix-row-scroller {
        scrollbar-width: thin;
        scrollbar-color: rgba(var(--theme-primary-color-rgb, 0, 123, 255), 0.5) transparent;
    }
    
    .netflix-row-scroller::-webkit-scrollbar {
        display: block;
        height: 4px;
    }
    
    .netflix-row-scroller::-webkit-scrollbar-track {
        background: transparent;
    }
    
    .netflix-row-scroller::-webkit-scrollbar-thumb {
        background: rgba(var(--theme-primary-color-rgb, 0, 123, 255), 0.5);
        border-radius: 2px;
    }
    
    .netflix-row-scroller::-webkit-scrollbar-thumb:hover {
        background: rgba(var(--theme-primary-color-rgb, 0, 123, 255), 0.8);
    }
}

/* Animation for smooth appearance */
.netflix-row {
    opacity: 0;
    animation: netflix-fade-in 0.5s ease forwards;
}

.netflix-row:nth-child(1) { animation-delay: 0.1s; }
.netflix-row:nth-child(2) { animation-delay: 0.2s; }
.netflix-row:nth-child(3) { animation-delay: 0.3s; }
.netflix-row:nth-child(4) { animation-delay: 0.4s; }
.netflix-row:nth-child(5) { animation-delay: 0.5s; }
.netflix-row:nth-child(6) { animation-delay: 0.6s; }
.netflix-row:nth-child(7) { animation-delay: 0.7s; }
.netflix-row:nth-child(8) { animation-delay: 0.8s; }

@keyframes netflix-fade-in {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* Integration with existing Jellyfin themes */
.netflix-rows * {
    box-sizing: border-box;
}

/* High contrast mode support */
@media (prefers-contrast: high) {
    .netflix-item-overlay {
        background: linear-gradient(to top, rgba(0, 0, 0, 0.9) 0%, transparent 60%);
    }
    
    .netflix-item-favorite {
        background: rgba(255, 255, 255, 1);
        border: 2px solid var(--theme-primary-color, #007bff);
    }
}

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
    .netflix-item-card,
    .netflix-item-image,
    .netflix-item-overlay,
    .netflix-item-favorite {
        transition: none;
    }
    
    .netflix-row {
        animation: none;
        opacity: 1;
    }
    
    .loading-spinner {
        animation: none;
    }
    
    .netflix-row-scroller {
        scroll-behavior: auto;
    }
}
";
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