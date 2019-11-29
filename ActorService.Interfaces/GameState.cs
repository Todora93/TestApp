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
            foreach (UserRequest user in users)
            {
                State.Add(new PlayerState(user));
            }
            GameTimeSec = 0;
        }

        public PlayerState GetPlayerState(UserRequest user)
        {
            return State.Exists(u => u.User.Equals(user)) ? State.Find(u => u.User.Equals(user)) : null;
        }

        public PlayerState GetOpponentPlayerState(UserRequest user)
        {
            return State.Find(u => !u.User.Equals(user));
        }

        public int GetPlayerIndex(UserRequest user)
        {
            return State.FindIndex(u => u.User.Equals(user));
        }

        public List<UserRequest> GetUsers()
        {
            var users = new List<UserRequest>();
            foreach(var state in State)
            {
                users.Add(state.User);
            }
            return users;
        }

        public void UpdatePlayerState(UserRequest user, string move, int life, int positionX, int positionY)
        {
            var playerState = GetPlayerState(user);
            playerState.UpdateState(move, life, positionX, positionY);
        }

        public override string ToString()
        {
            if (State == null || State.Count == 0)
            {
                return "State is null or empty";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append($"GameTime: {GameTimeSec}s");
            builder.Append("    ");
            foreach (PlayerState state in State)
            {
                builder.Append(state.ToString());
                builder.Append("    ");
            }
            return builder.ToString();
        }
    }
}
