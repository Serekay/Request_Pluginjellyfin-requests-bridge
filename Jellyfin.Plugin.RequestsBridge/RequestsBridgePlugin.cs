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
    public sealed class RequestsBridgePlugin : BasePlugin<BridgeConfig>, IHasWebPages
    {
        public static readonly Guid PluginGuid = Guid.Parse("1b7a3e7c-7c7c-43e0-9af0-7c7e8a2f2c01");

        public override string Name => "Requests Bridge";
        public override string Description => "Jellyseerr integration plugin for Jellyfin, if you installed my Jellyfin Android TV app (see GitHub instrucctions) you got the same functionality built-in for Android TV devices.";
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

        private void InjectScriptIntoIndexHtml(IApplicationPaths applicationPaths, IServerConfigurationManager configurationManager)
        {
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

            var scriptSrc = $"{basePath}/plugins/requests/assets/requests-implementation.js";
            var scriptElement =
                $"<script plugin=\"RequestsBridge\" defer=\"defer\" src=\"{scriptSrc}\"></script>";

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

            var scriptReplace = "<script plugin=\"RequestsBridge\".*?</script>";
            indexContents = Regex.Replace(indexContents, scriptReplace, "", RegexOptions.Singleline);

            if (indexContents.Contains(scriptElement, StringComparison.Ordinal))
            {
                _logger.LogInformation("RequestsBridge: client script already present in {IndexFile}", indexFile);
                return;
            }

            _logger.LogInformation("RequestsBridge: injecting client script into {IndexFile}", indexFile);
            _logger.LogDebug("RequestsBridge: <script> = {Script}", scriptElement);

            var bodyClosing = indexContents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyClosing == -1)
            {
                _logger.LogWarning("RequestsBridge: could not find closing </body> in {IndexFile}", indexFile);
                return;
            }

            var patched = indexContents.Insert(bodyClosing, scriptElement);

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
        public string JellyseerrBase { get; set; } = "http://192.168.178.37:5055";

        public string TailscaleJellyseerrBase { get; set; } = string.Empty;

        public string JellyseerrApiKey { get; set; } = string.Empty;

        /// Disable User Data for collections to fix slow loading issues.
        public bool DisableUserDataOnCollections { get; set; } = false;
    }
}
