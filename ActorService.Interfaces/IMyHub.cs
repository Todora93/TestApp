using System.Globalization;
using System.Threading.Tasks;

namespace MyActorService.Interfaces
{
    public interface IMyHub
    {
        Task StartGame(string actorId, int actorIndex, int playerIndex, string player1Name, string player2Name);

        Task ReconnectToGame(string actorId, int actorIndex, int playerIndex, GameState gameState);

        Task State(string gameState);

        Task EndGame(string gameState);

        Task ReceiveMove(object move);

        Task LifeUpdate(int life);

        Task PositionUpdate(int x, int y);
    }
}
