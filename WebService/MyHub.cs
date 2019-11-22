using Microsoft.AspNetCore.SignalR;
using MyActorService.Interfaces;
using System.Threading.Tasks;

namespace WebService
{
    public class MyHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        //public async Task SendState(GameState gameState)
        //{
        //    await Clients.All.SendAsync("ReceiveGameState", gameState.ToString());
        //}
    }
}
