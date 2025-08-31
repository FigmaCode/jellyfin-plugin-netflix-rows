using System;
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
            var modifiedContents = transformData.Contents + "\n" + jsCode;

            return JsonSerializer.Serialize(new { contents = modifiedContents });
        }
        catch (Exception)
        {
            return data;
        }
    }

    private static string GetNetflixRowsJs()
    {
        return @"
// Netflix Rows Plugin - Injected JavaScript
(function() {
    'use strict';

    // Configuration
    let netflixConfig = null;
    
    // Load configuration
    async function loadConfig() {
        try {
            const response = await fetch('/NetflixRows/Config');
            if (response.ok) {
                netflixConfig = await response.json();
            }
        } catch (e) {
            console.warn('Netflix Rows: Could not load configuration');
        }
    }

    // Replace heart icons with plus icons
    function replaceHeartWithPlus() {
        if (!netflixConfig?.replaceHeartWithPlus) return;
        
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        // Find heart icons and replace with plus
                        const heartIcons = node.querySelectorAll?.('.material-icons:contains(\"favorite\")') || [];
                        heartIcons.forEach((icon) => {
                            if (icon.textContent === 'favorite') {
                                icon.textContent = 'add';
                                icon.title = 'Zu meiner Liste hinzufügen';
                            } else if (icon.textContent === 'favorite_border') {
                                icon.textContent = 'add_circle_outline';
                                icon.title = 'Zu meiner Liste hinzufügen';
                            }
                        });
                    }
                });
            });
        });
        
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Create Netflix-style rows
    function createNetflixRows() {
        const homeView = document.querySelector('.homeView, .view[data-type=\"home\"]');
        if (!homeView) return;

        const rowsContainer = document.createElement('div');
        rowsContainer.className = 'netflix-rows-container';
        rowsContainer.innerHTML = `
            <div class='netflix-rows' id='netflix-rows'>
                <div class='loading-indicator'>
                    <div class='loading-spinner'></div>
                    <p>Lädt Netflix Rows...</p>
                </div>
            </div>
        `;
        
        // Replace or insert rows
        const existingContent = homeView.querySelector('.sections, .homePageContent');
        if (existingContent) {
            existingContent.parentNode.replaceChild(rowsContainer, existingContent);
        } else {
            homeView.appendChild(rowsContainer);
        }
        
        loadNetflixRows();
    }

    // Load and display Netflix rows
    async function loadNetflixRows() {
        if (!netflixConfig) {
            await loadConfig();
        }
        
        const rowsContainer = document.getElementById('netflix-rows');
        if (!rowsContainer) return;

        try {
            const userId = getCurrentUserId();
            if (!userId) throw new Error('No user ID found');

            rowsContainer.innerHTML = '';
            
            // Load enabled rows
            const rows = [];
            
            if (netflixConfig.enableMyList) {
                rows.push({
                    id: 'my-list',
                    title: 'Meine Liste',
                    endpoint: `/NetflixRows/MyList?userId=${userId}&limit=${netflixConfig.maxItemsPerRow}`,
                    priority: 0
                });
            }
            
            if (netflixConfig.enableRecentlyAdded) {
                rows.push({
                    id: 'recently-added',
                    title: 'Kürzlich hinzugefügt',
                    endpoint: `/NetflixRows/RecentlyAdded?userId=${userId}&limit=${netflixConfig.maxItemsPerRow}`,
                    priority: 1
                });
            }
            
            if (netflixConfig.enableRandomPicks) {
                rows.push({
                    id: 'random-picks',
                    title: 'Zufallsauswahl',
                    endpoint: `/NetflixRows/RandomPicks?userId=${userId}&limit=${netflixConfig.maxItemsPerRow}`,
                    priority: 2
                });
            }
            
            if (netflixConfig.enableLongNotWatched) {
                rows.push({
                    id: 'long-not-watched',
                    title: 'Lange nicht gesehen',
                    endpoint: `/NetflixRows/LongNotWatched?userId=${userId}&limit=${netflixConfig.maxItemsPerRow}`,
                    priority: 3
                });
            }
            
            // Add genre rows
            if (netflixConfig.enabledGenres) {
                netflixConfig.enabledGenres.forEach((genre, index) => {
                    const displayName = netflixConfig.genreDisplayNames[genre] || genre;
                    rows.push({
                        id: `genre-${genre.toLowerCase()}`,
                        title: displayName,
                        endpoint: `/NetflixRows/Genre/${genre}?userId=${userId}&limit=${netflixConfig.maxItemsPerRow}`,
                        priority: 4 + index
                    });
                });
            }
            
            // Sort rows by priority (unless random order is enabled)
            if (!netflixConfig.randomRowOrder) {
                rows.sort((a, b) => a.priority - b.priority);
            } else {
                rows.sort(() => Math.random() - 0.5);
            }
            
            // Limit number of rows
            const limitedRows = rows.slice(0, netflixConfig.maxRows || 8);
            
            // Create row elements
            for (const row of limitedRows) {
                const rowElement = createRowElement(row);
                rowsContainer.appendChild(rowElement);
                
                if (netflixConfig.lazyLoadRows) {
                    observeRowForLazyLoading(rowElement, row);
                } else {
                    loadRowContent(rowElement, row);
                }
            }
            
        } catch (error) {
            console.error('Netflix Rows: Error loading rows:', error);
            rowsContainer.innerHTML = '<p class=\"error\">Fehler beim Laden der Netflix Rows</p>';
        }
    }
    
    // Create row element
    function createRowElement(row) {
        const rowDiv = document.createElement('div');
        rowDiv.className = 'netflix-row';
        rowDiv.setAttribute('data-row-id', row.id);
        rowDiv.innerHTML = `
            <div class='netflix-row-header'>
                <h2 class='netflix-row-title'>${row.title}</h2>
            </div>
            <div class='netflix-row-content'>
                <div class='netflix-row-scroller'>
                    <div class='loading-placeholder'>Lädt...</div>
                </div>
            </div>
        `;
        return rowDiv;
    }
    
    // Lazy loading observer
    function observeRowForLazyLoading(rowElement, rowData) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    loadRowContent(rowElement, rowData);
                    observer.unobserve(entry.target);
                }
            });
        }, {
            rootMargin: '100px'
        });
        
        observer.observe(rowElement);
    }
    
    // Load row content
    async function loadRowContent(rowElement, rowData) {
        const scrollerElement = rowElement.querySelector('.netflix-row-scroller');
        if (!scrollerElement) return;
        
        try {
            const response = await fetch(rowData.endpoint);
            if (!response.ok) throw new Error('Failed to load row data');
            
            const data = await response.json();
            
            if (!data.Items || data.Items.length === 0) {
                scrollerElement.innerHTML = '<p class=\"no-items\">Keine Inhalte gefunden</p>';
                return;
            }
            
            // Create item cards
            const itemsHtml = data.Items.map(item => createItemCard(item)).join('');
            scrollerElement.innerHTML = `
                <div class='netflix-row-items'>
                    ${itemsHtml}
                </div>
            `;
            
            // Add scroll functionality
            addRowScrollFunctionality(rowElement);
            
        } catch (error) {
            console.error('Netflix Rows: Error loading row content:', error);
            scrollerElement.innerHTML = '<p class=\"error\">Fehler beim Laden</p>';
        }
    }
    
    // Create item card
    function createItemCard(item) {
        const imageUrl = getItemImageUrl(item);
        const itemUrl = getItemUrl(item);
        
        return `
            <div class='netflix-item-card' data-item-id='${item.Id}'>
                <a href='${itemUrl}' class='netflix-item-link'>
                    <div class='netflix-item-image-container'>
                        <img class='netflix-item-image' src='${imageUrl}' alt='${item.Name}' loading='lazy'>
                        <div class='netflix-item-overlay'>
                            <div class='netflix-item-actions'>
                                <button class='netflix-item-favorite' data-item-id='${item.Id}' title='${item.UserData?.IsFavorite ? 'Aus meiner Liste entfernen' : 'Zu meiner Liste hinzufügen'}'>
                                    <i class='material-icons'>${item.UserData?.IsFavorite ? 'remove' : 'add'}</i>
                                </button>
                            </div>
                        </div>
                    </div>
                    <div class='netflix-item-info'>
                        <h3 class='netflix-item-title'>${item.Name}</h3>
                        ${item.ProductionYear ? `<span class='netflix-item-year'>${item.ProductionYear}</span>` : ''}
                    </div>
                </a>
            </div>
        `;
    }
    
    // Get item image URL
    function getItemImageUrl(item) {
        const baseUrl = getApiBaseUrl();
        if (item.ImageTags?.Primary) {
            return `${baseUrl}/Items/${item.Id}/Images/Primary?width=300&height=450&quality=90`;
        } else if (item.ImageTags?.Thumb) {
            return `${baseUrl}/Items/${item.Id}/Images/Thumb?width=300&height=450&quality=90`;
        }
        return '/web/assets/img/icon-transparent.png';
    }
    
    // Get item URL
    function getItemUrl(item) {
        if (item.Type === 'Movie') {
            return `#!/details?id=${item.Id}`;
        } else if (item.Type === 'Series') {
            return `#!/details?id=${item.Id}`;
        }
        return `#!/details?id=${item.Id}`;
    }
    
    // Add row scroll functionality
    function addRowScrollFunctionality(rowElement) {
        const scroller = rowElement.querySelector('.netflix-row-items');
        if (!scroller) return;
        
        let isScrolling = false;
        
        scroller.addEventListener('wheel', (e) => {
            if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;
            
            e.preventDefault();
            const scrollAmount = e.deltaY > 0 ? 300 : -300;
            scroller.scrollBy({ left: scrollAmount, behavior: 'smooth' });
        });
        
        // Add favorite functionality
        scroller.addEventListener('click', (e) => {
            if (e.target.closest('.netflix-item-favorite')) {
                e.preventDefault();
                const button = e.target.closest('.netflix-item-favorite');
                const itemId = button.getAttribute('data-item-id');
                toggleFavorite(itemId, button);
            }
        });
    }
    
    // Toggle favorite status
    async function toggleFavorite(itemId, buttonElement) {
        try {
            const userId = getCurrentUserId();
            const response = await fetch(`/Users/${userId}/FavoriteItems/${itemId}`, {
                method: 'POST'
            });
            
            if (response.ok) {
                const icon = buttonElement.querySelector('.material-icons');
                const isFavorite = icon.textContent === 'remove';
                
                if (isFavorite) {
                    icon.textContent = 'add';
                    buttonElement.title = 'Zu meiner Liste hinzufügen';
                } else {
                    icon.textContent = 'remove';
                    buttonElement.title = 'Aus meiner Liste entfernen';
                }
            }
        } catch (error) {
            console.error('Netflix Rows: Error toggling favorite:', error);
        }
    }
    
    // Utility functions
    function getCurrentUserId() {
        return window.ApiClient?.getCurrentUserId?.() || 
               window.Dashboard?.getCurrentUserId?.() ||
               localStorage.getItem('userId');
    }
    
    function getApiBaseUrl() {
        return window.ApiClient?.serverAddress?.() || 
               window.ApiClient?.baseUrl || 
               window.location.origin;
    }
    
    // Initialize when DOM is ready
    function init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', init);
            return;
        }
        
        loadConfig().then(() => {
            replaceHeartWithPlus();
            
            // Watch for navigation changes
            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    if (mutation.addedNodes) {
                        mutation.addedNodes.forEach((node) => {
                            if (node.nodeType === Node.ELEMENT_NODE && 
                                (node.classList?.contains('homeView') || 
                                 node.querySelector?.('.homeView'))) {
                                setTimeout(createNetflixRows, 100);
                            }
                        });
                    }
                });
            });
            
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
            
            // Initial check for home view
            if (document.querySelector('.homeView, .view[data-type=\"home\"]')) {
                setTimeout(createNetflixRows, 100);
            }
        });
    }
    
    init();
})();
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