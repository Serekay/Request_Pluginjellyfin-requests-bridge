# Jellyfin Requests Bridge

Jellyfin plugin that integrates [Jellyseerr](https://github.com/Fallenbagel/jellyseerr) directly into the Jellyfin web UI. Request movies and TV shows without leaving Jellyfin.

## Features
- **Discover Button** – Adds a "Discover" button to Jellyfin's navigation (desktop & mobile)
- **Jellyseerr Overlay** – Opens Jellyseerr inside Jellyfin, no tab switching needed
- **Android TV Support** – Works with [JellyArc-Jellyseerr-AndroidTv](https://github.com/Serekay/jellyarc)
- **Auto-Inject** – Automatically adds the client script to Jellyfin's `index.html` on startup
- **VPN/Remote Access** – Optional Tailscale URL endpoints for accessing from outside your network
- **Performance Option** – Disable user data on Collections if they load slowly

---

## Mobile: Use as Web App (recommended)

The native Jellyfin apps for iOS and Android cannot properly display external links (e.g. to TMDB, IMDb). **Solution:** Open Jellyfin in your browser and add it to your home screen as a web app. It works just like a native app – fullscreen mode, dedicated icon, no installation required.

<details>
<summary><b>iPhone / iPad (Safari)</b></summary>

1. Open **Safari** and navigate to your Jellyfin URL
2. Tap the **Share button** (square with arrow pointing up)
3. Scroll down and select **"Add to Home Screen"**
4. Enter a name (e.g. "Jellyfin") and tap **"Add"**

> **Note:** Only Safari can add web apps to the home screen. Chrome, Firefox etc. don't support this on iOS.

</details>

<details>
<summary><b>Android (Chrome)</b></summary>

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

---

## Installation

The plugin needs to modify Jellyfin's `index.html` file. Many Docker setups mount this directory as read-only, so you need to grant write access first.

### Step 1: Grant Write Access

Choose the method that matches your setup:

<details open>
<summary><b>Method 1: Unraid (recommended for Unraid users)</b></summary>

Use the **User Scripts** plugin to grant write permissions at startup.

1. Install **User Scripts** from the Unraid "Apps" tab
2. Find your Jellyfin container name by running this in Unraid Terminal:
   ```bash
   docker ps --format "{{.Names}}" | grep jelly
   ```
3. Go to **Settings → User Scripts → Add New Script**
4. Name it `Jellyfin-Web-Permissions` and paste this (replace `binhex-jellyfin` with your container name if different):
   ```bash
   #!/bin/bash
   sleep 15
   docker exec -u 0 binhex-jellyfin chmod -R 777 /usr/share/jellyfin/web
   echo "Permissions granted for Jellyfin Requests Bridge"
   ```
5. Save and set schedule to **At Startup of Array**
6. Run the script once manually
7. Continue with Step 2

</details>

<details>
<summary><b>Method 2: Docker Compose</b></summary>

Copy Jellyfin's web files to your host and mount them with read/write access.

1. Create the directory and copy files:
   ```bash
   mkdir -p ./custom-web
   docker run --rm -v "$(pwd)/custom-web:/out" jellyfin/jellyfin cp -a /usr/share/jellyfin/web/. /out/
   ```
2. Add this volume mapping to your `docker-compose.yml`:
   ```yaml
   services:
     jellyfin:
       image: jellyfin/jellyfin
       volumes:
         - ./config:/config
         - ./custom-web:/usr/share/jellyfin/web:rw
   ```
3. Restart: `docker-compose up -d`

> **Warning:** After Jellyfin updates, you must re-copy the web files or you may get 404 errors.

</details>

<details>
<summary><b>Method 3: Direct Volume Mapping (not recommended)</b></summary>

Mount `/usr/share/jellyfin/web` directly from the host with read/write access.

Example for Unraid:
```bash
mkdir -p /mnt/user/appdata/jellyfin/custom-web
rm -rf /mnt/user/appdata/jellyfin/custom-web/*
docker cp binhex-jellyfin:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
```
Then map container path `/usr/share/jellyfin/web` to that host path with RW access.

> **Warning:** Files become stale after Jellyfin upgrades. You must recopy everything after updates.

</details>

### Step 2: Install the Plugin

1. Open **Jellyfin Dashboard → Plugins → Repositories**
2. Click **"+"** to add a new repository
3. Enter this URL:
   ```
   https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/master/manifest.json
   ```
4. **Restart Jellyfin**
5. Go to **Plugins → Catalog** and install **"Requests Bridge"**
6. **Restart Jellyfin again**

> **Unraid Users:** If the "Discover" button doesn't appear, run the User Script manually, restart Jellyfin, clear your browser cache, and refresh the page.

---

## Configuration

Go to **Dashboard → Plugins → Requests Bridge** to configure the plugin.

### Basic Configuration (Required)

These settings are required for the plugin to work:

| Setting | Description | Example |
|---------|-------------|---------|
| **Jellyseerr URL** | Your local Jellyseerr address | `http://192.168.1.100:5055` |
| **Jellyseerr API Key** | Your API key from Jellyseerr | (see below how to find it) |

**How to find your Jellyseerr API Key:**
1. Open Jellyseerr
2. Go to **Settings → General**
3. Copy the **API Key**

### VPN / Remote Access (Optional)

These settings are only needed if you access Jellyfin from outside your home network using a VPN like Tailscale. Leave them empty if you only use Jellyfin locally.

| Setting | Description | Example |
|---------|-------------|---------|
| **Local Jellyfin URL** | Your local Jellyfin address | `http://192.168.1.100:8096` |
| **Tailscale Jellyseerr URL** | Jellyseerr via Tailscale | `http://100.64.0.1:5055` |
| **Tailscale Jellyfin URL** | Jellyfin via Tailscale | `http://100.64.0.2:8096` |

**When do you need this?**
- You use Tailscale (or another VPN) to access your server remotely
- You use the [Android TV app](https://github.com/Serekay/jellyarc-jellyserr-androidtv) outside your home network
- The Android TV app uses these URLs to switch between local and remote connections

### Performance Settings (Optional)

| Setting | Description |
|---------|-------------|
| **Disable User Data on Collections** | Enable this if Collections load slowly. Disables watch status and progress indicators on Collections to improve performance. |

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **Discover button missing** | 1. Check Jellyfin logs for "RequestsBridge" errors<br>2. Make sure the web directory is writable (run User Script or check volume mapping)<br>3. Clear browser cache and refresh |
| **404 errors after Jellyfin update** | If you used Method 2 or 3, re-copy the web files from the new Jellyfin version |
| **Plugin disappears after restart** | Stop Jellyfin, delete the plugin folder (`/var/lib/jellyfin/plugins/RequestsBridge`), restart, and reinstall |
| **Jellyseerr overlay not loading** | Check that your Jellyseerr URL is correct and reachable from your browser |

---

## API Endpoints (For Developers)

The plugin provides these API endpoints for integrations like the Android TV app:

### Local Network Endpoints

| Endpoint | Returns |
|----------|---------|
| `GET /plugins/requests/apibase` | Jellyseerr URL |
| `GET /plugins/requests/jellyfinbase` | Local Jellyfin URL |
| `GET /plugins/requests/apikey` | Jellyseerr API Key |

### Tailscale / VPN Endpoints

| Endpoint | Returns |
|----------|---------|
| `GET /plugins/requests/tailscale/apibase` | Tailscale Jellyseerr URL |
| `GET /plugins/requests/tailscale/jellyfinbase` | Tailscale Jellyfin URL |

### Other Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /plugins/requests/proxy/*` | Proxy requests to Jellyseerr |

All endpoints return JSON: `{ "success": true, "data": "..." }`

---

## License

MIT
