using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Functory{
    [DataContract]
    public class SerializedEditor{
        public SerializedEditor(){

        }
        [DataMember]
        public SerializedNode[] nodes;

        [DataMember]
        public bool IsLambdaEditor;

        [DataMember]
        public string LambdaNodeId;

        [DataMember]
        public Connection[] connections;

        [DataContract]
        public struct Connection{
            [DataMember]
            public int toPort;
            [DataMember]
            public string fromNodeId;
            [DataMember]
            public string toNodeId;

        }
    }
}