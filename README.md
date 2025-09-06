# Netflix Rows Plugin for Jellyfin

[![Build](https://github.com/IAmParadox27/jellyfin-plugin-netflixrows/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/IAmParadox27/jellyfin-plugin-netflixrows/actions/workflows/build-and-release.yml)

Transform your Jellyfin home screen with Netflix-style horizontal content rows for a premium streaming experience.

## Features

🎬 **Netflix-Style Interface**: Horizontal scrolling rows with smooth hover effects  
📚 **Smart Content Sections**: My List, Recently Added, Random Picks, and Genre-based rows  
🎨 **Seamless Integration**: Works with Jellyfin's Home Screen Sections plugin  
⚙️ **Highly Configurable**: Customize which sections to show and how many items per row  
📱 **Responsive Design**: Optimized for all screen sizes from mobile to desktop  
🌙 **Theme Compatible**: Integrates perfectly with Jellyfin's dark and light themes

## Prerequisites

This plugin requires the following Jellyfin plugins to be installed first:

1. **[File Transformation Plugin](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** - For CSS styling injection
2. **[Home Screen Sections Plugin](https://github.com/IAmParadox27/jellyfin-plugin-home-sections)** - For managing home screen sections

Install them from the plugin catalog or manually from:
```
https://www.iamparadox.dev/jellyfin/plugins/manifest.json
```

## Installation

### From Plugin Catalog
1. Navigate to **Admin Dashboard → Plugins → Catalog**
2. Search for "Netflix Rows"
3. Click **Install** and restart Jellyfin

### Manual Installation
1. Download the latest `netflix-rows-plugin.zip` from [Releases](https://github.com/IAmParadox27/jellyfin-plugin-netflixrows/releases)
2. Extract to your Jellyfin plugins directory:
   - **Windows**: `%ProgramData%\Jellyfin\Server\plugins\Netflix Rows`
   - **Linux**: `/var/lib/jellyfin/plugins/Netflix Rows`
   - **Docker**: `/config/plugins/Netflix Rows`
3. Restart Jellyfin

## Configuration

1. Go to **Admin Dashboard → Plugins → Netflix Rows**
2. Configure your preferred sections:

### Available Sections
- **My List**: Shows user favorites
- **Recently Added**: Latest content (configurable timeframe)
- **Random Picks**: Randomly selected content for discovery
- **Genre Rows**: Create custom rows for specific genres

### Settings
```json
{
  "EnableMyList": true,
  "MyListCount": 20,
  "EnableRecentlyAdded": true,
  "RecentlyAddedCount": 25,
  "RecentlyAddedDays": 30,
  "EnableRandomPicks": true,
  "RandomPicksCount": 20,
  "EnabledGenres": ["Action", "Comedy", "Drama", "Horror", "Sci-Fi"],
  "GenreDisplayNames": {
    "Sci-Fi": "Science Fiction",
    "Action": "Action & Adventure"
  },
  "GenreRowCounts": {
    "Action": 25,
    "Comedy": 20
  },
  "MaxItemsPerRow": 50
}
```

## Architecture

This plugin follows a modular architecture:

### Integration Components
- **File Transformation**: Injects Netflix-style CSS into Jellyfin's interface
- **Home Screen Sections**: Registers content sections via API endpoints
- **Section Endpoints**: Provides formatted data for each Netflix-style row

### Data Flow
```
1. Plugin starts → Registers CSS transformation
2. Plugin registers sections with Home Screen Sections plugin
3. Home Screen Sections plugin calls our section endpoints
4. Netflix-style rows appear on Jellyfin home screen
```

## API Endpoints

The plugin exposes the following endpoints for the Home Screen Sections plugin:

- `GET /NetflixRows/MyListSection` - My List content
- `GET /NetflixRows/RecentlyAddedSection` - Recently added content  
- `GET /NetflixRows/RandomPicksSection` - Random picks content
- `GET /NetflixRows/GenreSection/{genre}` - Genre-specific content

## Development

### Building
```bash
dotnet build --configuration Release
```

### Testing
```bash
dotnet test
```

### Project Structure
```
Jellyfin.Plugin.NetflixRows/
├── Configuration/           # Plugin configuration
├── Controllers/            # API endpoints  
├── Transformations/        # File transformation handlers
├── Web/                   # Static assets (CSS/JS)
└── Plugin.cs              # Main plugin class
```

## Compatibility

- **Jellyfin**: 10.10.0 or later
- **Platforms**: Windows, Linux, macOS, Docker
- **Browsers**: All modern browsers with CSS Grid support

## Troubleshooting

### Netflix Rows Not Appearing
1. Verify File Transformation plugin is installed and enabled
2. Check Home Screen Sections plugin is installed and enabled  
3. Restart Jellyfin after installing dependencies
4. Review Jellyfin logs for plugin errors

### Styling Issues
1. Clear browser cache and reload
2. Check if custom CSS themes conflict
3. Verify CSS injection in browser developer tools

### Section Configuration
1. Ensure sections are enabled in plugin settings
2. Check that content exists for configured sections
3. Verify API endpoints are responding correctly

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Commit changes: `git commit -am 'Add feature'`
4. Push to branch: `git push origin feature-name`
5. Submit a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

- **Jellyfin Team**: For the amazing media server platform
- **Plugin Dependencies**: File Transformation & Home Screen Sections plugins
- **Netflix**: For the UI/UX inspiration

## Support

- 🐛 **Issues**: [GitHub Issues](https://github.com/IAmParadox27/jellyfin-plugin-netflixrows/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/IAmParadox27/jellyfin-plugin-netflixrows/discussions)  
- 📖 **Wiki**: [Plugin Documentation](https://github.com/IAmParadox27/jellyfin-plugin-netflixrows/wiki)