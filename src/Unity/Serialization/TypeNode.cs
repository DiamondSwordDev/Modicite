using Modicite.Utilities;

namespace Modicite.Unity.Serialization {

    class TypeNode {

        public string Type; //Base class's name should always be "base"
        public string Name;
        public int ByteSize;
        public int Index;
        public int IsArray;
        public int Version;
        public int MetaFlag;
        public int NumberOfChildren;
        public TypeNode[] Children;


        private TypeNode() {

        }

        public static TypeNode Read(DataReader reader) {
            TypeNode tn = new TypeNode();

            tn.Type = reader.ReadString();
            tn.Name = reader.ReadString();
            tn.ByteSize = reader.ReadInt32();
            tn.Index = reader.ReadInt32();
            tn.IsArray = reader.ReadInt32();
            tn.Version = reader.ReadInt32();
            tn.MetaFlag = reader.ReadInt32();
            tn.NumberOfChildren = reader.ReadInt32();
            tn.Children = new TypeNode[tn.NumberOfChildren];

            for (int i = 0; i < tn.NumberOfChildren; i++) {
                tn.Children[i] = TypeNode.Read(reader);
            }

            return tn;
        }
    }
}
