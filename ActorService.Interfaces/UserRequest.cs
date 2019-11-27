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

        [DataMember]
        public string UserName { get; set; }

        public UserRequest() { }

        public UserRequest(string userName)
        {
            UserName = userName;
        }

        public int CompareTo([AllowNull] UserRequest other)
        {
            return UserName.CompareTo(other.UserName);
        }

        public int CompareTo(object obj)
        {
            return UserName.CompareTo(((UserRequest)obj).UserName);
        }

        public bool Equals([AllowNull] UserRequest other)
        {
            return UserName.Equals(other.UserName);
        }

        public override string ToString()
        {
            return $"User: {UserName}";
        }
    }
}
