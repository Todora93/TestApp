using MyActorService.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using Microsoft.ServiceFabric.Actors;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace WebService
{
    public class MyHub : Hub<IMyHub>
    {
        private readonly IUserConnectionManager _userConnectionManager;
        private readonly IWebService _webService;
        private readonly StatelessServiceContext serviceContext;
        private readonly FabricClient fabricClient;

        public MyHub(IUserConnectionManager userConnectionManager, IWebService webService, StatelessServiceContext serviceContext, FabricClient fabricClient)
        {
            _userConnectionManager = userConnectionManager;
            _webService = webService;
            this.serviceContext = serviceContext;
            this.fabricClient = fabricClient;
        }

        

        //public async Task SendInput(string userName, string actorId, bool[] input)
        //{
        //    var user = new UserRequest(userName);
        //    var userInput = new UserInput(input);
        //    var actor = new ActorId(Int64.Parse(actorId));

        //    await _webService.SendInput(user, actor, userInput);
        //}

        #region Client -> Web

        public string GetConnectionId()
        {
            var httpContext = this.Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"];
            _userConnectionManager.KeepUserConnection(userId, Context.ConnectionId);

            return Context.ConnectionId;
        }

        public async Task AddRequest(string userName)
        {
            var user = new UserRequest(userName);

            string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/RequestsService";

            ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

            if (partitions == null || partitions.Count == 0)
            {
                ServiceEventSource.Current.Message($"There's no available replica. Please check service status.");
                return;
            }

            ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)(partitions[0].PartitionInformation)).LowKey);

            IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

            var assignedActor = await service.AddRequestAsync(user);
            if (assignedActor.HasMatch)
            {
                var gameState = await _webService.GetGameState(user, assignedActor.ActorId);

                var connectionIds = _userConnectionManager.GetUserConnections(user.UserName);
                await Clients.Clients(connectionIds).ReconnectToGame(assignedActor.ActorId.ToString(), gameState.GetPlayerIndex(user), gameState);
            }
        }

        // todo: check if move as string is valid
        public async Task MoveUpdate(string userName, string actorId, string move)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.UpdateMove(user, actor, move);

            var opponent = await _webService.GetOpponent(user, actor);
            var connectionIds = _userConnectionManager.GetUserConnections(opponent.UserName);

            await Clients.Clients(connectionIds).ReceiveMove(move);
        }

        public async Task LifeUpdate(string userName, string actorId, int life)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.UpdateLife(user, actor, life);

            var opponent = await _webService.GetOpponent(user, actor);
            var connectionIds = _userConnectionManager.GetUserConnections(opponent.UserName);

            await Clients.Clients(connectionIds).LifeUpdate(life);
        }

        public async Task PositionUpdate(string userName, string actorId, int x, int y)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.UpdatePosition(user, actor, x, y);

            var opponent = await _webService.GetOpponent(user, actor);
            var connectionIds = _userConnectionManager.GetUserConnections(opponent.UserName);

            await Clients.Clients(connectionIds).PositionUpdate(x, y);
        }

        public async Task FighterDead(string userName, string actorId)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.FighterDead(user, actor);
        }

        #endregion

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            //get the connectionId
            var connectionId = Context.ConnectionId;
            _userConnectionManager.RemoveUserConnection(connectionId);
            var value = await Task.FromResult(0);//adding dump code to follow the template of Hub > OnDisconnectedAsync
        }

        private List<string> GetConnectionIds(List<UserRequest> users)
        {
            var connectionIds = new List<string>();
            foreach(var user in users)
            {
                connectionIds.AddRange(_userConnectionManager.GetUserConnections(user.UserName));
            }
            return connectionIds;
        }
    }
}
