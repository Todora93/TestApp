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
    using System.Net.Http;
    using MyActorService.Interfaces;
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using System;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.HttpOverrides;
    using System.ComponentModel;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WebService : StatelessService, IWebService
    {
        public WebService(StatelessServiceContext context)
            : base(context) 
        {
        }

        #region Communication

        #region Actor -> Client

        public async Task StartGame(ActorInfo actorInfo, List<UserRequest> users)
        {
            var mLong = actorInfo.ActorId.GetLongId();
            var index = actorInfo.ActorIndex;

            var connectionIds = GetConnectionIds(users[0]);
            await Startup.hubContext.Clients.Clients(connectionIds).StartGame(mLong.ToString(), index, 0, users[0].UserName, users[1].UserName);

            connectionIds = GetConnectionIds(users[1]);
            await Startup.hubContext.Clients.Clients(connectionIds).StartGame(mLong.ToString(), index, 1, users[0].UserName, users[1].UserName);
        }

        public async Task GameStateChanged(GameState gameState)
        {
            var connectionIds = GetConnectionIds(gameState);
            await Startup.hubContext.Clients.Clients(connectionIds).State(gameState.ToString());

            //await Startup.hubContext.Clients.All.SendAsync("ReceiveGameState",$"[STATE]: {gameState.ToString()}");
        }

        public async Task MatchFinished(GameState finalState)
        {
            var connectionIds = GetConnectionIds(finalState);
            await Startup.hubContext.Clients.Clients(connectionIds).EndGame(finalState.ToString());
        }

        #endregion

        #region Client -> Actor

        public async Task SendInput(UserRequest user, ActorInfo actorInfo, UserInput input)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            await simulationActor.ApplyInput(user, input);
        }

        public async Task<UserRequest> GetOpponent(UserRequest user, ActorInfo actorInfo)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            return await simulationActor.GetOpponent(user);
        }

        public async Task<GameState> GetGameState(UserRequest user, ActorInfo actorInfo)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            return await simulationActor.GetGameState(user);
        }

        public async Task UpdateMove(UserRequest user, ActorInfo actorInfo, string move) 
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            await simulationActor.UpdateMove(user, actorInfo, move);
        }

        public async Task UpdateLife(UserRequest user, ActorInfo actorInfo, int life)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            await simulationActor.UpdateLife(user, actorInfo, life);
        }

        public async Task UpdatePosition(UserRequest user, ActorInfo actorInfo, int posX, int posY)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            await simulationActor.UpdatePosition(user, actorInfo, posX, posY);
        }

        //public async Task UpdatePlayerState(UserRequest user, ActorId actorId, string move, int life, int posX, int posY)
        //{
        //    string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService";
        //    ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorId, new Uri(actorServiceUri));

        //    await simulationActor.UpdatePlayerState(user, move, life, posX, posY);
        //}
             
        public async Task<GameState> FighterDead(UserRequest user, ActorInfo actorInfo)
        {
            string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSuffix(actorInfo.ActorIndex)}";
            ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorInfo.ActorId, new Uri(actorServiceUri));

            return await simulationActor.FighterDead(user);
        }

        #endregion

        #endregion

        #region Overriden

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

        #endregion

        #region Helpers

        private List<string> GetConnectionIds(GameState gameState)
        {
            var connectionIds = new List<string>();
            foreach (var user in gameState.State)
            {
                connectionIds.AddRange(GetConnectionIds(user.User));
            }
            return connectionIds;
        }

        private List<string> GetConnectionIds(UserRequest user)
        {
            return Startup.userConnectionManager.GetUserConnections(user.UserName);
        }

        private string GetSuffix(int index)
        {
            return index == 0 ? "" : $"{index}";
        }

        #endregion
    }
}
