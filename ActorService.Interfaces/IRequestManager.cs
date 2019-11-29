﻿namespace MyActorService.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IRequestManager : IService
    {
        // todo delete
        //Task<bool> GetActiveActorId(ActorId actorId);

        Task<ExistingMatch> AddRequestAsync(UserRequest user);

        Task<string> GetAllRequests();

        Task DeleteAllRequests();

        Task DeleteAll();

        Task Matchmake(CancellationToken cancellationToken);
    }
}
