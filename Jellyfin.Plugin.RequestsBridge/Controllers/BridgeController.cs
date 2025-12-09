using System.Net;
using System.Net.Http.Headers;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.RequestsBridge.Controllers;

[ApiController]
public sealed class BridgeController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;

    private BridgeConfig Config => RequestsBridgePlugin.Instance?.Configuration ?? new BridgeConfig();

    public BridgeController(IHttpClientFactory httpClientFactory)
    {
        _httpFactory = httpClientFactory;
    }

    // ---------------- API-Key Endpoint ----------------
    /// <summary>
    /// Gibt den konfigurierten Jellyseerr API-Key zurück.
    /// Nützlich für die Android TV App.
    /// </summary>
    [HttpGet]
    [Route("plugins/requests/apikey")]
    public IActionResult GetApiKey()
    {
        var apiKey = Config.JellyseerrApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Ok(new { success = false, message = "API-Key not configured", apiKey = "" });
        }
        return Ok(new { success = true, apiKey });
    }

    // ---------------- Jellyseerr Base Endpoint ----------------
    /// <summary>
    /// Gibt die konfigurierte Jellyseerr Basis-URL zurück.
    /// Nützlich für die Android TV App.
    /// </summary>
    [HttpGet]
    [Route("plugins/requests/apibase")]
    public IActionResult GetApiBase()
    {
        var apiBase = Config.JellyseerrBase;
        if (string.IsNullOrWhiteSpace(apiBase))
        {
            return Ok(new { success = false, message = "Jellyseerr Base URL not configured", apiBase = "" });
        }
        return Ok(new { success = true, apiBase });
    }

    // ---------------- Jellyfin Base Endpoint ----------------
    /// <summary>
    /// Gibt die konfigurierte lokale Jellyfin Basis-URL zurück.
    /// Nützlich für die Android TV App.
    /// </summary>
    [HttpGet]
    [Route("plugins/requests/jellyfinbase")]
    public IActionResult GetJellyfinBase()
    {
        var jellyfinBase = Config.JellyfinBase;
        if (string.IsNullOrWhiteSpace(jellyfinBase))
        {
            return Ok(new { success = false, message = "Jellyfin Base URL not configured", jellyfinBase = "" });
        }
        return Ok(new { success = true, jellyfinBase });
    }

    // ---------------- Jellyseerr Base Endpoint (Tailscale) ----------------
    /// <summary>
    /// Gibt die konfigurierte Jellyseerr Basis-URL für Tailscale zurück.
    /// Hilfreich für die Android TV App im Tailscale-Netz.
    /// </summary>
    [HttpGet]
    [Route("plugins/requests/tailscale/apibase")]
    public IActionResult GetTailscaleApiBase()
    {
        var apiBase = Config.TailscaleJellyseerrBase;
        if (string.IsNullOrWhiteSpace(apiBase))
        {
            return Ok(new { success = false, message = "Tailscale Jellyseerr Base URL not configured", apiBase = "" });
        }
        return Ok(new { success = true, apiBase });
    }

    // ---------------- Jellyfin Base Endpoint (Tailscale) ----------------
    /// <summary>
    /// Gibt die konfigurierte Jellyfin Basis-URL für Tailscale zurück.
    /// Hilfreich für die Android TV App im Tailscale-Netz.
    /// </summary>
    [HttpGet]
    [Route("plugins/requests/tailscale/jellyfinbase")]
    public IActionResult GetTailscaleJellyfinBase()
    {
        var jellyfinBase = Config.TailscaleJellyfinBase;
        if (string.IsNullOrWhiteSpace(jellyfinBase))
        {
            return Ok(new { success = false, message = "Tailscale Jellyfin Base URL not configured", jellyfinBase = "" });
        }
        return Ok(new { success = true, jellyfinBase });
    }

    // ---------------- Jellyfin URL Redirect (Emby-style to Jellyfin-style) ----------------
    /// <summary>
    /// Redirects old Emby/Jellyseerr-style URLs to new Jellyfin URLs.
    /// Jellyseerr generates: /web/index.html#/item?id=xxx&context=home&serverId=xxx
    /// Jellyfin expects:     /web/#/details?id=xxx&serverId=xxx
    /// </summary>
    [HttpGet]
    [Route("web/index.html")]
    public IActionResult RedirectEmbyStyleUrl()
    {
        // The fragment (#/item?...) is not sent to the server, so we return a page
        // that reads the fragment client-side and redirects to the correct URL
        var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Redirecting...</title>
    <script>
        (function() {
            var hash = window.location.hash || '';
            // Check if it's an old-style /item URL
            if (hash.startsWith('#/item?')) {
                var params = new URLSearchParams(hash.substring(7));
                var id = params.get('id');
                var serverId = params.get('serverId');
                if (id) {
                    var newUrl = '/web/#/details?id=' + id;
                    if (serverId) newUrl += '&serverId=' + serverId;
                    window.location.replace(newUrl);
                    return;
                }
            }
            // Fallback: redirect to home
            window.location.replace('/web/');
        })();
    </script>
</head>
<body>
    <p>Redirecting to Jellyfin...</p>
</body>
</html>";
        return Content(html, "text/html");
    }

    // ---------------- UI ----------------
    [HttpGet]
    [Route("plugins/requests/ui")]
    public ContentResult Ui() => Content(@"
        <!doctype html>
        <html><head>
        <meta charset='utf-8'>
        <title>Requests</title>
        <link rel='stylesheet' href='/plugins/requests/assets/requests-bridge.css'>
        </head>
        <body>
        <div class='header-spacer'></div>
        <iframe class='request-frame' src='/plugins/requests/proxy/'></iframe>
        <script src='/plugins/requests/assets/requests-nav.js' defer></script>
        </body></html>", "text/html");

    // ---------------- ROOT-PROXIES ----------------
    [Route("/api/{**path}")]             public Task ProxyApi(string? path)           => ProxyInternal($"api/{path ?? ""}");
    [Route("/_next/{**path}")]           public Task ProxyNext(string? path)          => ProxyInternal($"_next/{path ?? ""}");
    [Route("/assets/{**path}")]          public Task ProxyAssets(string? path)        => ProxyInternal($"assets/{path ?? ""}");
    [Route("/login")]                    public Task ProxyLogin()                     => ProxyInternal("login");
    [Route("/favicon.ico")]              public Task ProxyFavicon()                   => ProxyInternal("favicon.ico");
    [Route("/logo_full.svg")]            public Task ProxyLogoFull()                  => ProxyInternal("logo_full.svg");
    [Route("/avatarproxy/{**path}")]     public Task ProxyAvatar(string? path)        => ProxyInternal($"avatarproxy/{path ?? ""}");
    [Route("/images/{**path}")]          public Task ProxyImages(string? path)        => ProxyInternal($"images/{path ?? ""}");
    [Route("/robots.txt")]               public Task ProxyRobots()                    => ProxyInternal("robots.txt");

    // ---------------- NAMESPACE-PROXY ----------------
    [Route("plugins/requests/proxy/{**path}")]
    public Task ProxyNamespaced(string? path) => ProxyInternal(path ?? "");

    // ---------------- Assets (Embedded Resources) ----------------
    [HttpGet]
    [Route("plugins/requests/assets/{file}")]
    public IActionResult Assets(string file)
    {
        var asm = Assembly.GetExecutingAssembly();

        var expected = $"Jellyfin.Plugin.RequestsBridge.Web.{file}";
        var stream = asm.GetManifestResourceStream(expected);

        if (stream == null)
        {
            var names = asm.GetManifestResourceNames();
            var match = names.FirstOrDefault(n => n.EndsWith("." + file, StringComparison.OrdinalIgnoreCase))
                     ?? names.FirstOrDefault(n => n.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) stream = asm.GetManifestResourceStream(match);
        }

        if (stream == null)
        {
            var names = string.Join(", ", asm.GetManifestResourceNames());
            return NotFound($"Resource '{file}' not found. Embedded: {names}");
        }

        var contentType = file.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ? "application/javascript; charset=utf-8"
                       : file.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ? "text/css; charset=utf-8"
                       : "application/octet-stream";

        return File(stream, contentType);
    }

    // ---------------- Debug ----------------

    // 1) Alle Embedded Resources
    [HttpGet]
    [Route("plugins/requests/debug/resources")]
    public IActionResult DebugResources()
    {
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames();
        return Ok(new { count = names.Length, names });
    }

    // 2) Inhalt der realen Client-index.html anzeigen + Marker-Check
    [HttpGet]
    [Route("plugins/requests/debug/index")]
    public IActionResult DebugIndex()
    {
        const string start = "<!-- REQUESTS_BRIDGE_JS_START -->";
        const string end   = "<!-- REQUESTS_BRIDGE_JS_END -->";
        var candidates = new[]
        {
            "/usr/share/jellyfin/web/index.html",
            "/usr/lib/jellyfin/bin/jellyfin-web/index.html",
            "/usr/share/webapps/jellyfin/web/index.html"
        };

        var found = candidates.FirstOrDefault(System.IO.File.Exists);
        if (found is null) return NotFound(new { path = "(none exists)", hasStart=false, hasEnd=false, snippet="" });

        var html = System.IO.File.ReadAllText(found);
        var hasStart = html.Contains(start, StringComparison.OrdinalIgnoreCase);
        var hasEnd   = html.Contains(end,   StringComparison.OrdinalIgnoreCase);
        var pos = hasStart ? html.IndexOf(start, StringComparison.OrdinalIgnoreCase) : -1;
        var snippet = pos >= 0 ? html.Substring(Math.Max(0, pos - 200), Math.Min(500, html.Length - Math.Max(0, pos - 200))) : "";
        return Ok(new { path = found, hasStart, hasEnd, snippet });
    }

    // 3) Schreibprobe neben index.html (zeigt dir, ob Mount/Permission stimmt)
    [HttpGet]
    [Route("plugins/requests/debug/perm")]
    public IActionResult DebugPerm()
    {
        var candidates = new[]
        {
            "/usr/share/jellyfin/web/index.html",
            "/usr/lib/jellyfin/bin/jellyfin-web/index.html",
            "/usr/share/webapps/jellyfin/web/index.html"
        };
        var idx = candidates.FirstOrDefault(System.IO.File.Exists);
        if (idx is null) return NotFound(new { ok=false, path="(none exists)" });

        try
        {
            var dir = System.IO.Path.GetDirectoryName(idx)!;
            var testFile = System.IO.Path.Combine(dir, "requestsbridge-perm-test.txt");
            System.IO.File.WriteAllText(testFile, DateTime.UtcNow.ToString("O"));
            return Ok(new { ok=true, wrote=testFile });
        }
        catch (Exception ex)
        {
            return Problem($"write failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ---------------- Proxy-Kern ----------------
    private async Task ProxyInternal(string path)
    {
        var upstreamBase = (Config.JellyseerrBase ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(upstreamBase))
        {
            Response.StatusCode = 500;
            await Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("JellyseerrBase not configured."));
            return;
        }

        path = (path is null or "/" ? string.Empty : path).TrimStart('/');

        var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var start = new Uri($"{upstreamBase}/{path}{query}");

        using var upstreamResp = await FetchFollowingRedirectsAsync(start);

        Response.StatusCode = (int)upstreamResp.StatusCode;

        if (upstreamResp.Content.Headers.ContentType is { } ct)
            Response.ContentType = ct.ToString();

        var skip = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Transfer-Encoding","Content-Length","Connection","Keep-Alive",
            "Proxy-Authenticate","Proxy-Authorization","TE","Trailer","Upgrade",
            "Location","Content-Encoding"
        };

        foreach (var (k, v) in upstreamResp.Headers)
            if (!skip.Contains(k)) Response.Headers[k] = v.ToArray();
        foreach (var (k, v) in upstreamResp.Content.Headers)
            if (!skip.Contains(k)) Response.Headers[k] = v.ToArray();

        Response.Headers.Remove("X-Frame-Options");
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
        Response.Headers.Remove("Content-Encoding");

        if (Response.Headers.TryGetValue("Set-Cookie", out var setCookies))
        {
            var list = new List<string>();
            foreach (var raw in setCookies)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                var c = raw;
                if (!string.Equals(Request.Scheme, "https", System.StringComparison.OrdinalIgnoreCase))
                {
                    c = c.Replace("SameSite=None", "SameSite=Lax", System.StringComparison.OrdinalIgnoreCase)
                         .Replace("; Secure", "", System.StringComparison.OrdinalIgnoreCase);
                }
                list.Add(c);
            }
            if (list.Count > 0) Response.Headers["Set-Cookie"] = list.ToArray();
            else Response.Headers.Remove("Set-Cookie");
        }

        var isHtml = upstreamResp.Content.Headers.ContentType?.MediaType?.Contains("html", System.StringComparison.OrdinalIgnoreCase) == true;
        var isRootLike = string.IsNullOrEmpty(path) || path == "index.html";
        if (isHtml && isRootLike && upstreamResp.StatusCode == HttpStatusCode.OK)
        {
            var html = await upstreamResp.Content.ReadAsStringAsync();
            const string baseTag = "<base href=\"/plugins/requests/proxy/\">";
            if (!html.Contains("<base", System.StringComparison.OrdinalIgnoreCase))
                html = html.Replace("<head>", "<head>" + baseTag, System.StringComparison.OrdinalIgnoreCase);

            Response.ContentType = "text/html; charset=utf-8";
            await Response.BodyWriter.WriteAsync(System.Text.Encoding.UTF8.GetBytes(html));
            return;
        }

        await upstreamResp.Content.CopyToAsync(Response.Body);
    }

    private async Task<HttpResponseMessage> FetchFollowingRedirectsAsync(Uri start)
    {
        var client = _httpFactory.CreateClient();
        var current = start;
        const int maxHops = 10;

        for (var i = 0; i < maxHops; i++)
        {
            using var req = new HttpRequestMessage(new HttpMethod(Request.Method), current);

            req.Headers.Remove("Accept-Encoding");
            req.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");

            req.Headers.TryAddWithoutValidation("X-Forwarded-Proto", Request.Scheme);
            req.Headers.TryAddWithoutValidation("X-Forwarded-Host", Request.Host.Value);
            req.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", "/plugins/requests/proxy");

            foreach (var (k, v) in Request.Headers)
            {
                if (k.Equals("Host", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (k.StartsWith("Content-", System.StringComparison.OrdinalIgnoreCase)) continue;
                req.Headers.TryAddWithoutValidation(k, (IEnumerable<string>)v);
            }

            if (i == 0 && Request.ContentLength > 0)
            {
                req.Content = new StreamContent(Request.Body);
                if (Request.ContentType is not null)
                    req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            }

            var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);

            var code = (int)resp.StatusCode;
            if (code is 301 or 302 or 303 or 307 or 308)
            {
                var loc = resp.Headers.Location?.ToString();
                if (string.IsNullOrWhiteSpace(loc)) return resp;

                if (!Uri.TryCreate(loc, UriKind.RelativeOrAbsolute, out var next)) return resp;
                if (!next.IsAbsoluteUri) next = new Uri(current, next);
                if (next == current) return resp;

                current = next;
                resp.Dispose();
                continue;
            }

            return resp;
        }

        return await _httpFactory.CreateClient().GetAsync(current, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
    }
}
