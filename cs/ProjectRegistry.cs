using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Functory{

    [DataContract]
    public class ProjectRegistry{
        [DataMember]
        public Dictionary<string, string> functions; //Node ID, Title.
        //use node ID as function coordinate. 

        public ProjectRegistry(){
            functions = new Dictionary<string, string>();
        }
    }
}