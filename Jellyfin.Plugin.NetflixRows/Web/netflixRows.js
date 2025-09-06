// Netflix Rows Plugin - Main JavaScript File
console.log('[NetflixRows] Main script loaded');

(function() {
    'use strict';
    
    // Configuration
    var API_BASE = window.location.origin + '/NetflixRows';
    var initialized = false;
    
    console.log('[NetflixRows] Initializing Netflix Rows Plugin');
    
    // Show plugin status indicator
    function showStatusIndicator(message, type) {
        var indicator = document.createElement('div');
        indicator.innerHTML = '🎬 ' + message;
        indicator.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#e50914'};
            color: white;
            padding: 12px 16px;
            border-radius: 8px;
            font-weight: bold;
            z-index: 10000;
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            font-family: Arial, sans-serif;
            font-size: 14px;
            max-width: 300px;
        `;
        
        document.body.appendChild(indicator);
        
        setTimeout(function() {
            if (indicator.parentNode) {
                indicator.remove();
            }
        }, 4000);
    }
    
    // Get current user ID
    function getCurrentUserId() {
        // Try multiple methods to get user ID
        if (window.ApiClient && window.ApiClient.getCurrentUserId) {
            return window.ApiClient.getCurrentUserId();
        }
        
        if (window.Dashboard && window.Dashboard.getCurrentUserId) {
            return window.Dashboard.getCurrentUserId();
        }
        
        // Try to get from localStorage
        try {
            var userStr = localStorage.getItem('jellyfin_credentials');
            if (userStr) {
                var userData = JSON.parse(userStr);
                if (userData.Servers && userData.Servers.length > 0) {
                    var server = userData.Servers[0];
                    if (server.UserId) {
                        return server.UserId;
                    }
                }
            }
        } catch (e) {
            console.warn('[NetflixRows] Could not get user ID from localStorage');
        }
        
        return null;
    }
    
    // Test API connectivity
    function testApi() {
        console.log('[NetflixRows] Testing API connectivity...');
        
        fetch(API_BASE + '/Test')
            .then(function(response) {
                if (response.ok) {
                    return response.text();
                }
                throw new Error('API responded with status: ' + response.status);
            })
            .then(function(message) {
                console.log('[NetflixRows] API test successful:', message);
                showStatusIndicator('Netflix Rows Plugin Connected!', 'success');
                initializeRows();
            })
            .catch(function(error) {
                console.error('[NetflixRows] API test failed:', error);
                showStatusIndicator('Netflix Rows API Error: ' + error.message, 'error');
            });
    }
    
    // Initialize Netflix rows
    function initializeRows() {
        if (initialized) {
            return;
        }
        
        var userId = getCurrentUserId();
        if (!userId) {
            console.warn('[NetflixRows] No user ID available, retrying in 2 seconds...');
            setTimeout(initializeRows, 2000);
            return;
        }
        
        console.log('[NetflixRows] User ID found:', userId);
        
        // Check if we're on the home page
        checkAndSetupHome(userId);
        
        initialized = true;
    }
    
    // Check if on home page and setup
    function checkAndSetupHome(userId) {
        // Wait for home view to be available
        var attempts = 0;
        var maxAttempts = 10;
        
        function findHomeView() {
            attempts++;
            
            var homeView = document.querySelector('.homeView, [data-type="home"], .homePage, .home');
            
            if (homeView) {
                console.log('[NetflixRows] Home view found, setting up Netflix rows...');
                setupNetflixRows(homeView, userId);
                return true;
            }
            
            if (attempts < maxAttempts) {
                console.log('[NetflixRows] Home view not found, attempt', attempts, '- retrying...');
                setTimeout(findHomeView, 1000);
            } else {
                console.warn('[NetflixRows] Could not find home view after', maxAttempts, 'attempts');
                showStatusIndicator('Could not find home page', 'error');
            }
            
            return false;
        }
        
        findHomeView();
    }
    
    // Setup Netflix-style rows
    function setupNetflixRows(homeView, userId) {
        // Create or update Netflix rows container
        var existingContainer = document.getElementById('netflix-rows-container');
        if (existingContainer) {
            existingContainer.remove();
        }
        
        var container = document.createElement('div');
        container.id = 'netflix-rows-container';
        container.style.cssText = `
            margin: 20px 0;
            padding: 0;
        `;
        
        // Insert at the top of home view
        homeView.insertBefore(container, homeView.firstChild);
        
        // Add loading indicator
        var loadingDiv = document.createElement('div');
        loadingDiv.innerHTML = '🎬 Loading Netflix Rows...';
        loadingDiv.style.cssText = `
            color: white;
            text-align: center;
            padding: 20px;
            font-size: 18px;
        `;
        container.appendChild(loadingDiv);
        
        // Load rows
        loadNetflixRows(container, userId);
    }
    
    // Load all Netflix rows
    function loadNetflixRows(container, userId) {
        console.log('[NetflixRows] Loading rows for user:', userId);
        
        // Clear loading indicator
        container.innerHTML = '';
        
        // Load different row types
        Promise.allSettled([
            loadRowData('MyList', userId),
            loadRowData('RecentlyAdded', userId),
            loadRowData('RandomPicks', userId)
        ]).then(function(results) {
            var successCount = 0;
            
            results.forEach(function(result, index) {
                if (result.status === 'fulfilled' && result.value.success) {
                    var rowTitle = ['My List', 'Recently Added', 'Random Picks'][index];
                    createRow(container, rowTitle, result.value.data.Items || []);
                    successCount++;
                } else {
                    console.warn('[NetflixRows] Failed to load row:', index, result.reason);
                }
            });
            
            if (successCount === 0) {
                var errorDiv = document.createElement('div');
                errorDiv.innerHTML = '⚠️ Could not load Netflix rows. Check console for details.';
                errorDiv.style.cssText = `
                    color: #ff6b6b;
                    text-align: center;
                    padding: 20px;
                    font-size: 16px;
                `;
                container.appendChild(errorDiv);
            } else {
                console.log('[NetflixRows] Successfully loaded', successCount, 'rows');
            }
        });
    }
    
    // Load row data from API
    function loadRowData(endpoint, userId) {
        var url = API_BASE + '/' + endpoint + '?userId=' + userId + '&limit=20';
        
        return fetch(url)
            .then(function(response) {
                if (response.ok) {
                    return response.json();
                }
                throw new Error(endpoint + ' API failed with status: ' + response.status);
            })
            .then(function(data) {
                console.log('[NetflixRows]', endpoint, 'loaded:', data.Items ? data.Items.length : 0, 'items');
                return { success: true, data: data };
            })
            .catch(function(error) {
                console.error('[NetflixRows]', endpoint, 'failed:', error);
                return { success: false, error: error };
            });
    }
    
    // Create a Netflix-style row
    function createRow(container, title, items) {
        if (!items || items.length === 0) {
            console.log('[NetflixRows] No items for row:', title);
            return;
        }
        
        console.log('[NetflixRows] Creating row:', title, 'with', items.length, 'items');
        
        var rowDiv = document.createElement('div');
        rowDiv.className = 'netflix-row';
        rowDiv.style.cssText = `
            margin-bottom: 40px;
            padding: 0 20px;
        `;
        
        // Row title
        var titleDiv = document.createElement('h2');
        titleDiv.textContent = title;
        titleDiv.style.cssText = `
            color: white;
            font-size: 1.8em;
            margin-bottom: 15px;
            font-weight: bold;
            font-family: 'Segoe UI', Arial, sans-serif;
        `;
        
        // Row items container
        var itemsContainer = document.createElement('div');
        itemsContainer.className = 'netflix-items-container';
        itemsContainer.style.cssText = `
            display: flex;
            overflow-x: auto;
            gap: 15px;
            padding-bottom: 15px;
            scroll-behavior: smooth;
        `;
        
        // Add scrollbar styling
        itemsContainer.style.setProperty('scrollbar-width', 'thin');
        itemsContainer.style.setProperty('scrollbar-color', '#e50914 #333');
        
        // Add items
        items.forEach(function(item) {
            var itemDiv = createRowItem(item);
            itemsContainer.appendChild(itemDiv);
        });
        
        rowDiv.appendChild(titleDiv);
        rowDiv.appendChild(itemsContainer);
        container.appendChild(rowDiv);
    }
    
    // Create individual row item
    function createRowItem(item) {
        var itemDiv = document.createElement('div');
        itemDiv.className = 'netflix-item';
        itemDiv.style.cssText = `
            min-width: 220px;
            width: 220px;
            height: 330px;
            background: #333;
            border-radius: 8px;
            cursor: pointer;
            transition: all 0.3s ease;
            display: flex;
            flex-direction: column;
            overflow: hidden;
            position: relative;
            box-shadow: 0 4px 8px rgba(0,0,0,0.3);
        `;
        
        // Hover effects
        itemDiv.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.08)';
            this.style.zIndex = '10';
            this.style.boxShadow = '0 8px 16px rgba(0,0,0,0.5)';
        });
        
        itemDiv.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
            this.style.zIndex = '1';
            this.style.boxShadow = '0 4px 8px rgba(0,0,0,0.3)';
        });
        
        // Click handler
        itemDiv.addEventListener('click', function() {
            if (item.Id) {
                console.log('[NetflixRows] Navigating to item:', item.Name, item.Id);
                window.location.hash = '#/details?id=' + item.Id;
            }
        });
        
        // Item image
        if (item.ImageTags && item.ImageTags.Primary) {
            var img = document.createElement('img');
            img.src = '/Items/' + item.Id + '/Images/Primary?maxHeight=300&quality=85';
            img.style.cssText = `
                width: 100%;
                height: 250px;
                object-fit: cover;
                background: #555;
            `;
            
            img.onerror = function() {
                this.style.display = 'none';
            };
            
            itemDiv.appendChild(img);
        }
        
        // Item info overlay
        var infoDiv = document.createElement('div');
        infoDiv.style.cssText = `
            position: absolute;
            bottom: 0;
            left: 0;
            right: 0;
            background: linear-gradient(transparent, rgba(0,0,0,0.8));
            padding: 20px 10px 10px;
            color: white;
        `;
        
        // Item title
        var titleDiv = document.createElement('div');
        titleDiv.textContent = item.Name || 'Unknown Title';
        titleDiv.style.cssText = `
            font-size: 14px;
            font-weight: bold;
            margin-bottom: 5px;
            text-shadow: 1px 1px 2px rgba(0,0,0,0.8);
            line-height: 1.3;
        `;
        
        // Item year/type
        var metaDiv = document.createElement('div');
        var year = item.ProductionYear ? item.ProductionYear : '';
        var type = item.Type === 'Series' ? 'TV Series' : 'Movie';
        metaDiv.textContent = [year, type].filter(Boolean).join(' • ');
        metaDiv.style.cssText = `
            font-size: 12px;
            opacity: 0.8;
            text-shadow: 1px 1px 2px rgba(0,0,0,0.8);
        `;
        
        infoDiv.appendChild(titleDiv);
        infoDiv.appendChild(metaDiv);
        itemDiv.appendChild(infoDiv);
        
        return itemDiv;
    }
    
    // Initialize when ready
    function init() {
        console.log('[NetflixRows] Initializing...');
        showStatusIndicator('Netflix Rows Plugin Loading...', 'info');
        
        // Test API first
        testApi();
    }
    
    // Start initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    
    // Handle navigation changes (for SPA)
    window.addEventListener('hashchange', function() {
        if (window.location.hash.includes('home') || window.location.hash === '' || window.location.hash === '#/') {
            console.log('[NetflixRows] Navigated to home, reinitializing...');
            initialized = false;
            setTimeout(function() {
                initializeRows();
            }, 500);
        }
    });
    
})();