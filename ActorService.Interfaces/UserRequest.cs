namespace MyActorService.Interfaces
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [DataContract(Name = "UserRequest", Namespace = "SimulationActor.Interfaces")]
    public class UserRequest : IExtensibleDataObject, IEquatable<UserRequest>, IComparable<UserRequest>, IComparable
    {
        private ExtensionDataObject extensibleDataObject;

        public ExtensionDataObject ExtensionData 
        {
            get => extensibleDataObject; 
            set => extensibleDataObject = value; 
        }

        [DataMember]
        public long UserId { get; private set; }

        [DataMember]
        public string UserName { get; private set; }

        public UserRequest() { }

        public UserRequest(string userName)
        {
            UserName = userName;
        }

        public override string ToString()
        {
            return $"User: {UserName}";
        }

        public bool Equals([AllowNull] UserRequest other)
        {
            int compare = string.Compare(UserName, other?.UserName);
            return compare == 0;
        }

        public int CompareTo([AllowNull] UserRequest other)
        {
            return string.Compare(UserName, other?.UserName);
        }

        public int CompareTo(object obj)
        {
            return string.Compare(UserName, ((UserRequest)obj)?.UserName);
        }
        public override int GetHashCode()
        {
            return UserName.GetHashCode();
        }
    }
}
