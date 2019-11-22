namespace MyActorService.Interfaces
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [DataContract(Name = "UserRequest", Namespace = "SimulationActor.Interfaces")]
    public sealed class UserRequest : IExtensibleDataObject, IEquatable<UserRequest>, IComparable<UserRequest>, IComparable
    {
        private ExtensionDataObject extensibleDataObject;

        public ExtensionDataObject ExtensionData 
        {
            get => extensibleDataObject; 
            set => extensibleDataObject = value; 
        }

        [DataMember]
        public long UserId { get; set; }

        public UserRequest(long userId)
        {
            UserId = userId;
        }

        public int CompareTo([AllowNull] UserRequest other)
        {
            return UserId.CompareTo(other.UserId);
        }

        public int CompareTo(object obj)
        {
            return UserId.CompareTo(((UserRequest)obj).UserId);
        }

        public bool Equals([AllowNull] UserRequest other)
        {
            return UserId == other.UserId;
        }

        public override string ToString()
        {
            return $"ID: {UserId}";
        }
    }
}
