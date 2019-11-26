using MyActorService.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using Microsoft.ServiceFabric.Actors;
using System.Globalization;

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

        // todo delete
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // todo delete
        public async Task SendInput(string user, string message)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", message);
        }

        public async Task SendInputWithParams(string userId, string actorId, int input)
        {
            var userRequest = new UserRequest() { UserId = Int64.Parse(userId) };
            var userInput = new UserInput(input);
            var actor = new ActorId(Int64.Parse(actorId));

            await _webService.SendInput(userRequest, actor, userInput);
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
    }
}
