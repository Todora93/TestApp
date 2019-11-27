using MyActorService.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using Microsoft.ServiceFabric.Actors;
using System.Globalization;
using System.Collections.Generic;

namespace WebService
{
    public class MyHub : Hub
    {
        private readonly IUserConnectionManager _userConnectionManager;
        private readonly IWebService _webService;

        public MyHub(IUserConnectionManager userConnectionManager, IWebService webService)
        {
            _userConnectionManager = userConnectionManager;
            _webService = webService;
        }

        public async Task SendInput(string userName, string actorId, bool[] input)
        {
            var user = new UserRequest(userName);
            var userInput = new UserInput(input);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.SendInput(user, actor, userInput);
        }

        public async Task SendMove(string userName, string actorId, object move)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            var opponent = await _webService.GetOpponent(user, actor);
            var connectionIds = _userConnectionManager.GetUserConnections(opponent.UserName);

            await Clients.Clients(connectionIds).SendAsync("ReceiveMove", move);
        }

        public async Task LifeUpdate(string userName, string actorId, int life)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            var opponent = await _webService.GetOpponent(user, actor);
            var connectionIds = _userConnectionManager.GetUserConnections(opponent.UserName);

            await Clients.Clients(connectionIds).SendAsync("LifeUpdate", life);
        }

        public async Task PositionUpdate(string userName, string actorId, int x, int y)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            var opponent = await _webService.GetOpponent(user, actor);
            var connectionIds = _userConnectionManager.GetUserConnections(opponent.UserName);

            await Clients.Clients(connectionIds).SendAsync("PositionUpdate", x, y);
        }

        public async Task FighterDead(string userName, string actorId)
        {
            var user = new UserRequest(userName);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.FighterDead(user, actor);
        }

        public string GetConnectionId()
        {
            var httpContext = this.Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"];
            _userConnectionManager.KeepUserConnection(userId, Context.ConnectionId);

            return Context.ConnectionId;
        }

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
