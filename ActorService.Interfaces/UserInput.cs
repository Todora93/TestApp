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

        public bool[] PressedKeys { get; set; }

        public UserInput() { }

        public UserInput(bool[] pressedKeys)
        {
            //Input = input;
            PressedKeys = pressedKeys;
        }

        public override string ToString()
        {
            return $"Input: {Input}";
        }
    }
}
