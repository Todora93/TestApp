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
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using System;
    using System.Fabric.Query;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.AspNetCore.Components.Forms;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WebService : Microsoft.ServiceFabric.Services.Runtime.StatelessService, IWebService
    {
        public WebService(StatelessServiceContext context)
            : base(context) { }

        #region Communication with simulation

        public async Task StartGame(ActorId actorId, List<UserRequest> users)
        {
            var connectionIds = GetConnectionIds(users);
            var mLong = actorId.GetLongId();
            var mPartition = actorId.GetPartitionKey();

            await Startup.hubContext.Clients.Clients(connectionIds).SendAsync("GameStarted", mLong.ToString(), mPartition.ToString());
        }

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

        public async Task SendInput(UserRequest user, ActorId actorId, UserInput input)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorId, new Uri(actorServiceUri));

            string requestsServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/RequestsService";

            ServicePartitionList partitions = await Startup.fabricClient.QueryManager.GetPartitionListAsync(new Uri(requestsServiceUri));
            ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)partitions[0].PartitionInformation).LowKey);
            IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(requestsServiceUri), key);

            bool isActiveActor = await service.GetActiveActorId(actorId);
            if (!isActiveActor)
            {
                ServiceEventSource.Current.Message($"Actor with id {actorId.ToString()} is not active actor.");
                return;
            }

            await simulationActor.ApplyInput(user, input);
        }

        #endregion

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
                connectionIds.AddRange(GetConnectionIds(user.User));
            }
            return connectionIds;
        }

        private List<string> GetConnectionIds(List<UserRequest> users)
        {
            var connectionIds = new List<string>();
            foreach (var user in users)
            {
                connectionIds.AddRange(GetConnectionIds(user));
            }
            return connectionIds;
        }

        private List<string> GetConnectionIds(UserRequest user)
        {
            return Startup.userConnectionManager.GetUserConnections(user.UserId.ToString());
        }
    }
}
