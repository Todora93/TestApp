using System.Runtime.Serialization;

namespace MyActorService.Interfaces
{
    [DataContract(Name = "PlayerState", Namespace = "SimulationActor.Interface")]
    public class PlayerState : IExtensibleDataObject
    {
        private ExtensionDataObject extensibleDataObject;

        public ExtensionDataObject ExtensionData
        {
            get => extensibleDataObject;
            set => extensibleDataObject = value;
        }

        [DataMember]
        public UserRequest User { get; private set; }

        [DataMember]
        public int Value { get; set; }

        public PlayerState(UserRequest user, int value)
        {
            User = user;
            Value = value;
        }

        public override string ToString()
        {
            return $"(User: {User.ToString()}, State: {Value})";
        }
    }
}
