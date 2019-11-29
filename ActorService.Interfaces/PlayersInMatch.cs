namespace MyActorService.Interfaces
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Runtime.Serialization;

    [DataContract(Name = "PlayersInMatch", Namespace = "SimulationActor.Interfaces")]
    public sealed class PlayersInMatch
    {
        private static readonly IEnumerable<UserRequest> NoUserRequest = ImmutableList<UserRequest>.Empty;

        [DataMember]
        public IEnumerable<UserRequest> Players { get; private set; }

        public int Count => ((ImmutableList<UserRequest>)Players).Count;

        public PlayersInMatch(IEnumerable<UserRequest> users = null)
        {
            Players = users == null ? NoUserRequest : users.ToImmutableList();
        }

        public void OnDeserialized(StreamingContext context)
        {
            Players = Players.ToImmutableList();
        }

        public PlayersInMatch AddPlayer(UserRequest user)
        {
            return new PlayersInMatch(((ImmutableList<UserRequest>)Players).Add(user));
        }

        public List<UserRequest> GetList()
        {
            var list = new List<UserRequest>();
            foreach(var p in Players)
            {
                list.Add(p);
            }
            return list;
        }
    }
}
