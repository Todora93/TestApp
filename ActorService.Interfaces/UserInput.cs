using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MyActorService.Interfaces
{
    [DataContract(Name = "UserInput", Namespace = "SimulationActor.Interface")]
    public class UserInput : IExtensibleDataObject
    {
        private ExtensionDataObject extensibleDataObject;

        public ExtensionDataObject ExtensionData
        {
            get => extensibleDataObject;
            set => extensibleDataObject = value;
        }

        [DataMember]
        public int Input { get; set; }

        public UserInput() { }

        public UserInput(int input)
        {
            Input = input;
        }

        public override string ToString()
        {
            return $"Input: {Input}";
        }
    }
}
