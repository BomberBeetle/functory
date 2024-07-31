using System;
using System.Runtime.Serialization;

namespace Functory{
    [DataContract]
    public class SerializedNode{

        public SerializedNode(){

        }
        [DataMember]
        public float X;
        [DataMember]
        public float Y;
        [DataMember]
        public string registryAddress;
        [DataMember]
        public SerializedEditor defEditor;
        [DataMember]
        public string[] parameters;
        [DataMember]

        public bool IsLambdaNode;
        [DataMember]
        public bool IsOutputNode;
        [DataMember]
        public bool IsParamNode;
        [DataMember]

        public string paramName;
        [DataMember]

        public string paramIdx;
        [DataMember]

        public string nodeId;
    }
}