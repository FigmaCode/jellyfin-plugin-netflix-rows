# 🎬 Jellyfin Netflix Rows Plugin

Transform your Jellyfin home screen into a Netflix-like experience with dynamic rows, genre-based content organization, and a custom watchlist functionality.

## ✨ Features

### 🎯 Netflix-Style Interface
- **Dynamic Rows**: Automatically generated content rows similar to Netflix
- **Horizontal Scrolling**: Smooth, responsive row scrolling with mouse wheel support
- **Lazy Loading**: Rows load only when visible for optimal performance
- **Theme Adaptive**: Seamlessly integrates with all Jellyfin themes including custom ones

### 📚 Content Organization
- **My List**: Custom watchlist using Jellyfin's favorite system with + button
- **Recently Added**: Content from the last X days (configurable)
- **Random Picks**: Discover new content with randomized selections
- **Long Not Watched**: Rediscover older content you haven't seen
- **Genre Rows**: Automatic genre-based content organization

### ⚙️ Advanced Configuration
- **Admin Dashboard**: Comprehensive configuration panel
- **Genre Management**: Enable/disable genres, set minimum item requirements
- **Custom Display Names**: Rename genres (e.g., "Action" → "Adrenalinkick")
- **Blacklist Support**: Hide unwanted genres completely
- **Row Limits**: Configure maximum rows and items per row
- **Flexible Ordering**: Fixed or random row order

### 📱 Modern UX
- **Responsive Design**: Works perfectly on desktop, tablet, and mobile
- **Accessibility**: High contrast mode and reduced motion support
- **Performance Optimized**: Efficient loading and rendering
- **Error Handling**: Graceful fallbacks for missing content

## 🛠️ Installation

