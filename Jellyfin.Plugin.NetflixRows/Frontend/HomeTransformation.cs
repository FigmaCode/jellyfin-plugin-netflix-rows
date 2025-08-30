using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetflixRows.Frontend;

/// <summary>
/// Transforms the index.html page to inject Netflix rows.
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

            // Inject CSS directly into <head>
            var headEndIndex = content.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headEndIndex >= 0)
            {
                var netflixCSS = @"
<style id=""netflix-rows-styles"">
/* Netflix Rows Styles */
.netflix-rows-container {
    margin: 20px 0;
    width: 100%;
    background: transparent;
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
// Netflix Rows JavaScript
(function() {
    'use strict';
    
    class NetflixRows {
        constructor() {
            this.apiBase = '/NetflixRows';
            this.userId = null;
            this.initialized = false;
            this.init();
        }
        
        init() {
            // Wait for Jellyfin to be ready
            if (typeof ApiClient === 'undefined') {
                setTimeout(() => this.init(), 500);
                return;
            }
            
            this.userId = ApiClient.getCurrentUserId();
            
            // Initialize on home page
            this.observeHomePage();
        }
        
        observeHomePage() {
            const checkForHome = () => {
                // Check if we're on the home page
                const isHomePage = window.location.hash === '' || 
                                 window.location.hash === '#/' || 
                                 window.location.hash.includes('#/home');
                                 
                if (isHomePage && !this.initialized) {
                    this.injectNetflixRows();
                    this.initialized = true;
                } else if (!isHomePage) {
                    this.initialized = false;
                }
            };
            
            // Check immediately and on navigation changes
            checkForHome();
            window.addEventListener('hashchange', checkForHome);
            
            // Also observe DOM changes
            const observer = new MutationObserver(() => {
                checkForHome();
            });
            
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
        }
        
        injectNetflixRows() {
            // Find main content area
            const mainContent = document.querySelector('.mainDrawerContent') || 
                              document.querySelector('.main') || 
                              document.querySelector('#indexPage');
                              
            if (!mainContent) {
                setTimeout(() => this.injectNetflixRows(), 1000);
                return;
            }
            
            // Check if already injected
            if (document.querySelector('#netflix-rows-container')) {
                return;
            }
            
            // Create and inject Netflix rows container
            const netflixContainer = document.createElement('div');
            netflixContainer.id = 'netflix-rows-container';
            netflixContainer.className = 'netflix-rows-container';
            netflixContainer.innerHTML = '<div id=""netflix-rows-loading"" class=""netflix-loading"">' +
                '<div class=""loading-spinner""></div>' +
                '<span>Loading Netflix Rows...</span>' +
                '</div>' +
                '<div id=""netflix-rows-content"" style=""display: none;""></div>';
            
            // Insert at the beginning of main content
            mainContent.insertBefore(netflixContainer, mainContent.firstChild);
            
            // Load rows
            this.loadNetflixRows();
        }
        
        async loadNetflixRows() {
            const container = document.querySelector('#netflix-rows-content');
            const loading = document.querySelector('#netflix-rows-loading');
            
            if (!container || !this.userId) {
                console.log('Netflix Rows: Container or userId not available');
                return;
            }
            
            try {
                console.log('Netflix Rows: Loading rows for user', this.userId);
                const response = await fetch(this.apiBase + '/Rows?userId=' + this.userId);
                
                if (!response.ok) {
                    throw new Error('HTTP ' + response.status);
                }
                
                const rows = await response.json();
                console.log('Netflix Rows: Loaded', rows.length, 'rows');
                
                loading.style.display = 'none';
                container.style.display = 'block';
                
                this.renderRows(rows, container);
            } catch (error) {
                console.error('Netflix Rows: Failed to load rows', error);
                loading.innerHTML = '<div style=""color: #ff6b6b; text-align: center;"">Failed to load Netflix Rows: ' + error.message + '</div>';
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
                '<button class=""netflix-scroll-button netflix-scroll-left"" data-row=""' + row.id + '"">‹</button>' +
                '<div class=""netflix-row-scroller"" data-row=""' + row.id + '"">' +
                row.previewItems.map(item => this.createCardHTML(item, row.type)).join('') +
                '</div>' +
                '<button class=""netflix-scroll-button netflix-scroll-right"" data-row=""' + row.id + '"">›</button>' +
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
                'onclick=""window.netflixRows.toggleMyList(this, \'' + item.Id + '\')"">' +
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
            // Log error but return original content to avoid breaking the page
            Console.WriteLine($"Netflix Rows Transform Error: {ex.Message}");
            return input;
        }
    }
}