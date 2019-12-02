using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

// todo: make generic
namespace MyActorService.Interfaces
{
    [DataContract(Name = "ActiveActors", Namespace = "SimulationActor.Interfaces")]
    public sealed class ActiveActors
    {
        private static readonly IEnumerable<int> NoUserRequest = ImmutableList<int>.Empty;

        [DataMember]
        public IEnumerable<int> Indexes { get; private set; }

        public int Count => ((ImmutableList<int>)Indexes).Count;

        public ActiveActors(IEnumerable<int> indexes = null)
        {
            Indexes = indexes == null ? NoUserRequest : indexes.ToImmutableList();
        }

        public void OnDeserialized(StreamingContext context)
        {
            Indexes = Indexes.ToImmutableList();
        }

        public ActiveActors AddPlayer(int index)
        {
            return new ActiveActors(((ImmutableList<int>)Indexes).Add(index));
        }

        public List<int> GetList()
        {
            var list = new List<int>();
            foreach (var p in Indexes)
            {
                list.Add(p);
            }
            return list;
        }
    }
}
