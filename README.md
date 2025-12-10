# Jellyfin Requests Bridge

Jellyfin plugin that integrates [Jellyseerr](https://github.com/Fallenbagel/jellyseerr) directly into the Jellyfin web UI. Request movies and TV shows without leaving Jellyfin. Android TV is supported via my fork of the Jellyfin Android TV app.

## Features
- Seamless integration: Jellyseerr overlay inside Jellyfin
- Discover button in navigation bar and sidebar (desktop & mobile)
- Android TV support via [JellyArc-Jellyseerr-AndroidTv](https://github.com/Serekay/jellyarc-jellyserr-androidtv)
- Auto-inject: adds the client script into `index.html` at startup
- Optional Tailscale Jellyseerr URL endpoint for remote access

## Mobile: Use as Web App (recommended)

The native Jellyfin apps for iOS and Android cannot properly display external links (e.g. to TMDB, IMDb). **Solution:** Open Jellyfin in your browser and add it to your home screen as a web app. It works just like a native app – fullscreen mode, dedicated icon, no installation required.

<details>
<summary>iPhone / iPad (Safari)</summary>

1. Open **Safari** and navigate to your Jellyfin URL
2. Tap the **Share button** (square with arrow pointing up)
3. Scroll down and select **"Add to Home Screen"**
4. Enter a name (e.g. "Jellyfin") and tap **"Add"**

> **Note:** Only Safari can add web apps to the home screen. Chrome, Firefox etc. don't support this on iOS.

</details>

<details>
<summary>Android (Chrome)</summary>

1. Open **Chrome** and navigate to your Jellyfin URL
2. Tap the **three-dot menu** (top right)
3. Select **"Add to Home screen"** or **"Install app"**
4. Enter a name and tap **"Add"**

> **Tip:** If "Install app" appears, choose that option – Jellyfin will be installed as a full web app.

</details>

### Benefits
- Fullscreen mode without browser address bar
- Dedicated app icon on your home screen
- External links (TMDB, IMDb etc.) work properly
- No app store installation needed
- Always up to date

## Installation Guide
The plugin injects a script tag into Jellyfin’s `index.html`. Many Docker images mount the web directory read-only, so write access is required. Choose one method:

<details open>
<summary>Method 1: Unraid (recommended)</summary>

Use the **User Scripts** plugin to grant write permissions at startup. Without this, the plugin cannot inject its script on Unraid containers.

1) Install the **User Scripts** plugin from the Unraid “Apps” tab.  
2) Find your Jellyfin container name:
```bash
docker ps --format "{{.Names}}" | grep jelly
```
3) Create a script in **Settings → User Scripts → Add New Script**, name it `Jellyfin-Web-Permissions`, edit it and paste (replace container name if needed):
```bash
#!/bin/bash
sleep 15
docker exec -u 0 binhex-jellyfin chmod -R 777 /usr/share/jellyfin/web
echo "Permissions granted for Jellyfin Requests Bridge"
```
4) Save, set schedule to **At Startup of Array**, and run once manually.  
5) Install the plugin (see below).

</details>

<details>
<summary>Method 2: Docker Compose (volume mapping)</summary>

Copy the Jellyfin web assets to the host and mount them read/write.

1) Prepare files (run on host):
```bash
mkdir -p ./custom-web
docker run --rm -v "$(pwd)/custom-web:/out" jellyfin/jellyfin cp -a /usr/share/jellyfin/web/. /out/
```
2) Update `docker-compose.yml`:
```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    volumes:
      - ./config:/config
      - ./custom-web:/usr/share/jellyfin/web:rw
```
3) `docker-compose up -d`

Note: after Jellyfin image updates you must refresh the copied web files or you may get 404s.

</details>

<details>
<summary>Method 3: Direct volume mapping (not recommended)</summary>

Mount `/usr/share/jellyfin/web` read/write from the host. Risk: stale files after Jellyfin upgrades unless you recopy everything.

Example (Unraid):
```bash
mkdir -p /mnt/user/appdata/jellyfin/custom-web
rm -rf /mnt/user/appdata/jellyfin/custom-web/*
docker cp binhex-jellyfin:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
```
Then map container path `/usr/share/jellyfin/web` to that host path with RW access.

</details>

## Step 2: Install the Plugin
1) Jellyfin Dashboard → Plugins → Repositories.  
2) Add repository URL:
```
https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/master/manifest.json
```
3) Open Catalog, install “Requests Bridge”.  
4) Restart Jellyfin.  
5) On Unraid, if the button is missing, run the User Script once and refresh.

## Step 3: Configuration
1) Dashboard → Plugins → Requests Bridge.  
2) Jellyseerr URL: e.g. `http://192.168.1.100:5055`.  
3) Tailscale Jellyseerr URL (optional): e.g. `http://100.x.y.z:5055` for remote devices.  
4) Jellyseerr API Key: from Jellyseerr → Settings → General.  
5) Save.

## Troubleshooting
- **404 after Jellyfin update:** If you mapped `/usr/share/jellyfin/web`, recopy fresh web files or re-run the volume-setup steps. Method 1 avoids this.  
- **Discover button missing:** Check logs for “RequestsBridge”; ensure web path is writable (User Script/volume RW), then clear browser cache.  
- **Plugin install fails or vanishes:** Stop Jellyfin, delete plugin folder (`/var/lib/jellyfin/plugins/RequestsBridge` or Unraid equivalent), restart, reinstall.

## API Endpoints
| Endpoint | Description |
|----------|-------------|
| `GET /plugins/requests/apibase` | Configured Jellyseerr URL |
| `GET /plugins/requests/tailscale/apibase` | Configured Tailscale Jellyseerr URL (optional) |
| `GET /plugins/requests/apikey` | Configured API key |
| `GET /plugins/requests/proxy/*` | Proxy to Jellyseerr |

## License
MIT
