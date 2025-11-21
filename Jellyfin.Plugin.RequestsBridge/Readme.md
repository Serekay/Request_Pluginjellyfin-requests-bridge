# Jellyfin Requests Bridge Plugin

Integriert Jellyseerr direkt in die Jellyfin Web-UI. Ermöglicht Film- und Serienanfragen ohne die Jellyfin-Oberfläche zu verlassen.

## Features

- Jellyseerr als Overlay in Jellyfin eingebettet
- "Wünsche"-Button in der Tab-Leiste und Sidebar
- API-Key Endpoint für Android TV App Integration
- Automatische index.html Injection (wenn Schreibrechte vorhanden)

## Installation

### Methode 1: Plugin-Repository (Empfohlen)

1. Öffne Jellyfin Dashboard → Plugins → Repositories
2. Füge folgende Repository-URL hinzu:
   ```
   https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/main/manifest.json
   ```
3. Gehe zu "Katalog" und installiere "Requests Bridge (Jellyseerr)"
4. Jellyfin neustarten

### Methode 2: Manuelle Installation

1. Lade die neueste `Jellyfin.Plugin.RequestsBridge.zip` von den Releases herunter
2. Entpacke in: `/config/data/plugins/RequestsBridge/`
3. Jellyfin neustarten

## Konfiguration

Nach der Installation:

1. Dashboard → Plugins → Requests Bridge (Jellyseerr)
2. Jellyseerr URL eintragen (z.B. `http://192.168.178.37:5055`)
3. Jellyseerr API-Key eintragen (findest du in Jellyseerr → Settings → General)
4. Speichern

## API-Endpoints

| Endpoint | Beschreibung |
|----------|--------------|
| `GET /plugins/requests/apikey` | Gibt den konfigurierten API-Key zurück (für Android TV App) |
| `GET /plugins/requests/proxy/*` | Proxy zu Jellyseerr |

## Automatische vs. Manuelle Script-Injection

Das Plugin versucht automatisch, den "Wünsche"-Button in die Jellyfin-UI zu injizieren.

### Automatische Injection funktioniert wenn:
- Jellyfin auf Windows läuft
- Docker-Container mit beschreibbarem Web-Verzeichnis

### Manuelle Schritte nötig bei:
- Read-Only Docker-Container (Standard bei den meisten Images)

### Unraid/Docker: Volume Mapping einrichten

Für **binhex-jellyfin** oder **linuxserver/jellyfin**:

```bash
# 1. index.html aus Container kopieren
docker cp jellyfin:/usr/share/jellyfin/web/index.html /mnt/user/appdata/jellyfin/custom-web/index.html

# 2. Volume Mapping in Docker-Einstellungen hinzufügen:
#    Host: /mnt/user/appdata/jellyfin/custom-web/index.html
#    Container: /usr/share/jellyfin/web/index.html
#    Mode: rw

# 3. Container neustarten
```

Das Plugin patcht dann automatisch die gemountete index.html beim Start.

### Alternative: Docker Compose

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    volumes:
      - ./config:/config
      - ./custom-web/index.html:/usr/share/jellyfin/web/index.html:rw
```

## Fehlerbehebung

### "Wünsche"-Button erscheint nicht

1. Prüfe Jellyfin-Logs: Suche nach "RequestsBridge"
2. Wenn "nicht beschreibbar" → Volume Mapping einrichten (siehe oben)
3. Browser-Cache leeren (Ctrl+Shift+R)

### Jellyseerr lädt nicht im Overlay

1. Prüfe ob Jellyseerr erreichbar ist
2. Prüfe die URL in den Plugin-Einstellungen
3. CORS-Probleme? Das Plugin proxied bereits alle Requests

### API-Key Endpoint gibt leeren Key zurück

1. API-Key in Plugin-Einstellungen eintragen
2. Speichern klicken
3. Jellyfin muss NICHT neu gestartet werden

## Entwicklung

### Build

```bash
dotnet build -c Release
```

### Release erstellen

```powershell
.\build-release.ps1 -Version "1.0.1.0"
```

## Lizenz

MIT
