namespace Functory{
    public class SerializedNode{
        public float X;
        public float Y;
        public string registryAddress;
        public SerializedEditor defEditor;
        public string[] parameters;

        public bool IsLambdaNode;
        public bool IsOutputNode;
        public bool IsParamNode;

        public string paramName;

        public string paramIdx;

        public string nodeId;
    }
}