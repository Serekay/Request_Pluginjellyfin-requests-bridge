# Jellyfin Requests Bridge

A Jellyfin plugin that integrates [Jellyseerr](https://github.com/Fallenbagel/jellyseerr) directly into the Jellyfin web interface. Request movies and TV shows without leaving Jellyfin.

## Features

- Jellyseerr embedded as an overlay within Jellyfin
- "Discover" button in the navigation bar and sidebar
- API endpoint for external integrations (e.g., Android TV apps)
- Automatic UI injection (when write permissions are available)

## Installation

### Method 1: Plugin Repository (Recommended)

1. Open **Jellyfin Dashboard** → **Plugins** → **Repositories**
2. Add the following repository URL:
   ```
   https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/master/manifest.json
   ```
3. Maybe you need to restart your Jellyfin Server to see the plugin in Catalog
4. Go to **Catalog** and install **"Requests Bridge (Jellyseerr)"**
5. Restart Jellyfin

### Method 2: Manual Installation

1. Download the latest `Jellyfin.Plugin.RequestsBridge.zip` from [Releases](https://github.com/Serekay/jellyfin-requests-bridge/releases)
2. Extract to your Jellyfin plugins folder:
   - Linux: `/var/lib/jellyfin/plugins/RequestsBridge/`
   - Docker: `/config/data/plugins/RequestsBridge/`
   - Windows: `%LOCALAPPDATA%\jellyfin\plugins\RequestsBridge\`
3. Restart Jellyfin

## Configuration

After installation:

1. Go to **Dashboard** → **Plugins** → **Requests Bridge (Jellyseerr)**
2. Enter your Jellyseerr URL (e.g., `http://192.168.1.100:5055`)
3. Enter your Jellyseerr API Key (found in Jellyseerr → Settings → General)
4. Click **Save**

## API Endpoints (Nice to know)

| Endpoint | Description |
|----------|-------------|
| `GET /plugins/requests/apikey` | Returns the configured API key (for external apps) |
| `GET /plugins/requests/proxy/*` | Proxy requests to Jellyseerr |

## Docker Setup

The plugin automatically injects the "Discover" button into Jellyfin's UI. However, most Docker images have a read-only web directory. You need to make the web directory writable for the plugin to work.

### Unraid

For **binhex-jellyfin** or **linuxserver/jellyfin** on Unraid:

1. Add a new **Path** mapping in the Unraid Docker settings:
   - **Container Path:** `/usr/share/jellyfin/web`
   - **Host Path:** `/mnt/user/appdata/jellyfin/custom-web`
   - **Access Mode:** Read/Write

2. Restart the container

The plugin will automatically patch `index.html` on startup.

### Docker Compose

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    volumes:
      - ./config:/config
      - ./custom-web:/usr/share/jellyfin/web:rw
```

### Generic Docker

Add a volume mapping for the web directory:

```
/path/to/custom-web:/usr/share/jellyfin/web:rw
```

Then restart the container. The plugin handles everything else automatically.

## Troubleshooting

### "Discover" button doesn't appear

1. Check Jellyfin logs for "RequestsBridge" entries
2. If logs show "not writable" → Set up volume mapping (see above)
3. Clear browser cache (`Ctrl+Shift+R`)

### Jellyseerr doesn't load in overlay

1. Verify Jellyseerr is accessible from the Jellyfin server
2. Check the URL in plugin settings
3. The plugin proxies all requests, so CORS issues should be handled automatically

### API key endpoint returns empty

1. Enter the API key in plugin settings
2. Click Save
3. No restart required

## Development

### Build

```bash
dotnet build -c Release
```

### Create Release

```powershell
.\build-release.ps1 -Version "1.0.1"
```

## License

MIT