### Prerequisites
This plugin requires the following dependencies:
- **Jellyfin 10.10.7** or later
- **[File Transformation Plugin](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** (v2.2.1.0+)
- **[Home Screen Sections Plugin](https://github.com/IAmParadox27/jellyfin-plugin-home-sections)** (v2.2.2.0+) - Optional
- **[Plugin Pages Plugin](https://github.com/IAmParadox27/jellyfin-plugin-pages)** (v2.2.2.0+) - Optional

### Method 1: Plugin Repository (Recommended)
1. Open Jellyfin Admin Dashboard
2. Navigate to **Plugins** → **Repositories**
3. Add repository: `https://github.com/FigmaCode/jellyfin-plugin-netflix-rows/releases/latest/download/repository.json`
4. Go to **Plugins** → **Catalog**
5. Find and install **"Netflix Rows"**
6. Restart Jellyfin

### Method 2: Manual Installation
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file to your Jellyfin plugins directory:
   - Windows: `%ProgramData%\Jellyfin\Server\plugins\Netflix Rows\`
   - Linux: `/var/lib/jellyfin/plugins/Netflix Rows/`
   - Docker: `/config/plugins/Netflix Rows/`
3. Restart Jellyfin

## 🔧 Configuration

### Initial Setup
1. After installation, restart Jellyfin
2. Navigate to **Admin Dashboard** → **Plugins** → **Netflix Rows**
3. Configure your preferred settings (see Configuration Guide below)
4. Save and refresh your browser

### Configuration Guide

#### General Settings
- **Max Rows**: Maximum number of rows displayed (1-20, default: 8)
- **Items per Row**: Minimum and maximum items shown per row (default: 10-25)
- **Row Order**: Fixed priority order or randomized
- **Lazy Loading**: Enable for better performance (recommended)
- **Replace Heart with Plus**: Changes favorite icon to + for "My List" feel

#### Row Types
- **My List** ⭐: Shows user favorites, configurable item limit (default: 50)
- **Recently Added** 📅: Content added in last X days (default: 30)
- **Random Picks** 🎲: Random content for discovery
- **Long Not Watched** 📺: Content older than X months, never played (default: 6)

#### Genre Configuration
- **Enable/Disable Genres**: Choose which genres to display
- **Minimum Items**: Hide genres with fewer than X items (default: 5)
- **Custom Names**: Rename genres for localization
- **Blacklist**: Permanently hide specific genres

### Default MVP Configuration
```json
{
  "maxRows": 8,
  "enabledRows": ["My List", "Recently Added", "Random Picks"],
  "enabledGenres": ["Action", "Anime", "Comedy"],
  "genreDisplayNames": {
    "Action": "Adrenalinkick",
    "Comedy": "Zum Lachen"
  }
}
```

## 🎨 Theming & Customization

The plugin automatically adapts to your Jellyfin theme:
- **Dark/Light Themes**: Automatic color adaptation
- **Custom Themes**: Uses theme CSS variables for perfect integration
- **Mobile Responsive**: Optimized layouts for all screen sizes
- **High Contrast**: Improved visibility for accessibility needs

### Custom Styling
Advanced users can override styles by adding custom CSS to their theme:

```css
.netflix-rows {
  /* Your custom styles here */
}
```

## 🔄 How It Works

### Technical Architecture
- **Backend**: C#/.NET plugin integrating with Jellyfin's API
- **Frontend**: JavaScript injection for UI modifications
- **Styling**: CSS injection with theme integration
- **Dependencies**: Leverages existing plugin ecosystem

### Data Flow
1. Plugin loads user configuration
2. Queries Jellyfin library for content based on enabled rows
3. Transforms favorite system into "My List" functionality  
4. Injects Netflix-style UI components
5. Lazy loads content as user scrolls

## 🚀 Development

### Building from Source
```bash
git clone https://github.com/[YOUR-USERNAME]/jellyfin-plugin-netflix-rows.git
cd jellyfin-plugin-netflix-rows
dotnet restore
dotnet build --configuration Release
```

### Project Structure
```
├── Configuration/
│   ├── PluginConfiguration.cs    # Settings model
│   └── configPage.html          # Admin UI
├── Controllers/
│   └── NetflixRowsController.cs # API endpoints
├── Transformations/
│   ├── CssTransformation.cs     # Style injection
│   └── JsTransformation.cs      # Script injection
├── Plugin.cs                    # Main plugin class
└── .github/workflows/           # CI/CD pipeline
```

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests
5. Submit a pull request

## 📋 API Endpoints

The plugin exposes the following REST endpoints:

- `GET /NetflixRows/MyList` - User's favorite items
- `GET /NetflixRows/RecentlyAdded` - Recently added content  
- `GET /NetflixRows/RandomPicks` - Random content selection
- `GET /NetflixRows/LongNotWatched` - Old unwatched content
- `GET /NetflixRows/Genre/{genre}` - Content by genre
- `GET /NetflixRows/Genres` - Available genres
- `GET /NetflixRows/Config` - Plugin configuration
- `POST /NetflixRows/Config` - Update configuration

## 🔍 Troubleshooting

### Common Issues

**Rows not appearing:**
1. Ensure File Transformation plugin is installed and working
2. Clear browser cache and hard refresh (Ctrl+Shift+R)
3. Check browser developer console for JavaScript errors
4. Verify plugin is enabled in Jellyfin admin

**Performance issues:**
1. Enable lazy loading in settings
2. Reduce maximum items per row
3. Limit number of enabled genres
4. Check server hardware resources

**Styling conflicts:**
1. Disable other UI modification plugins temporarily
2. Check for custom CSS conflicts
3. Try default Jellyfin theme to isolate issue

### Debug Mode
Enable debug logging in Jellyfin settings to see detailed plugin operation logs.

### Support
- 🐛 **Bug Reports**: [GitHub Issues](../../issues)
- 💡 **Feature Requests**: [GitHub Discussions](../../discussions)
- 📚 **Documentation**: [Wiki](../../wiki)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **IAmParadox27** for the foundational plugin architecture
- **Jellyfin Team** for the amazing media server platform
- **Community** for testing and feedback

## 📊 Compatibility Matrix

| Plugin Version | Jellyfin Version | File Transformation | Status |
|---------------|------------------|-------------------|---------|
| 1.0.x | 10.10.7+ | 2.2.1.0+ | ✅ Stable |
| 1.1.x | 10.11.0+ | 2.3.0+ | 🚧 In Development |

---

**Made with ❤️ for the Jellyfin Community**