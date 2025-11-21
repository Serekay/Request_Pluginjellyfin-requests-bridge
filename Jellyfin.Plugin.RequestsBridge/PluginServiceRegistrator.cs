using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.Plugin.RequestsBridge
{
    /// <summary>
    /// Registriert Plugin-Services bei Jellyfin.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
        {
            // Registriert den EntryPoint als Hosted Service
            services.AddHostedService<RequestsBridgeEntryPoint>();
        }
    }
}
