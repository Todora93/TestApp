﻿using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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

        public PlayerState GetPlayerState(UserRequest user)
        {
            return State.Exists(u => u.User.Equals(user)) ? State.Find(u => u.User.Equals(user)) : null;
        }

        public PlayerState GetOpponentPlayerState(UserRequest user)
        {
            return State.Find(u => !u.User.Equals(user));
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
