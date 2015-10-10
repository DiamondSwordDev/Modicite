using Modicite.Utilities;

namespace Modicite.Unity.RTTI {

    class TypeNode {

        public string Type; //Base class's name should always be "base"
        public string Name;
        public int ByteSize;
        public int Index;
        public int IsArray;
        public int Version;
        public int MetaFlag;
        public TypeNode[] Children; //TypeNodes in UnityFiles are not meant to be modified just yet, and will
                                    //as such remain non-generic for now.  Turtles!


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

            int numberOfChildren = reader.ReadInt32();
            tn.Children = new TypeNode[numberOfChildren];

            for (int i = 0; i < numberOfChildren; i++) {
                tn.Children[i] = TypeNode.Read(reader);
            }

            return tn;
        }

        public void Write(DataWriter writer) {
            writer.WriteString(Type);
            writer.WriteString(Name);
            writer.WriteInt32(ByteSize);
            writer.WriteInt32(Index);
            writer.WriteInt32(IsArray);
            writer.WriteInt32(Version);
            writer.WriteInt32(MetaFlag);

            writer.WriteInt32(Children.Length);
            foreach (TypeNode childNode in Children) {
                childNode.Write(writer);
            }
        }
    }
}
