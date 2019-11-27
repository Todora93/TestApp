using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyActorService.Interfaces
{
    public interface IWebService : IService
    {
        // Actor -> Client

        public Task StartGame(ActorId actorId, List<UserRequest> users);

        public Task GameStateChanged(GameState gameState);

        public Task MatchFinished(GameState gameState);

        // Client -> Actor

        public Task SendInput(UserRequest user, ActorId actorId, UserInput input);

        public Task<UserRequest> GetOpponent(UserRequest user, ActorId actorId);

        public Task<GameState> FighterDead(UserRequest user, ActorId actorId);
    }
}
