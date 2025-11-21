using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RequestsBridge
{
    /// <summary>
    /// Automatischer Start nach Server-Boot, patcht die echte Client-Index mit Retries.
    /// Implementiert IHostedService für Jellyfin 10.9+
    /// </summary>
    public sealed class RequestsBridgeEntryPoint : IHostedService, IDisposable
    {
        private readonly ILogger<RequestsBridgeEntryPoint> _log;
        private Task? _patchTask;
        private CancellationTokenSource? _cts;

        private const string ScriptUrl   = "/plugins/requests/assets/requests-implementation.js";
        private const string MarkerStart = "<!-- REQUESTS_BRIDGE_JS_START -->";
        private const string MarkerEnd   = "<!-- REQUESTS_BRIDGE_JS_END -->";

        // Bekannte Pfade für verschiedene Docker-Images und Installationen
        private static readonly string[] IndexPaths =
        {
            // binhex-jellyfin
            "/usr/share/jellyfin/web/index.html",
            // linuxserver/jellyfin
            "/usr/lib/jellyfin/bin/jellyfin-web/index.html",
            // Offizielle Jellyfin Docker
            "/usr/share/webapps/jellyfin/web/index.html",
            // Native Linux Installation
            "/usr/share/jellyfin-web/index.html",
            // Windows Installation (typisch)
            @"C:\Program Files\Jellyfin\Server\jellyfin-web\index.html",
            @"C:\ProgramData\Jellyfin\Server\jellyfin-web\index.html"
        };

        public RequestsBridgeEntryPoint(ILogger<RequestsBridgeEntryPoint> log)
        {
            _log = log;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _patchTask = Task.Run(() => PatchWithRetries(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                if (_patchTask != null)
                {
                    try { await _patchTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                }
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }

        private void PatchWithRetries(CancellationToken ct)
        {
            const int maxAttempts = 60; // 30 Sekunden
            const int delayMs = 500;

            for (int i = 1; i <= maxAttempts && !ct.IsCancellationRequested; i++)
            {
                try
                {
                    if (TryPatchOnce(out var path))
                    {
                        _log.LogInformation("RequestsBridge: Script-Tag erfolgreich in {Path} injiziert", path);
                        return;
                    }

                    if (i % 10 == 0) // Alle 5 Sekunden loggen
                    {
                        _log.LogDebug("RequestsBridge: Versuch {Attempt}/{Max} - warte auf index.html", i, maxAttempts);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "RequestsBridge: Versuch {Attempt}/{Max} fehlgeschlagen", i, maxAttempts);
                }

                try { Task.Delay(delayMs, ct).Wait(ct); }
                catch (OperationCanceledException) { return; }
            }

            _log.LogWarning("RequestsBridge: Konnte index.html nicht automatisch patchen. " +
                           "Das Plugin funktioniert trotzdem, aber der 'Wünsche'-Button wird nicht angezeigt. " +
                           "Manuelles Patchen: Script-Tag in index.html einfügen oder Container mit beschreibbarem /jellyfin-web Volume starten.");
        }

        private bool TryPatchOnce(out string? patchedPath)
        {
            patchedPath = null;

            foreach (var idx in IndexPaths)
            {
                if (TryPatchFile(idx))
                {
                    patchedPath = idx;
                    return true;
                }
            }

            return false;
        }

        private bool TryPatchFile(string idx)
        {
            try
            {
                if (!File.Exists(idx))
                {
                    return false;
                }

                // Prüfe Schreibrechte
                if (!IsWritable(idx))
                {
                    _log.LogTrace("RequestsBridge: {Path} nicht beschreibbar (Rechte-Problem)", idx);
                    return false;
                }

                var html = File.ReadAllText(idx, Encoding.UTF8);

                // Bereits vorhanden?
                if (html.Contains(MarkerStart, StringComparison.OrdinalIgnoreCase) &&
                    html.Contains(ScriptUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _log.LogDebug("RequestsBridge: Script bereits in {Path} vorhanden", idx);
                    return true;
                }

                // Script-Block erstellen
                var block = $"\n{MarkerStart}\n<script src=\"{ScriptUrl}\" defer></script>\n{MarkerEnd}\n";

                // Einfügen
                string patched;
                var headPos = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
                var bodyEndPos = html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);

                if (headPos >= 0)
                {
                    patched = html.Insert(headPos + 6, block);
                }
                else if (bodyEndPos >= 0)
                {
                    patched = html.Insert(bodyEndPos, block);
                }
                else
                {
                    patched = html + block;
                }

                File.WriteAllText(idx, patched, new UTF8Encoding(false));
                _log.LogInformation("RequestsBridge: Script erfolgreich in {Path} eingefügt", idx);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Keine Rechte - normal bei read-only Containern
                return false;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "RequestsBridge: Fehler beim Patchen von {Path}", idx);
                return false;
            }
        }

        private static bool IsWritable(string path)
        {
            try
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
