```markdown
# Jellyfin Netflix Rows Plugin

Ein Plugin für Jellyfin, das die Startseite in Netflix-ähnliche horizontale Rows verwandelt.

## Features

- **Netflix-style Rows**: Horizontale Scroll-Bereiche wie bei Netflix
- **Meine Liste**: Favoriten-basierte Watchlist mit Plus-Icon
- **Kürzlich hinzugefügt**: Automatische Row für neue Inhalte
- **Genre-Rows**: Automatisch generierte Rows basierend auf Genres
- **Zufällige Auswahl**: Entdeckung neuer Inhalte
- **Lange nicht gesehen**: Inhalte, die schon länger nicht angeschaut wurden
- **Vollständig konfigurierbar**: Admin-Panel für alle Einstellungen
- **Lazy Loading**: Performance-optimiert durch verzögertes Laden

## Installation

### Über Repository (Empfohlen)

1. In Jellyfin Admin Panel → Plugins → Repositories
2. Repository URL hinzufügen: `https://raw.githubusercontent.com/IhrUsername/jellyfin-plugin-netflix-rows/main/manifest.json`
3. Plugin "Netflix Rows" installieren
4. Jellyfin Server neu starten

### Manuell

1. Neueste Release von GitHub herunterladen
2. DLL in `jellyfin/plugins/NetflixRows/` kopieren
3. Server neu starten

## Konfiguration

Nach der Installation:

1. Admin Panel → Plugins → Netflix Rows
2. Einstellungen nach Wunsch anpassen:
   - Row-Typen aktivieren/deaktivieren
   - Genres auswählen
   - Anzahl Items pro Row
   - Zeiträume für "Kürzlich hinzugefügt" etc.

## Kompatibilität

- **Jellyfin Version**: 10.10.7+ 
- **Framework**: .NET 8.0
- **Plattformen**: Alle unterstützten Jellyfin-Plattformen

## Entwicklung

### Voraussetzungen

- .NET 8.0 SDK
- Jellyfin 10.10.7+ Development Headers

### Build

```bash
dotnet build
```

### Deployment

```bash
dotnet publish -c Release
```

## Lizenz

MIT License - siehe LICENSE Datei

## Support

Issues und Feature Requests bitte über GitHub Issues.
```