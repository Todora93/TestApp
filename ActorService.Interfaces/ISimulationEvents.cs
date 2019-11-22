namespace MyActorService.Interfaces
{
    using Microsoft.ServiceFabric.Actors;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public interface ISimulationEvents : IActorEvents
    {
        public void StateUpdated(ActorId actorId, GameState gameState);

        public void MatchFinished(ActorId actorId, GameState finalGameState);
    }
}
