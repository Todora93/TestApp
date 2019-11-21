namespace MyActorService.Interfaces
{
    using Microsoft.ServiceFabric.Actors;

    public interface ISimulationEvents : IActorEvents
    {
        void StateUpdated(ActorId actorId, GameState gameState);

        void MatchFinished(ActorId actorId, GameState finalGameState);
    }
}
