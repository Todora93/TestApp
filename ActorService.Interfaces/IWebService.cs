using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace MyActorService.Interfaces
{
    public interface IWebService : IService
    {
        public Task GameStateChanged(GameState gameState);

        public Task MatchFinished(GameState gameState);
    }
}
