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

        [DataMember]
        public string Move { get; set; }

        [DataMember]
        public int Life { get; set; }

        [DataMember]
        public int PositionX { get; set; }

        [DataMember]
        public int PositionY { get; set; }

        public PlayerState(UserRequest user)
        {
            User = user;
            //Value = value;
        }

        public void UpdateState(string move, int life, int positionX, int positionY)
        {
            Move = move;
            Life = life;
            PositionX = positionX;
            PositionY = positionY;
        }

        public override string ToString()
        {
            return $"(User: {User.ToString()})";
        }
    }
}
