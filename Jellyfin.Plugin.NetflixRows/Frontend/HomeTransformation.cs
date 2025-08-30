using System;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Frontend;

/// <summary>
/// Transforms the index.html page to inject Netflix rows with theme compatibility.
/// </summary>
public class HomeTransformation
{
    /// <summary>
    /// Transform index.html content.
    /// </summary>
    /// <param name="input">Input JSON with contents.</param>
    /// <returns>Modified content.</returns>
    public static string TransformIndex(string input)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TransformationData>(input);
            if (data?.Contents == null) return input;

            var content = data.Contents;

            // Inject CSS directly into <head> with theme compatibility
            var headEndIndex = content.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headEndIndex >= 0)
            {
                var netflixCSS = @"
<style id=""netflix-rows-styles"">
/* Netflix Rows Styles - Theme Compatible */
.netflix-rows-container {
    margin: 2em 0;
    width: 100%;
    background: transparent;
    position: relative;
    z-index: 1;
}

.netflix-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2.5rem;
    color: var(--theme-text-color, #ffffff);
    font-family: inherit;
}

.loading-spinner {
    width: 24px;
    height: 24px;
    border: 2px solid var(--theme-border-color, #333333);
    border-top: 2px solid var(--theme-accent-color, #00a4dc);
    border-radius: 50%;
    animation: netflix-spin 1s linear infinite;
    margin-right: 0.75rem;
}

@keyframes netflix-spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.netflix-row {
    margin: 2rem 0;
    position: relative;
    font-family: inherit;
}

.netflix-row-header {
    display: flex;
    align-items: center;
    margin-bottom: 1rem;
    padding: 0 4rem;
}

.netflix-row-title {
    font-size: 1.4em;
    font-weight: 600;
    color: var(--theme-text-color, #ffffff);
    margin: 0;
    font-family: inherit;
    letter-spacing: inherit;
}

.netflix-row-container {
    position: relative;
    overflow: hidden;
}

.netflix-row-scroller {
    display: flex;
    gap: 0.5rem;
    padding: 0 4rem;
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
    flex-shrink: 0;
    position: relative;
    cursor: pointer;
    transition: transform 0.3s ease, box-shadow 0.3s ease;
    border-radius: var(--rounding, 4px);
    overflow: hidden;
}

.netflix-card:hover {
    transform: scale(1.05);
    z-index: 10;
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.6);
}

.netflix-card-image {
    width: 100%;
    height: 113px;
    object-fit: cover;
    border-radius: var(--rounding, 4px);
    background-color: var(--theme-background-color-secondary, #333333);
    display: block;
}

.netflix-card-title {
    color: var(--theme-text-color, #ffffff);
    font-size: 0.9em;
    margin-top: 0.5rem;
    font-weight: 500;
    font-family: inherit;
    text-overflow: ellipsis;
    overflow: hidden;
    white-space: nowrap;
    line-height: 1.2;
}

.netflix-scroll-button {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    background: rgba(0, 0, 0, 0.7);
    border: 1px solid var(--theme-border-color, transparent);
    color: var(--theme-text-color, #ffffff);
    width: 50px;
    height: 100px;
    cursor: pointer;
    z-index: 5;
    font-size: 20px;
    font-family: inherit;
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: var(--rounding, 4px);
}

.netflix-scroll-button:hover {
    background: rgba(0, 0, 0, 0.9);
    border-color: var(--theme-accent-color, #00a4dc);
}

.netflix-scroll-button:focus {
    outline: 2px solid var(--theme-accent-color, #00a4dc);
    outline-offset: 2px;
}

.netflix-scroll-left {
    left: 0.5rem;
}

.netflix-scroll-right {
    right: 0.5rem;
}

.netflix-my-list-button {
    background: rgba(0, 0, 0, 0.7);
    border: 1px solid var(--theme-border-color, rgba(255, 255, 255, 0.5));
    color: var(--theme-text-color, #ffffff);
    padding: 0.25rem 0.5rem;
    border-radius: var(--rounding, 2px);
    font-size: 12px;
    font-family: inherit;
    cursor: pointer;
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    transition: all 0.3s ease;
    z-index: 2;
    min-width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.netflix-my-list-button:hover {
    background: rgba(0, 0, 0, 0.9);
    border-color: var(--theme-accent-color, #00a4dc);
    transform: scale(1.1);
}

.netflix-my-list-button:focus {
    outline: 2px solid var(--theme-accent-color, #00a4dc);
    outline-offset: 2px;
}

.netflix-my-list-button.added {
    background: var(--theme-accent-color, #46d369);
    border-color: var(--theme-accent-color, #46d369);
    color: var(--theme-background-color, #000000);
}

/* Mobile responsiveness */
@media (max-width: 768px) {
    .netflix-row-header,
    .netflix-row-scroller {
        padding: 0 1.5rem;
    }
    
    .netflix-card {
        min-width: 150px;
        width: 150px;
    }
    
    .netflix-card-image {
        height: 85px;
    }
    
    .netflix-scroll-button {
        width: 40px;
        height: 80px;
        font-size: 16px;
    }
}

/* Extra small screens */
@media (max-width: 480px) {
    .netflix-row-header,
    .netflix-row-scroller {
        padding: 0 1rem;
    }
    
    .netflix-card {
        min-width: 130px;
        width: 130px;
    }
    
    .netflix-card-image {
        height: 73px;
    }
    
    .netflix-scroll-button {
        width: 35px;
        height: 70px;
        font-size: 14px;
    }
    
    .netflix-scroll-left {
        left: 0.25rem;
    }
    
    .netflix-scroll-right {
        right: 0.25rem;
    }
}

/* High contrast theme support */
@media (prefers-contrast: high) {
    .netflix-card {
        border: 2px solid var(--theme-text-color, #ffffff);
    }
    
    .netflix-scroll-button,
    .netflix-my-list-button {
        border-width: 2px;
    }
}

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
    .netflix-card,
    .netflix-scroll-button,
    .netflix-my-list-button {
        transition: none;
    }
    
    .netflix-card:hover {
        transform: none;
    }
    
    .netflix-row-scroller {
        scroll-behavior: auto;
    }
    
    .loading-spinner {
        animation: none;
        border-top-color: var(--theme-text-color, #ffffff);
    }
}

/* Dark theme detection fallback */
@media (prefers-color-scheme: dark) {
    .netflix-rows-container {
        --fallback-text-color: #ffffff;
        --fallback-bg-color: #181818;
        --fallback-accent-color: #00a4dc;
    }
}

@media (prefers-color-scheme: light) {
    .netflix-rows-container {
        --fallback-text-color: #000000;
        --fallback-bg-color: #ffffff;
        --fallback-accent-color: #0066cc;
    }
}
</style>";

                content = content.Insert(headEndIndex, netflixCSS);
            }

            // Inject JavaScript before closing </body>
            var bodyEndIndex = content.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyEndIndex >= 0)
            {
                var netflixJS = @"
<script id=""netflix-rows-script"">
// Netflix Rows JavaScript - Theme Compatible
(function() {
    'use strict';
    
    class NetflixRows {
        constructor() {
            this.apiBase = '/NetflixRows';
            this.userId = null;
            this.initialized = false;
            this.container = null;
            this.init();
        }
        
        init() {
            // Wait for Jellyfin to be ready
            if (typeof ApiClient === 'undefined') {
                setTimeout(() => this.init(), 500);
                return;
            }
            
            this.userId = ApiClient.getCurrentUserId();
            console.log('Netflix Rows: Initialized with user ID', this.userId);
            
            // Initialize on home page
            this.observeHomePage();
        }
        
        observeHomePage() {
            let debounceTimer = null;
            
            const checkForHome = () => {
                // Clear previous timer
                if (debounceTimer) {
                    clearTimeout(debounceTimer);
                }
                
                // Debounce to avoid excessive calls
                debounceTimer = setTimeout(() => {
                    const isHomePage = this.isOnHomePage();
                    
                    if (isHomePage && !this.initialized) {
                        console.log('Netflix Rows: Detected home page, injecting rows');
                        this.injectNetflixRows();
                    } else if (!isHomePage && this.initialized) {
                        console.log('Netflix Rows: Left home page, cleaning up');
                        this.cleanup();
                    }
                }, 200);
            };
            
            // Check immediately
            checkForHome();
            
            // Listen for navigation changes
            window.addEventListener('hashchange', checkForHome);
            window.addEventListener('popstate', checkForHome);
            
            // Also observe DOM changes for dynamic loading
            const observer = new MutationObserver(() => {
                checkForHome();
            });
            
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
        }
        
        isOnHomePage() {
            const hash = window.location.hash;
            return hash === '' || hash === '#/' || hash === '#/home' || 
                   hash.includes('#/home.html') || hash.includes('#/index.html') ||
                   document.querySelector('.homePage, [data-role=""page""].indexPage');
        }
        
        cleanup() {
            if (this.container) {
                this.container.remove();
                this.container = null;
            }
            this.initialized = false;
        }
        
        injectNetflixRows() {
            // Avoid double injection
            if (this.container && document.contains(this.container)) {
                return;
            }
            
            // Find main content area - try multiple selectors for theme compatibility
            const selectors = [
                '.homePage .pageContainer',
                '.homePage',
                '.indexPage .pageContainer',
                '.indexPage',
                '.page-view .pageContainer',
                '.page-view',
                '.mainContent',
                '.content-primary',
                '.view-home',
                '#indexPage',
                'main',
                '.main'
            ];
            
            let mainContent = null;
            for (const selector of selectors) {
                mainContent = document.querySelector(selector);
                if (mainContent) break;
            }
            
            if (!mainContent) {
                console.log('Netflix Rows: Main content area not found, retrying in 1 second');
                setTimeout(() => this.injectNetflixRows(), 1000);
                return;
            }
            
            // Create Netflix rows container
            this.container = document.createElement('div');
            this.container.id = 'netflix-rows-container';
            this.container.className = 'netflix-rows-container';
            this.container.innerHTML = 
                '<div id=""netflix-rows-loading"" class=""netflix-loading"">' +
                    '<div class=""loading-spinner""></div>' +
                    '<span>Loading Netflix Rows...</span>' +
                '</div>' +
                '<div id=""netflix-rows-content"" style=""display: none;""></div>';
            
            // Insert at the beginning of main content
            mainContent.insertBefore(this.container, mainContent.firstChild);
            
            this.initialized = true;
            
            // Load rows
            this.loadNetflixRows();
        }
        
        async loadNetflixRows() {
            const container = this.container?.querySelector('#netflix-rows-content');
            const loading = this.container?.querySelector('#netflix-rows-loading');
            
            if (!container || !loading || !this.userId) {
                console.log('Netflix Rows: Container elements or userId not available');
                return;
            }
            
            try {
                console.log('Netflix Rows: Loading rows for user', this.userId);
                const response = await fetch(this.apiBase + '/Rows?userId=' + this.userId);
                
                if (!response.ok) {
                    throw new Error('HTTP ' + response.status + ': ' + response.statusText);
                }
                
                const rows = await response.json();
                console.log('Netflix Rows: Loaded', rows.length, 'rows');
                
                if (rows.length === 0) {
                    loading.innerHTML = '<div style=""text-align: center; padding: 2rem; color: var(--theme-text-color, #ffffff);"">No Netflix rows configured. Please check plugin settings.</div>';
                    return;
                }
                
                loading.style.display = 'none';
                container.style.display = 'block';
                
                this.renderRows(rows, container);
            } catch (error) {
                console.error('Netflix Rows: Failed to load rows', error);
                loading.innerHTML = '<div style=""color: #ff6b6b; text-align: center; padding: 2rem;"">Failed to load Netflix Rows: ' + 
                    this.escapeHtml(error.message) + '</div>';
            }
        }
        
        renderRows(rows, container) {
            container.innerHTML = '';
            
            rows.forEach(row => {
                const rowElement = this.createRowElement(row);
                container.appendChild(rowElement);
            });
        }
        
        createRowElement(row) {
            const rowDiv = document.createElement('div');
            rowDiv.className = 'netflix-row';
            
            const headerHTML = '<div class=""netflix-row-header"">' +
                '<h2 class=""netflix-row-title"">' + this.escapeHtml(row.title) + '</h2>' +
                '</div>';
            
            const scrollerHTML = '<div class=""netflix-row-container"">' +
                '<button class=""netflix-scroll-button netflix-scroll-left"" data-row=""' + row.id + '"" aria-label=""Scroll left"">‹</button>' +
                '<div class=""netflix-row-scroller"" data-row=""' + row.id + '"">' +
                (row.previewItems ? row.previewItems.map(item => this.createCardHTML(item, row.type)).join('') : '') +
                '</div>' +
                '<button class=""netflix-scroll-button netflix-scroll-right"" data-row=""' + row.id + '"" aria-label=""Scroll right"">›</button>' +
                '</div>';
            
            rowDiv.innerHTML = headerHTML + scrollerHTML;
            
            // Add scroll functionality
            this.addScrollListeners(rowDiv, row.id);
            
            return rowDiv;
        }
        
        createCardHTML(item, rowType) {
            const imageUrl = this.getImageUrl(item);
            const isInMyList = this.isItemInMyList(item);
            const myListButton = rowType !== 'MyList' ? 
                '<button class=""netflix-my-list-button ' + (isInMyList ? 'added' : '') + '"" ' +
                'data-item-id=""' + item.Id + '"" ' + 
                'onclick=""window.netflixRows.toggleMyList(this, \'' + item.Id + '\')""' +
                'aria-label=""' + (isInMyList ? 'Remove from' : 'Add to') + ' My List"">' +
                (isInMyList ? '✓' : '+') +
                '</button>' : '';
            
            return '<div class=""netflix-card"" data-item-id=""' + item.Id + '"" onclick=""window.netflixRows.playItem(\'' + item.Id + '\')"">' +
                '<img class=""netflix-card-image"" ' +
                'src=""' + imageUrl + '"" ' +
                'alt=""' + this.escapeHtml(item.Name) + '"" ' +
                'loading=""lazy"" />' +
                '<div class=""netflix-card-title"">' + this.escapeHtml(item.Name) + '</div>' +
                myListButton +
                '</div>';
        }
        
        addScrollListeners(rowElement, rowId) {
            const scroller = rowElement.querySelector('[data-row=""' + rowId + '""].netflix-row-scroller');
            const leftBtn = rowElement.querySelector('[data-row=""' + rowId + '""].netflix-scroll-left');
            const rightBtn = rowElement.querySelector('[data-row=""' + rowId + '""].netflix-scroll-right');
            
            if (scroller && leftBtn && rightBtn) {
                leftBtn.addEventListener('click', () => {
                    scroller.scrollBy({ left: -400, behavior: 'smooth' });
                });
                
                rightBtn.addEventListener('click', () => {
                    scroller.scrollBy({ left: 400, behavior: 'smooth' });
                });
            }
        }
        
        async toggleMyList(button, itemId) {
            try {
                const isFavorite = button.classList.contains('added');
                
                await fetch('/Users/' + this.userId + '/FavoriteItems/' + itemId, {
                    method: isFavorite ? 'DELETE' : 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                
                button.classList.toggle('added');
                button.innerHTML = button.classList.contains('added') ? '✓' : '+';
                button.setAttribute('aria-label', (button.classList.contains('added') ? 'Remove from' : 'Add to') + ' My List');
                
            } catch (error) {
                console.error('Netflix Rows: Failed to toggle favorite', error);
            }
        }
        
        playItem(itemId) {
            window.location.href = '#!/details?id=' + itemId;
        }
        
        getImageUrl(item) {
            if (item.ImageTags && item.ImageTags.Primary) {
                return '/Items/' + item.Id + '/Images/Primary?maxWidth=400&tag=' + item.ImageTags.Primary;
            }
            if (item.BackdropImageTags && item.BackdropImageTags.length > 0) {
                return '/Items/' + item.Id + '/Images/Backdrop/0?maxWidth=400&tag=' + item.BackdropImageTags[0];
            }
            return '/web/assets/img/icon-transparent.png';
        }
        
        isItemInMyList(item) {
            return item.UserData && item.UserData.IsFavorite;
        }
        
        escapeHtml(text) {
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }
    }
    
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            window.netflixRows = new NetflixRows();
        });
    } else {
        window.netflixRows = new NetflixRows();
    }
})();
</script>";

                content = content.Insert(bodyEndIndex, netflixJS);
            }

            return JsonSerializer.Serialize(new TransformationData { Contents = content });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Netflix Rows Transform Error: {ex.Message}");
            return input;
        }
    }
}