using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyActorService.Interfaces
{
    public interface IWebService : IService
    {
        public Task StartGame(ActorId actorId, List<UserRequest> users);

        public Task GameStateChanged(GameState gameState);

        public Task MatchFinished(GameState gameState);

        public Task SendInput(UserRequest user, ActorId actorId, UserInput input);
    }
}
