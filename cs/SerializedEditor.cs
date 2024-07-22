using System;
using System.Runtime.CompilerServices;

namespace Functory{
    public class SerializedEditor{
        public SerializedNode[] nodes;

        public bool IsLambdaEditor;

        public string LambdaNodeId;

        public Connection[] connections;
        public struct Connection{
            public int toPort;
            public string fromNodeId;
            public string toNodeId;

        }
    }
}