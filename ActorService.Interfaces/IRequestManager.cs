namespace MyActorService.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IRequestManager : IService
    {
        Task AddRequestAsync(UserRequest guid);

        Task<string> GetAllRequests();

        Task DeleteAllRequests();

        Task Matchmake(CancellationToken cancellationToken);
    }
}
