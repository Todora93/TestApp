using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MyActorService.Interfaces
{
    [DataContract(Name = "GameState", Namespace = "SimulationActor.Interface")]
    public class GameState : IExtensibleDataObject
    {
        private ExtensionDataObject extensibleDataObject;

        public ExtensionDataObject ExtensionData
        {
            get => extensibleDataObject;
            set => extensibleDataObject = value;
        }

        [DataMember]
        public List<PlayerState> State { get; private set; }

        [DataMember]
        public long GameTimeSec { get; set; }

        public GameState(List<UserRequest> users)
        {
            State = new List<PlayerState>();
            foreach(UserRequest user in users)
            {
                State.Add(new PlayerState(user, 0));
            }
            GameTimeSec = 0;
        }

        public override string ToString()
        {
            if (State == null || State.Count == 0)
            {
                return "State is null or empty";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append($"GameTime: {GameTimeSec}[sec]\n");
            foreach (PlayerState state in State)
            {
                builder.Append(state.ToString());
                builder.Append("/n");
            }
            return builder.ToString();
        }
    }
}
