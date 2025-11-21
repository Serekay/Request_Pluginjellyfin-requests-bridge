using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RequestsBridge
{
    /// <summary>
    /// Plugin-Hauptklasse. Injiziert beim Laden einen Script-Tag in die ausgelieferte Web-UI.
    /// Das Script verweist auf dein Embedded-Asset: /plugins/requests/assets/requests-implementation.js
    /// </summary>
    public sealed class RequestsBridgePlugin : BasePlugin<BridgeConfig>, IHasWebPages
    {
        // Feste GUID – beibehalten
        public static readonly Guid PluginGuid = Guid.Parse("1b7a3e7c-7c7c-43e0-9af0-7c7e8a2f2c01");

        public override string Name => "Requests (Jellyseerr Bridge)";
        public override string Description => "Embed Jellyseerr via internal proxy + inject global JS";
        public override Guid Id => PluginGuid;

        public static RequestsBridgePlugin? Instance { get; private set; }

        private readonly ILogger<RequestsBridgePlugin> _logger;

        public RequestsBridgePlugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            ILogger<RequestsBridgePlugin> logger,
            IServerConfigurationManager configurationManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _logger = logger;

            try
            {
                InjectScriptIntoIndexHtml(applicationPaths, configurationManager);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestsBridge: exception while trying to inject client script");
            }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Web.configPage.html"
                }
            };
        }

        /// <summary>
        /// Sucht die ausgelieferte index.html und injiziert (oder aktualisiert) den Script-Tag.
        /// Logik entspricht dem bereitgestellten Repo, nur dass wir auf unser Asset verlinken.
        /// </summary>
        private void InjectScriptIntoIndexHtml(IApplicationPaths applicationPaths, IServerConfigurationManager configurationManager)
        {
            // 1) index.html finden
            if (string.IsNullOrWhiteSpace(applicationPaths.WebPath))
            {
                _logger.LogWarning("RequestsBridge: applicationPaths.WebPath is empty – cannot inject");
                return;
            }

            var indexFile = Path.Combine(applicationPaths.WebPath, "index.html");
            if (!File.Exists(indexFile))
            {
                _logger.LogWarning("RequestsBridge: index.html not found at {Path}", indexFile);
                return;
            }

            // 2) BaseUrl (BasePath) aus Netzwerk-Konfig (wie im Repo)
            string basePath = "";
            try
            {
                var networkConfig = configurationManager.GetConfiguration("network");
                var configType = networkConfig.GetType();
                var basePathProp = configType.GetProperty("BaseUrl");
                var confBasePath = basePathProp?.GetValue(networkConfig)?.ToString()?.Trim('/');

                if (!string.IsNullOrEmpty(confBasePath))
                    basePath = "/" + confBasePath;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RequestsBridge: unable to get BaseUrl from network config, falling back to '/'");
            }

            // 3) Script-Tag (wie im Repo, aber mit plugin="RequestsBridge" und src auf dein Asset)
            //    Wir nutzen 'defer', damit es zur Jellyfin-UI passt.
            var scriptSrc = $"{basePath}/plugins/requests/assets/requests-implementation.js";
            var scriptElement =
                $"<script plugin=\"RequestsBridge\" defer=\"defer\" src=\"{scriptSrc}\"></script>";

            // 4) Inhalt laden
            string indexContents;
            try
            {
                indexContents = File.ReadAllText(indexFile, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestsBridge: cannot read {IndexFile}", indexFile);
                return;
            }

            // 5) Falls derselbe Plugin-Block schon existiert: erst entfernen (wie im Repo mit Regex)
            var scriptReplace = "<script plugin=\"RequestsBridge\".*?</script>";
            indexContents = Regex.Replace(indexContents, scriptReplace, "", RegexOptions.Singleline);

            // 6) Prüfen, ob exakt unser Tag bereits enthalten ist (dann nichts zu tun)
            if (indexContents.Contains(scriptElement, StringComparison.Ordinal))
            {
                _logger.LogInformation("RequestsBridge: client script already present in {IndexFile}", indexFile);
                return;
            }

            _logger.LogInformation("RequestsBridge: injecting client script into {IndexFile}", indexFile);
            _logger.LogDebug("RequestsBridge: <script> = {Script}", scriptElement);

            // 7) Vor </body> einfügen
            var bodyClosing = indexContents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyClosing == -1)
            {
                _logger.LogWarning("RequestsBridge: could not find closing </body> in {IndexFile}", indexFile);
                return;
            }

            var patched = indexContents.Insert(bodyClosing, scriptElement);

            // 8) Schreiben
            try
            {
                File.WriteAllText(indexFile, patched, new UTF8Encoding(false));
                _logger.LogInformation("RequestsBridge: finished injecting client script in {IndexFile}", indexFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestsBridge: error writing to {IndexFile}", indexFile);
            }
        }
    }

    public sealed class BridgeConfig : BasePluginConfiguration
    {
        /// <summary>
        /// Die Basis-URL der Jellyseerr-Instanz.
        /// </summary>
        public string JellyseerrBase { get; set; } = "http://192.168.178.37:5055";

        /// <summary>
        /// Der Jellyseerr API-Key für Authentifizierung.
        /// Kann über den Endpoint /plugins/requests/apikey abgerufen werden.
        /// </summary>
        public string JellyseerrApiKey { get; set; } = string.Empty;
    }
}
