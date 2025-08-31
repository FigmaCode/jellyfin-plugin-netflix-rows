// Netflix Rows Plugin - Complete JavaScript Implementation
(function() {
    'use strict';

    console.log('Netflix Rows Plugin loaded');
    
    let netflixConfig = null;
    
    // Load configuration from server
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

    // Replace heart icons with plus icons
    function replaceHeartWithPlus() {
        if (!netflixConfig || !netflixConfig.replaceHeartWithPlus) return;
        
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        const heartIcons = node.querySelectorAll ? node.querySelectorAll('.material-icons') : [];
                        heartIcons.forEach(function(icon) {
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

    // Create Netflix-style rows interface
    function createNetflixRows() {
        console.log('Creating Netflix Rows...');
        const homeView = document.querySelector('.homeView, .view[data-type="home"]');
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
        
        const loadingDiv = document.createElement('div');
        loadingDiv.className = 'loading-indicator';
        
        const spinner = document.createElement('div');
        spinner.className = 'loading-spinner';
        
        const text = document.createElement('p');
        text.textContent = 'Lädt Netflix Rows...';
        
        loadingDiv.appendChild(spinner);
        loadingDiv.appendChild(text);
        rowsDiv.appendChild(loadingDiv);
        rowsContainer.appendChild(rowsDiv);
        
        const existingContent = homeView.querySelector('.sections, .homePageContent');
        if (existingContent) {
            existingContent.parentNode.insertBefore(rowsContainer, existingContent);
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
                    title: 'Meine Liste',
                    endpoint: '/NetflixRows/MyList?userId=' + userId + '&limit=' + (netflixConfig.maxItemsPerRow || 25),
                    priority: 0
                });
            }
            
            if (netflixConfig.enableRecentlyAdded) {
                rows.push({
                    id: 'recently-added',
                    title: 'Kürzlich hinzugefügt',
                    endpoint: '/NetflixRows/RecentlyAdded?userId=' + userId + '&limit=' + (netflixConfig.maxItemsPerRow || 25),
                    priority: 1
                });
            }
            
            if (netflixConfig.enableRandomPicks) {
                rows.push({
                    id: 'random-picks',
                    title: 'Zufallsauswahl',
                    endpoint: '/NetflixRows/RandomPicks?userId=' + userId + '&limit=' + (netflixConfig.maxItemsPerRow || 25),
                    priority: 2
                });
            }
            
            if (netflixConfig.enableLongNotWatched) {
                rows.push({
                    id: 'long-not-watched',
                    title: 'Lange nicht gesehen',
                    endpoint: '/NetflixRows/LongNotWatched?userId=' + userId + '&limit=' + (netflixConfig.maxItemsPerRow || 25),
                    priority: 3
                });
            }

            // Add genre rows
            if (netflixConfig.enabledGenres && Array.isArray(netflixConfig.enabledGenres)) {
                netflixConfig.enabledGenres.forEach(function(genre, index) {
                    const displayName = (netflixConfig.genreDisplayNames && netflixConfig.genreDisplayNames[genre]) || genre;
                    rows.push({
                        id: 'genre-' + genre.toLowerCase(),
                        title: displayName,
                        endpoint: '/NetflixRows/Genre/' + encodeURIComponent(genre) + '?userId=' + userId + '&limit=' + (netflixConfig.maxItemsPerRow || 25),
                        priority: 4 + index
                    });
                });
            }

            console.log('Rows to create:', rows.length);
            
            // Sort rows by priority (unless random order is enabled)
            if (!netflixConfig.randomRowOrder) {
                rows.sort(function(a, b) { return a.priority - b.priority; });
            } else {
                rows.sort(function() { return Math.random() - 0.5; });
            }
            
            // Limit rows
            const maxRows = netflixConfig.maxRows || 8;
            const limitedRows = rows.slice(0, maxRows);
            
            for (let i = 0; i < limitedRows.length; i++) {
                const row = limitedRows[i];
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
            const errorDiv = document.createElement('p');
            errorDiv.className = 'error';
            errorDiv.textContent = 'Fehler beim Laden der Netflix Rows';
            rowsContainer.innerHTML = '';
            rowsContainer.appendChild(errorDiv);
        }
    }
    
    // Create a single row element
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
        
        const placeholder = document.createElement('div');
        placeholder.className = 'loading-placeholder';
        placeholder.textContent = 'Lädt...';
        
        scroller.appendChild(placeholder);
        content.appendChild(scroller);
        header.appendChild(title);
        rowDiv.appendChild(header);
        rowDiv.appendChild(content);
        
        return rowDiv;
    }
    
    // Lazy loading observer
    function observeRowForLazyLoading(rowElement, rowData) {
        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
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
    
    // Load content for a specific row
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
            console.log('Row data loaded:', data.Items ? data.Items.length + ' items' : 'no items');
            
            if (!data.Items || data.Items.length === 0) {
                const noItems = document.createElement('p');
                noItems.className = 'no-items';
                noItems.textContent = 'Keine Inhalte gefunden';
                scrollerElement.innerHTML = '';
                scrollerElement.appendChild(noItems);
                return;
            }
            
            const itemsContainer = document.createElement('div');
            itemsContainer.className = 'netflix-row-items';
            
            for (let i = 0; i < Math.min(data.Items.length, netflixConfig.maxItemsPerRow || 25); i++) {
                const item = data.Items[i];
                const itemCard = createItemCard(item);
                itemsContainer.appendChild(itemCard);
            }
            
            scrollerElement.innerHTML = '';
            scrollerElement.appendChild(itemsContainer);
            
            // Add scroll functionality
            addRowScrollFunctionality(scrollerElement);
            
        } catch (error) {
            console.error('Netflix Rows: Error loading row content:', error);
            const errorMsg = document.createElement('p');
            errorMsg.className = 'error';
            errorMsg.textContent = 'Fehler beim Laden';
            scrollerElement.innerHTML = '';
            scrollerElement.appendChild(errorMsg);
        }
    }
    
    // Create an item card
    function createItemCard(item) {
        const card = document.createElement('div');
        card.className = 'netflix-item-card';
        card.setAttribute('data-item-id', item.Id);
        
        const link = document.createElement('a');
        link.href = getItemUrl(item);
        link.className = 'netflix-item-link';
        
        const imageContainer = document.createElement('div');
        imageContainer.className = 'netflix-item-image-container';
        
        const img = document.createElement('img');
        img.className = 'netflix-item-image';
        img.src = getItemImageUrl(item);
        img.alt = item.Name || '';
        img.loading = 'lazy';
        
        const overlay = document.createElement('div');
        overlay.className = 'netflix-item-overlay';
        
        const actions = document.createElement('div');
        actions.className = 'netflix-item-actions';
        
        const favoriteBtn = document.createElement('button');
        favoriteBtn.className = 'netflix-item-favorite';
        favoriteBtn.setAttribute('data-item-id', item.Id);
        favoriteBtn.type = 'button';
        
        const isFavorite = item.UserData && item.UserData.IsFavorite;
        favoriteBtn.title = isFavorite ? 'Aus meiner Liste entfernen' : 'Zu meiner Liste hinzufügen';
        
        const icon = document.createElement('i');
        icon.className = 'material-icons';
        icon.textContent = isFavorite ? 'remove' : 'add';
        
        favoriteBtn.appendChild(icon);
        actions.appendChild(favoriteBtn);
        overlay.appendChild(actions);
        imageContainer.appendChild(img);
        imageContainer.appendChild(overlay);
        
        const info = document.createElement('div');
        info.className = 'netflix-item-info';
        
        const title = document.createElement('h3');
        title.className = 'netflix-item-title';
        title.textContent = item.Name || '';
        
        info.appendChild(title);
        
        if (item.ProductionYear) {
            const year = document.createElement('span');
            year.className = 'netflix-item-year';
            year.textContent = item.ProductionYear;
            info.appendChild(year);
        }
        
        link.appendChild(imageContainer);
        link.appendChild(info);
        card.appendChild(link);
        
        return card;
    }
    
    // Add scroll functionality to a row
    function addRowScrollFunctionality(scrollerElement) {
        const itemsContainer = scrollerElement.querySelector('.netflix-row-items');
        if (!itemsContainer) return;
        
        // Mouse wheel scrolling
        itemsContainer.addEventListener('wheel', function(e) {
            if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;
            
            e.preventDefault();
            const scrollAmount = e.deltaY > 0 ? 300 : -300;
            itemsContainer.scrollBy({ left: scrollAmount, behavior: 'smooth' });
        });
        
        // Favorite button functionality
        itemsContainer.addEventListener('click', function(e) {
            if (e.target.closest('.netflix-item-favorite')) {
                e.preventDefault();
                e.stopPropagation();
                const button = e.target.closest('.netflix-item-favorite');
                const itemId = button.getAttribute('data-item-id');
                toggleFavorite(itemId, button);
            }
        });
    }
    
    // Toggle favorite status of an item
    async function toggleFavorite(itemId, buttonElement) {
        try {
            const userId = getCurrentUserId();
            if (!userId) {
                console.error('No user ID available for favorite toggle');
                return;
            }
            
            console.log('Toggling favorite for item:', itemId);
            const response = await fetch('/Users/' + userId + '/FavoriteItems/' + itemId, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            if (response.ok) {
                const icon = buttonElement.querySelector('.material-icons');
                const isFavorite = icon.textContent === 'remove';
                
                if (isFavorite) {
                    icon.textContent = 'add';
                    buttonElement.title = 'Zu meiner Liste hinzufügen';
                    console.log('Item removed from favorites');
                } else {
                    icon.textContent = 'remove';
                    buttonElement.title = 'Aus meiner Liste entfernen';
                    console.log('Item added to favorites');
                }
                
                // Refresh My List if currently visible
                if (netflixConfig.enableMyList) {
                    const myListRow = document.querySelector('[data-row-id="my-list"]');
                    if (myListRow) {
                        const rowData = {
                            endpoint: '/NetflixRows/MyList?userId=' + userId + '&limit=' + (netflixConfig.maxItemsPerRow || 25)
                        };
                        setTimeout(function() {
                            loadRowContent(myListRow, rowData);
                        }, 500);
                    }
                }
            } else {
                console.error('Failed to toggle favorite status:', response.status);
            }
        } catch (error) {
            console.error('Netflix Rows: Error toggling favorite:', error);
        }
    }
    
    // Get image URL for an item
    function getItemImageUrl(item) {
        const baseUrl = getApiBaseUrl();
        if (item.ImageTags && item.ImageTags.Primary) {
            return baseUrl + '/Items/' + item.Id + '/Images/Primary?width=300&height=450&quality=90';
        } else if (item.ImageTags && item.ImageTags.Thumb) {
            return baseUrl + '/Items/' + item.Id + '/Images/Thumb?width=300&height=450&quality=90';
        } else if (item.ImageTags && item.ImageTags.Backdrop) {
            return baseUrl + '/Items/' + item.Id + '/Images/Backdrop?width=300&height=450&quality=90';
        }
        return '/web/assets/img/icon-transparent.png';
    }
    
    // Get item URL for navigation
    function getItemUrl(item) {
        if (item.Type === 'Movie') {
            return '#!/details?id=' + item.Id;
        } else if (item.Type === 'Series') {
            return '#!/details?id=' + item.Id;
        } else if (item.Type === 'Episode') {
            return '#!/details?id=' + item.Id;
        } else if (item.Type === 'Season') {
            return '#!/details?id=' + item.Id;
        }
        return '#!/details?id=' + item.Id;
    }
    
    // Get current user ID
    function getCurrentUserId() {
        // Try multiple methods to get user ID
        if (window.ApiClient && typeof window.ApiClient.getCurrentUserId === 'function') {
            return window.ApiClient.getCurrentUserId();
        }
        if (window.Dashboard && typeof window.Dashboard.getCurrentUserId === 'function') {
            return window.Dashboard.getCurrentUserId();
        }
        if (window.ApiClient && window.ApiClient.getCurrentUser) {
            const user = window.ApiClient.getCurrentUser();
            return user ? user.Id : null;
        }
        
        // Fallback to localStorage
        const userId = localStorage.getItem('userId');
        if (userId) {
            return userId;
        }
        
        // Try to extract from current page URL or context
        const pathMatch = window.location.hash.match(/userId=([^&]+)/);
        if (pathMatch) {
            return pathMatch[1];
        }
        
        console.warn('Netflix Rows: Could not determine user ID');
        return null;
    }
    
    // Get API base URL
    function getApiBaseUrl() {
        if (window.ApiClient && typeof window.ApiClient.serverAddress === 'function') {
            return window.ApiClient.serverAddress();
        }
        if (window.ApiClient && window.ApiClient.baseUrl) {
            return window.ApiClient.baseUrl;
        }
        if (window.ApiClient && window.ApiClient.appName) {
            // Likely we have a valid ApiClient, try to construct URL
            return window.location.protocol + '//' + window.location.host;
        }
        return window.location.origin;
    }
    
    // Cleanup function for plugin unload
    function cleanup() {
        const container = document.getElementById('netflix-rows-container');
        if (container) {
            container.remove();
        }
    }
    
    // Handle page navigation and cleanup
    function handleNavigation() {
        const currentPath = window.location.hash;
        console.log('Navigation detected:', currentPath);
        
        // Only show Netflix rows on home page
        if (currentPath === '' || currentPath === '#/' || currentPath.includes('home')) {
            setTimeout(createNetflixRows, 100);
        } else {
            cleanup();
        }
    }
    
    // Initialize the plugin
    function init() {
        console.log('Netflix Rows init');
        
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', init);
            return;
        }
        
        loadConfig().then(function() {
            console.log('Config loaded, setting up observers');
            
            // Replace heart icons if enabled
            replaceHeartWithPlus();
            
            // Set up mutation observer for DOM changes
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
            
            // Listen for navigation changes
            window.addEventListener('hashchange', handleNavigation);
            window.addEventListener('popstate', handleNavigation);
            
            // Initial check for home view
            if (document.querySelector('.homeView')) {
                setTimeout(createNetflixRows, 100);
            }
            
            // Also check on initial load
            handleNavigation();
            
        }).catch(function(error) {
            console.error('Netflix Rows: Error during initialization:', error);
        });
    }
    
    // Expose cleanup function globally for debugging
    window.NetflixRowsCleanup = cleanup;
    
    // Start the plugin
    init();
})();