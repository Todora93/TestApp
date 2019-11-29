//[assembly: FabricTransportActorRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace MyActorService.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface ISimulationActor : IActor, IActorEventPublisher<ISimulationEvents>
    {
        public Task SimulateMatch(List<UserRequest> players);

        public Task ApplyInput(UserRequest user, UserInput input);

        public Task<UserRequest> GetOpponent(UserRequest user);

        public Task<GameState> GetGameState(UserRequest user);

        public Task UpdateMove(UserRequest user, ActorId actorId, string move);

        public Task UpdateLife(UserRequest user, ActorId actorId, int life);

        public Task UpdatePosition(UserRequest user, ActorId actorId, int posX, int posY);

        //public Task UpdatePlayerState(UserRequest user, ActorId actorId, string move, int life, int posX, int posY);

        public Task<GameState> FighterDead(UserRequest user);
    }
}
