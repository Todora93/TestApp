namespace MyActorService.Interfaces
{
    using Microsoft.ServiceFabric.Actors;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public interface ISimulationEvents : IActorEvents
    {
        public void StateUpdated(ActorInfo actorId, GameState gameState);

        public void MatchFinished(ActorInfo actorId, GameState finalGameState);
    }
}
