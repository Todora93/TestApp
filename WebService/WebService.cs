namespace WebService
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Net.Http;
    using MyActorService.Interfaces;
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WebService : StatelessService, IWebService
    {
        public WebService(StatelessServiceContext context)
            : base(context) { }

        public async Task GameStateChanged(GameState gameState)
        {
            var connectionIds = GetConnectionIds(gameState);
            await Startup.hubContext.Clients.Clients(connectionIds).SendAsync("ReceiveGameState", $"[STATE]: {gameState.ToString()}");

            //await Startup.hubContext.Clients.All.SendAsync("ReceiveGameState",$"[STATE]: {gameState.ToString()}");
        }

        public async Task MatchFinished(GameState finalState)
        {
            var connectionIds = GetConnectionIds(finalState);
            await Startup.hubContext.Clients.Clients(connectionIds).SendAsync("GameFinished", $"[MATCH ENDED]: {finalState.ToString()}");
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            var listeners = new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatelessServiceContext>(serviceContext)
                                            .AddSingleton<FabricClient>(new FabricClient())
                                            .AddSingleton<HttpClient>(new HttpClient())
                                            .AddSingleton<ConfigSettings>(new ConfigSettings(serviceContext)))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    })),
            };

            return listeners.Concat(this.CreateServiceRemotingInstanceListeners());
        }

        private List<string> GetConnectionIds(GameState gameState)
        {
            var connectionIds = new List<string>();
            foreach (var user in gameState.State)
            {
                connectionIds.AddRange(Startup.userConnectionManager.GetUserConnections(user.User.UserId.ToString()));
            }
            return connectionIds;
        }
    }
}
