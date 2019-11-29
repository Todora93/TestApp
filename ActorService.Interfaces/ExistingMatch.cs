using Microsoft.ServiceFabric.Actors;
using System.Runtime.Serialization;

namespace MyActorService.Interfaces
{
    [DataContract(Name = "ExistingMatch", Namespace = "SimulationActor.Interfaces")]
    public class ExistingMatch
    {
        [DataMember]
        public bool HasMatch {get; private set;}

        [DataMember]
        public ActorId ActorId { get; private set; }

        public ExistingMatch(bool hasMatch, ActorId id)
        {
            HasMatch = hasMatch;
            ActorId = id;
        }
    }
}
