using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyActorService.Interfaces
{
    public interface IWebService : IService
    {
        // Actor -> Client

        public Task StartGame(ActorInfo actorInfo, List<UserRequest> users);

        public Task GameStateChanged(GameState gameState);

        public Task MatchFinished(GameState gameState);

        // Client -> Actor

        //public Task SendInput(UserRequest user, ActorId actorId, UserInput input);

        public Task UpdateMove(UserRequest user, ActorInfo actorId, string move);

        public Task UpdateLife(UserRequest user, ActorInfo actorId, int life);

        public Task UpdatePosition(UserRequest user, ActorInfo actorId, int posX, int posY);

        //public Task UpdatePlayerState(UserRequest user, ActorId actorId, string move, int life, int posX, int posY);

        public Task<UserRequest> GetOpponent(UserRequest user, ActorInfo actorId);

        public Task<GameState> GetGameState(UserRequest user, ActorInfo actorId);

        public Task<GameState> FighterDead(UserRequest user, ActorInfo actorId);
    }
}
