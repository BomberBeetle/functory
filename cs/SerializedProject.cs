using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Functory{

    [DataContract]
    public class SerializedProject{

        [DataMember]
        public SerializedEditor rootEditor;

        [DataMember]
        public ProjectRegistry registry;

        public SerializedProject(SerializedEditor rootEditor, ProjectRegistry registry){
            this.rootEditor = rootEditor;
            this.registry = registry;
        }

        public SerializedProject(){

        }

        public void WriteToXmlFile(string filePath, bool append = false)
        {
            TextWriter twriter = null;
            XmlWriter xmlWriter = null;
            try
            {
                var serializer = new DataContractSerializer(typeof(SerializedProject));
                twriter = new StreamWriter(filePath, append);
                xmlWriter = new XmlTextWriter(twriter);
                
                serializer.WriteObject(xmlWriter, this);
            }
            finally
            {
                xmlWriter?.Close();
                twriter?.Close();
            }
        }

        public static SerializedProject CreateFromXmlFile(string path){
            SerializedProject project;
            using (XmlReader reader = XmlReader.Create(path))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(SerializedProject));
                project = (SerializedProject)serializer.ReadObject(reader);
            }
            return project;

        }
    }
}