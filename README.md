# Jellyfin Requests Bridge

A Jellyfin plugin that integrates [Jellyseerr](https://github.com/Fallenbagel/jellyseerr) directly into the Jellyfin web interface. Request movies and TV shows without leaving Jellyfin.

## Features

- 🎬 **Seamless Integration:** Jellyseerr embedded as an overlay within Jellyfin.
- 🧭 **Discovery:** "Discover" button in the navigation bar and sidebar (Desktop & Mobile).
- 📺 **Android TV Support:** Full integration via my fork [JellyArc-Jellyseerr-AndroidTv](https://github.com/Serekay/jellyarc-jellyserr-androidtv) (original from https://github.com/jellyfin/jellyfin-androidtv).
- ⚡ **Auto-Inject:** Automatic UI injection on plugin startup.

---

## 🚀 Installation Guide

The plugin needs to modify Jellyfin's `index.html` to inject the "Discover" button. In Docker, the web directory is read-only by default. **Choose your installation method below.**

<details open>
<summary><h2>🟢 Method 1: Unraid (Recommended)</h2></summary>

This method uses the **User Scripts** plugin to grant write permissions at startup.
<br>
**✅ Pros:** Updates work automatically without breaking the UI (No 404 errors!). No file copying required.

### Prerequisites
1. Install the **"User Scripts"** plugin from the Unraid "Apps" tab.

### Step-by-Step

1. **Find your Container Name:**
   Open the Unraid Terminal (top right) and type:
   ```bash
   docker ps --format "{{.Names}}" | grep jelly
   ```
   *Note the name (usually `binhex-jellyfin` or `jellyfin`).*

2. **Create the Script:**
   - Go to **Settings** → **User Scripts** in Unraid.
   - Click **"Add New Script"**.
   - Name it: `Jellyfin-Web-Permissions`.
   - Click the **Gear Icon** next to the new script → **Edit Script**.

3. **Paste the Code:**
   Copy the code below. **Replace `binhex-jellyfin`** with the name you found in Step 1 if it differs!

   ```bash
   #!/bin/bash
   # Wait for Jellyfin to fully start (adjust sleep if needed)
   sleep 15
   
   # Grant write permissions to the web directory inside the container
   # Replace 'binhex-jellyfin' with your actual container name
   docker exec -u 0 binhex-jellyfin chmod -R 777 /usr/share/jellyfin/web
   
   echo "Permissions granted for Jellyfin Requests Bridge"
   ```

4. **Set Schedule:**
   - Click **"Save Changes"**.
   - Change the schedule dropdown from "Don't Run" to **"At Startup of Array"**.
   - **Important:** Click **"Run Script"** once manually to apply it right now.

5. **Proceed to "Step 2: Install the Plugin" below.**

</details>

<details>
<summary><h2>🔵 Method 2: Docker Compose</h2></summary>

For standard Docker installations, volume mapping is the most reliable method.

**⚠️ Warning:** When updating the Jellyfin image, you must re-run the setup commands to update the web files, otherwise you will get **404 Not Found** errors.

**1. Prepare the files (Run on Host):**
```bash
# Create directory
mkdir -p ./custom-web

# Copy current web files from the image to your host
# NOTE: We copy EVERYTHING, not just index.html, to ensure consistency
docker run --rm -v "$(pwd)/custom-web:/out" jellyfin/jellyfin cp -a /usr/share/jellyfin/web/. /out/
```

**2. Update `docker-compose.yml`:**
```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    volumes:
      - ./config:/config
      - ./custom-web:/usr/share/jellyfin/web:rw  # <--- Add this line
```

**3. Start Container:**
`docker-compose up -d`

</details>

<details>
<summary><h2>🟠 Method 3: Volume Mapping (Not Recommended)</h2></summary>

**⚠️ Why is this not recommended?**
This method copies the static web files to your host. When Jellyfin updates (e.g. version 10.9 to 10.10), your host files remain "old". This causes **Version Mismatches (404 Errors)** and the UI will break until you manually delete and re-copy the files.

**If you still want to use this method:**

1. **Stop the Container.**

2. **Clean old files & Copy new ones:**
   Open Unraid Terminal:
   ```bash
   # Create directory
   mkdir -p /mnt/user/appdata/jellyfin/custom-web
   
   # CLEAR directory (Critical for updates!)
   rm -rf /mnt/user/appdata/jellyfin/custom-web/*
   
   # Copy ALL web files (Replace binhex-jellyfin with your container name)
   docker cp binhex-jellyfin:/usr/share/jellyfin/web/. /mnt/user/appdata/jellyfin/custom-web/
   ```

3. **Add Path Mapping in Unraid:**
   - **Config Type:** Path
   - **Container Path:** `/usr/share/jellyfin/web`
   - **Host Path:** `/mnt/user/appdata/jellyfin/custom-web`
   - **Access Mode:** Read/Write

4. **Start Container.**

</details>

---

## 📦 Step 2: Install the Plugin

1. Open **Jellyfin Dashboard** → **Plugins** → **Repositories**.
2. Add the repository URL:
   ```text
   [https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/master/manifest.json](https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/master/manifest.json)
   ```
3. Go to **Catalog**, find **"Requests Bridge"** and install it.
4. **Restart Jellyfin.**
5. *Unraid Users:* If the button doesn't appear immediately, run your "User Script" manually once more and refresh the page.

---

## ⚙️ Step 3: Configuration

1. Go to **Dashboard** → **Plugins** → **Requests Bridge**.
2. **Jellyseerr URL:** Enter the full URL (e.g., `http://192.168.1.100:5055`).
3. **Jellyseerr API Key:** Found in Jellyseerr → Settings → General.
4. Click **Save**.

The "Discover" button should now appear in your sidebar!

---

## 🛠 Troubleshooting

### 🔴 I get "404 Not Found" errors after a Jellyfin Update
**Cause:** You are using Method 2 or 3 (Volume Mapping) and your mapped files are outdated.
**Fix:**
1. Stop the container.
2. Delete the contents of your local `custom-web` folder.
3. Remove the Volume Mapping temporarily.
4. Start container → Copy new files via `docker cp` → Add mapping back.
5. **Better Fix:** Switch to **Method 1 (User Scripts)** to avoid this issue in the future.

### ⚪ The "Discover" button is missing
1. Check the logs: `Dashboard` → `Logs`. Look for "RequestsBridge".
2. If it says "Access Denied" or "Read-only file system":
   - **Unraid:** Run your User Script manually and refresh.
   - **Docker:** Check if your volume mapping has `:rw` (Read/Write).
3. Clear your Browser Cache (`Ctrl + F5` or `Ctrl + Shift + R`).

### 🟠 Plugin installation fails or disappears after restart
This happens if the plugin files got corrupted.
**Fix:**
1. Stop Jellyfin.
2. Delete the plugin folder manually:
   - **Unraid:** `/mnt/user/appdata/jellyfin/data/plugins/Requests Bridge`
   - **Linux:** `/var/lib/jellyfin/plugins/RequestsBridge`
3. Start Jellyfin and reinstall from Catalog.

---

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /plugins/requests/apibase` | Returns the configured Jellyseerr URL |
| `GET /plugins/requests/apikey` | Returns the configured API key |
| `GET /plugins/requests/proxy/*` | Proxy requests to Jellyseerr |

## License

MIT