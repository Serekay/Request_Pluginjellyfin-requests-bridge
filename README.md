# Jellyfin Requests Bridge

A Jellyfin plugin that integrates [Jellyseerr](https://github.com/Fallenbagel/jellyseerr) directly into the Jellyfin web interface. Request movies and TV shows without leaving Jellyfin.

## Features

- Jellyseerr embedded as an overlay within Jellyfin
- "Discover" button in the navigation bar and sidebar, working on Desktop & Mobile
- Full Jellyseerr integration in the Android TV Jellyfin app with this plugin. 
   - get it from: 🔗 GitHub: https://github.com/Serekay/jellyfin-jellyserr-tv
- Automatic UI injection on plugin startup

## Installation

### Step 1: Docker Web Directory Setup (Required for Docker!)

The plugin needs to modify Jellyfin's `index.html` to inject the "Discover" button. In Docker, the web directory is read-only by default. **You must set this up BEFORE installing the plugin.**

#### Unraid (binhex-jellyfin / linuxserver/jellyfin)

> **All commands below must be run in the Unraid Terminal** (click "Terminal" in the Unraid web UI, top right corner)

1. **Stop the Jellyfin container** (via Unraid Docker UI)

2. **Find your container name:**
   ```bash
   docker ps -a --format "{{.Names}}" | grep -i jelly
   ```
   Common names: `jellyfin`, `binhex-jellyfin`, `Jellyfin`, etc.

3. **Copy the original web files** (replace `YOUR_CONTAINER_NAME` with your actual container name!):
   ```bash
   # Create the custom-web directory
   mkdir -p /mnt/user/appdata/jellyfin/custom-web

   # Copy files from container - USE YOUR CONTAINER NAME!
   docker cp YOUR_CONTAINER_NAME:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/

   # Examples:
   # docker cp jellyfin:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
   # docker cp binhex-jellyfin:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
   # docker cp Jellyfin:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
   ```

4. **Add Path mapping** in Unraid Docker settings (Edit the container):
   - Click "Add another Path, Port, Variable, Label or Device"
   - **Config Type:** Path
   - **Name:** Custom Web (or what you want)
   - **Container Path:** `/usr/share/jellyfin/web`
   - **Host Path:** `/mnt/user/appdata/jellyfin/custom-web`
   - **Access Mode:** Read/Write

5. **Start the container**

#### Docker Compose

> **Run these commands in your terminal/SSH where docker-compose.yml is located**

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    volumes:
      - ./config:/config
      - ./custom-web:/usr/share/jellyfin/web:rw
```

**First-time setup (run BEFORE starting the container):**
```bash
# Create directory and copy original files
mkdir -p ./custom-web
docker run --rm -v "$(pwd)/custom-web:/out" jellyfin/jellyfin cp -r /usr/share/jellyfin/web/. /out/
```

#### Generic Docker

> **Run these commands in your terminal/SSH on the Docker host**

```bash
# 1. Find your container name
docker ps -a --format "{{.Names}}" | grep -i jelly

# 2. Create host directory
mkdir -p /path/to/custom-web

# 3. Copy original web files (replace YOUR_CONTAINER_NAME!)
docker cp YOUR_CONTAINER_NAME:/usr/share/jellyfin/web/. /path/to/custom-web/

# 4. Add volume mapping to your docker run command:
-v /path/to/custom-web:/usr/share/jellyfin/web:rw
```

#### Native Installation (no Docker)

Skip this step - the plugin can modify the web directory directly.

---

### Step 2: Install the Plugin

1. Open **Jellyfin Dashboard** → **Plugins** → **Repositories**
2. Add the following repository URL:
   ```
   https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/master/manifest.json
   ```
3. Go to **Catalog** and install **"Requests Bridge"**
4. Restart Jellyfin

The plugin will automatically patch `index.html` on startup.

---

### Step 3: Configure the Plugin

1. Go to **Dashboard** → **Plugins** → **Requests Bridge**
2. Enter your Jellyseerr URL (e.g., `http://192.168.1.100:5055`)
3. Enter your Jellyseerr API Key (found in Jellyseerr → Settings → General)
4. Click **Save**

## Manual Installation (Alternative)

1. Complete Step 1 above (Docker setup)
2. Download the latest `Jellyfin.Plugin.RequestsBridge.zip` from [Releases](https://github.com/Serekay/jellyfin-requests-bridge/releases)
3. Extract to your Jellyfin plugins folder:
   - Linux: `/var/lib/jellyfin/plugins/RequestsBridge/`
   - Docker: `/config/data/plugins/RequestsBridge/`
   - Windows: `%LOCALAPPDATA%\jellyfin\plugins\RequestsBridge\`
4. Restart Jellyfin
5. Configure (Step 3)

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /plugins/requests/apibase` | Returns the configured Jellyseerr URL |
| `GET /plugins/requests/apikey` | Returns the configured API key |
| `GET /plugins/requests/proxy/*` | Proxy requests to Jellyseerr |

## Troubleshooting

### Jellyfin won't start after Docker volume mapping

The custom-web directory is empty! You need to copy the original files first.

**In Unraid Terminal** (or SSH on Docker host):
```bash
# Find your container name first!
docker ps -a --format "{{.Names}}" | grep -i jelly

# Then copy (replace YOUR_CONTAINER_NAME!)
docker cp YOUR_CONTAINER_NAME:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
```

### "Discover" button doesn't appear

1. Check Jellyfin logs for "RequestsBridge" entries
2. If you see "cannot read" or "error writing" → Web directory not writable (see Step 1)
3. Clear browser cache (`Ctrl+Shift+R`)

### Jellyseerr doesn't load in overlay

1. Verify Jellyseerr is accessible from the Jellyfin server
2. Check the URL in plugin settings
3. The plugin proxies all requests, so CORS issues should be handled automatically

### Plugin disappears after restart

Delete the old plugin folder manually and reinstall.

**In Unraid Terminal:**
```bash
# Stop Jellyfin first, then:
rm -rf /mnt/user/appdata/jellyfin/data/plugins/Requests\ Bridge*
# Start Jellyfin and reinstall from catalog
```

## License

MIT