using System;
using System.Text.Json;

namespace Jellyfin.Plugin.NetflixRows.Frontend;

/// <summary>
/// Injects Netflix-style JavaScript.
/// </summary>
public class JSTransformation
{
    /// <summary>
    /// Inject Netflix JavaScript functionality.
    /// </summary>
    /// <param name="input">Input JSON with JS contents.</param>
    /// <returns>Modified JavaScript.</returns>
    public static string InjectNetflixJS(string input)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TransformationData>(input);
            if (data?.Contents == null) return input;

            var netflixJS = @"
// Netflix Rows JavaScript
(function() {
    'use strict';
    
    class NetflixRows {
        constructor() {
            this.apiBase = '/NetflixRows';
            this.userId = null;
            this.init();
        }
        
        init() {
            // Wait for Jellyfin to be ready
            if (typeof ApiClient === 'undefined' || typeof Dashboard === 'undefined') {
                setTimeout(() => this.init(), 100);
                return;
            }
            
            this.userId = ApiClient.getCurrentUserId();
            
            // Initialize on home page load
            this.observeHomePage();
        }
        
        observeHomePage() {
            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    if (mutation.type === 'childList') {
                        const homeContainer = document.querySelector('#netflix-rows-container');
                        if (homeContainer && !homeContainer.classList.contains('loaded')) {
                            homeContainer.classList.add('loaded');
                            this.loadNetflixRows();
                        }
                    }
                });
            });
            
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });
        }
        
        async loadNetflixRows() {
            const container = document.querySelector('#netflix-rows-content');
            const loading = document.querySelector('#netflix-rows-loading');
            
            if (!container || !this.userId) return;
            
            try {
                const response = await fetch(`${this.apiBase}/Rows?userId=${this.userId}`);
                
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }
                
                const rows = await response.json();
                
                loading.style.display = 'none';
                container.style.display = 'block';
                
                this.renderRows(rows, container);
            } catch (error) {
                console.error('Netflix Rows: Failed to load rows', error);
                loading.innerHTML = `<div style='color: #ff6b6b; text-align: center;'>Failed to load Netflix Rows</div>`;
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
            rowDiv.innerHTML = `
                <div class='netflix-row-header'>
                    <h2 class='netflix-row-title'>${this.escapeHtml(row.title)}</h2>
                </div>
                <div class='netflix-row-container'>
                    <button class='netflix-scroll-button netflix-scroll-left' data-row='${row.id}'>‹</button>
                    <div class='netflix-row-scroller' data-row='${row.id}'>
                        ${row.previewItems.map(item => this.createCardHTML(item, row.type)).join('')}
                    </div>
                    <button class='netflix-scroll-button netflix-scroll-right' data-row='${row.id}'>›</button>
                </div>
            `;
            
            // Add scroll functionality
            this.addScrollListeners(rowDiv, row.id);
            
            return rowDiv;
        }
        
        createCardHTML(item, rowType) {
            const imageUrl = this.getImageUrl(item);
            const isInMyList = this.isItemInMyList(item);
            const myListButton = rowType !== 'MyList' ? 
                `<button class='netflix-my-list-button ${isInMyList ? 'added' : ''}' 
                         data-item-id='${item.Id}' 
                         onclick='netflixRows.toggleMyList(this, \"${item.Id}\")'>
                    ${isInMyList ? '✓' : '+'}
                 </button>` : '';
            
            return `
                <div class='netflix-card' data-item-id='${item.Id}' onclick='netflixRows.playItem(\"${item.Id}\")'>
                    <img class='netflix-card-image' 
                         src='${imageUrl}' 
                         alt='${this.escapeHtml(item.Name)}'
                         loading='lazy' />
                    <div class='netflix-card-title'>${this.escapeHtml(item.Name)}</div>
                    ${myListButton}
                </div>
            `;
        }
        
        addScrollListeners(rowElement, rowId) {
            const scroller = rowElement.querySelector(`[data-row='${rowId}'].netflix-row-scroller`);
            const leftBtn = rowElement.querySelector(`[data-row='${rowId}'].netflix-scroll-left`);
            const rightBtn = rowElement.querySelector(`[data-row='${rowId}'].netflix-scroll-right`);
            
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
                // Toggle favorite status
                const isFavorite = button.classList.contains('added');
                
                await fetch(`/Users/${this.userId}/FavoriteItems/${itemId}`, {
                    method: isFavorite ? 'DELETE' : 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                
                // Update button
                button.classList.toggle('added');
                button.innerHTML = button.classList.contains('added') ? '✓' : '+';
                
            } catch (error) {
                console.error('Netflix Rows: Failed to toggle favorite', error);
            }
        }
        
        playItem(itemId) {
            if (typeof ApiClient !== 'undefined' && ApiClient.getCurrentUserId) {
                window.location.href = `#!/details?id=${itemId}`;
            }
        }
        
        getImageUrl(item) {
            if (item.ImageTags && item.ImageTags.Primary) {
                return `/Items/${item.Id}/Images/Primary?maxWidth=400&tag=${item.ImageTags.Primary}`;
            }
            return '/web/assets/img/icon-transparent.png';
        }
        
        isItemInMyList(item) {
            return item.UserData && item.UserData.IsFavorite;
        }
        
        escapeHtml(text) {
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
})();";

            var content = data.Contents + netflixJS;
            return JsonSerializer.Serialize(new TransformationData { Contents = content });
        }
        catch (Exception)
        {
            return input;
        }
    }
}