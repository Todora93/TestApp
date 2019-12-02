using System.Runtime.Serialization;

namespace MyActorService.Interfaces
{
    [DataContract(Name = "ExistingMatch", Namespace = "SimulationActor.Interfaces")]
    public class ExistingMatch
    {
        [DataMember]
        public bool HasMatch {get; private set;}

        [DataMember]
        public ActorInfo ActorInfo { get; private set; }

        public ExistingMatch(bool hasMatch, ActorInfo info)
        {
            HasMatch = hasMatch;
            ActorInfo = info;
        }
    }
}
