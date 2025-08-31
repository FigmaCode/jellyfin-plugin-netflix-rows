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
            // Fall back to inline JavaScript if resource loading fails
        }
        
        // Fallback inline JavaScript - simplified to avoid escaping issues
        return GetInlineJs();
    }

    private static string GetInlineJs()
    {
        return @"
(function() {
    console.log('Netflix Rows Plugin loaded');
    
    let netflixConfig = null;
    
    async function loadConfig() {
        try {
            const response = await fetch('/NetflixRows/Config');
            if (response.ok) {
                netflixConfig = await response.json();
                console.log('Netflix Rows config loaded:', netflixConfig);
            }
        } catch (e) {
            console.warn('Netflix Rows: Could not load configuration', e);
        }
    }

    function createNetflixRows() {
        console.log('Creating Netflix Rows...');
        const homeView = document.querySelector('.homeView');
        if (!homeView) {
            console.log('No home view found');
            return;
        }

        const existingRows = document.getElementById('netflix-rows');
        if (existingRows) {
            console.log('Netflix rows already exist');
            return;
        }

        const rowsContainer = document.createElement('div');
        rowsContainer.id = 'netflix-rows-container';
        rowsContainer.className = 'netflix-rows-container';
        
        const rowsDiv = document.createElement('div');
        rowsDiv.id = 'netflix-rows';
        rowsDiv.className = 'netflix-rows';
        rowsDiv.innerHTML = '<div class=\"loading-indicator\"><p>Loading Netflix Rows...</p></div>';
        
        rowsContainer.appendChild(rowsDiv);
        
        const existingContent = homeView.querySelector('.sections, .homePageContent');
        if (existingContent) {
            existingContent.parentNode.insertBefore(rowsContainer, existingContent);
        } else {
            homeView.appendChild(rowsContainer);
        }
        
        loadNetflixRows();
    }

    async function loadNetflixRows() {
        if (!netflixConfig) {
            await loadConfig();
        }
        
        const rowsContainer = document.getElementById('netflix-rows');
        if (!rowsContainer || !netflixConfig) {
            console.log('No rows container or config');
            return;
        }

        try {
            const userId = getCurrentUserId();
            if (!userId) {
                throw new Error('No user ID found');
            }

            console.log('Loading rows for user:', userId);
            rowsContainer.innerHTML = '';
            
            const rows = [];
            
            if (netflixConfig.enableMyList) {
                rows.push({
                    id: 'my-list',
                    title: 'My List',
                    endpoint: '/NetflixRows/MyList?userId=' + userId + '&limit=25'
                });
            }
            
            if (netflixConfig.enableRecentlyAdded) {
                rows.push({
                    id: 'recently-added',
                    title: 'Recently Added',
                    endpoint: '/NetflixRows/RecentlyAdded?userId=' + userId + '&limit=25'
                });
            }

            console.log('Creating', rows.length, 'rows');
            
            for (let i = 0; i < rows.length; i++) {
                const row = rows[i];
                const rowElement = createRowElement(row);
                rowsContainer.appendChild(rowElement);
                loadRowContent(rowElement, row);
            }
            
        } catch (error) {
            console.error('Netflix Rows: Error loading rows:', error);
            rowsContainer.innerHTML = '<p class=\"error\">Error loading Netflix Rows</p>';
        }
    }
    
    function createRowElement(row) {
        const rowDiv = document.createElement('div');
        rowDiv.className = 'netflix-row';
        rowDiv.setAttribute('data-row-id', row.id);
        
        const header = document.createElement('div');
        header.className = 'netflix-row-header';
        
        const title = document.createElement('h2');
        title.className = 'netflix-row-title';
        title.textContent = row.title;
        
        const content = document.createElement('div');
        content.className = 'netflix-row-content';
        
        const scroller = document.createElement('div');
        scroller.className = 'netflix-row-scroller';
        scroller.innerHTML = '<div class=\"loading-placeholder\">Loading...</div>';
        
        header.appendChild(title);
        content.appendChild(scroller);
        rowDiv.appendChild(header);
        rowDiv.appendChild(content);
        
        return rowDiv;
    }
    
    async function loadRowContent(rowElement, rowData) {
        const scrollerElement = rowElement.querySelector('.netflix-row-scroller');
        if (!scrollerElement) return;
        
        try {
            console.log('Loading content for row:', rowData.endpoint);
            const response = await fetch(rowData.endpoint);
            if (!response.ok) {
                throw new Error('Failed to load row data: ' + response.status);
            }
            
            const data = await response.json();
            console.log('Row data loaded:', data);
            
            if (!data.Items || data.Items.length === 0) {
                scrollerElement.innerHTML = '<p class=\"no-items\">No content found</p>';
                return;
            }
            
            const itemsContainer = document.createElement('div');
            itemsContainer.className = 'netflix-row-items';
            
            for (let i = 0; i < data.Items.length; i++) {
                const item = data.Items[i];
                const itemCard = createItemCard(item);
                itemsContainer.appendChild(itemCard);
            }
            
            scrollerElement.innerHTML = '';
            scrollerElement.appendChild(itemsContainer);
            
        } catch (error) {
            console.error('Netflix Rows: Error loading row content:', error);
            scrollerElement.innerHTML = '<p class=\"error\">Error loading content</p>';
        }
    }
    
    function createItemCard(item) {
        const card = document.createElement('div');
        card.className = 'netflix-item-card';
        card.setAttribute('data-item-id', item.Id);
        
        const link = document.createElement('a');
        link.href = '#!/details?id=' + item.Id;
        link.className = 'netflix-item-link';
        
        const imageContainer = document.createElement('div');
        imageContainer.className = 'netflix-item-image-container';
        
        const img = document.createElement('img');
        img.className = 'netflix-item-image';
        img.src = getItemImageUrl(item);
        img.alt = item.Name || '';
        img.loading = 'lazy';
        
        const info = document.createElement('div');
        info.className = 'netflix-item-info';
        
        const title = document.createElement('h3');
        title.className = 'netflix-item-title';
        title.textContent = item.Name || '';
        
        imageContainer.appendChild(img);
        info.appendChild(title);
        link.appendChild(imageContainer);
        link.appendChild(info);
        card.appendChild(link);
        
        return card;
    }
    
    function getItemImageUrl(item) {
        const baseUrl = getApiBaseUrl();
        if (item.ImageTags && item.ImageTags.Primary) {
            return baseUrl + '/Items/' + item.Id + '/Images/Primary?width=300&height=450&quality=90';
        }
        return '/web/assets/img/icon-transparent.png';
    }
    
    function getCurrentUserId() {
        if (window.ApiClient && window.ApiClient.getCurrentUserId) {
            return window.ApiClient.getCurrentUserId();
        }
        if (window.Dashboard && window.Dashboard.getCurrentUserId) {
            return window.Dashboard.getCurrentUserId();
        }
        return localStorage.getItem('userId');
    }
    
    function getApiBaseUrl() {
        if (window.ApiClient && window.ApiClient.serverAddress) {
            return window.ApiClient.serverAddress();
        }
        if (window.ApiClient && window.ApiClient.baseUrl) {
            return window.ApiClient.baseUrl;
        }
        return window.location.origin;
    }
    
    function init() {
        console.log('Netflix Rows init');
        
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', init);
            return;
        }
        
        loadConfig().then(function() {
            console.log('Config loaded, setting up observers');
            
            const observer = new MutationObserver(function(mutations) {
                mutations.forEach(function(mutation) {
                    if (mutation.addedNodes) {
                        mutation.addedNodes.forEach(function(node) {
                            if (node.nodeType === Node.ELEMENT_NODE) {
                                if (node.classList && node.classList.contains('homeView')) {
                                    setTimeout(createNetflixRows, 100);
                                } else if (node.querySelector && node.querySelector('.homeView')) {
                                    setTimeout(createNetflixRows, 100);
                                }
                            }
                        });
                    }
                });
            });
            
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
            
            if (document.querySelector('.homeView')) {
                setTimeout(createNetflixRows, 100);
            }
        });
    }
    
    init();
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