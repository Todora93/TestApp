using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MyActorService.Interfaces
{
    [DataContract(Name = "ActorInfo", Namespace = "SimulationActor.Interfaces")]
    public class ActorInfo : IComparable<ActorInfo>, IComparable, IEquatable<ActorInfo>
    { 
        [DataMember]
        public int ActorIndex { get; private set; }

        [DataMember]
        public ActorId ActorId { get; private set; }

        public ActorInfo(ActorId id, int actorIndex)
        {
            ActorId = id;
            ActorIndex = actorIndex;
        }

        public override string ToString()
        {
            return $"Id: {ActorId}, Index: {ActorIndex}";
        }

        public int CompareTo([AllowNull] ActorInfo other)
        {
            int compare = ActorIndex.CompareTo(other?.ActorIndex);
            if (compare != 0) return compare;
            compare = ActorId.CompareTo(other?.ActorId);
            return compare;
        }

        public int CompareTo(object obj)
        {
            return this.CompareTo((ActorId)obj);
        }

        public bool Equals([AllowNull] ActorInfo other)
        {
            return ActorIndex == other?.ActorIndex && ActorId == other?.ActorId;
        }

        public override int GetHashCode()
        {
            return ActorId.GetHashCode();
        }
    }
}
