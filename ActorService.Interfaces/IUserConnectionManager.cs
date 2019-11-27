using System;
using System.Collections.Generic;
using System.Text;

namespace MyActorService.Interfaces
{
    public interface IUserConnectionManager
    {
        void KeepUserConnection(string userName, string connectionId);
        void RemoveUserConnection(string connectionId);
        List<string> GetUserConnections(string userName);
    }
}
