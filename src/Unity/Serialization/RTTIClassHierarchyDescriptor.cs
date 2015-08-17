using Modicite.Utilities;

namespace Modicite.Unity.Serialization {

    class RTTIClassHierarchyDescriptor {

        public string Signature;
        public int Attributes;
        public int NumberOfBaseClasses;
        public RTTIBaseClassDescriptor[] BaseClassDescriptors;


        private RTTIClassHierarchyDescriptor() {

        }

        public static RTTIClassHierarchyDescriptor Read(DataReader reader) {
            RTTIClassHierarchyDescriptor chd = new RTTIClassHierarchyDescriptor();

            chd.Signature = reader.ReadString();
            chd.Attributes = reader.ReadInt32();
            chd.NumberOfBaseClasses = reader.ReadInt32();
            chd.BaseClassDescriptors = new RTTIBaseClassDescriptor[chd.NumberOfBaseClasses];

            for (int i = 0; i < chd.NumberOfBaseClasses; i++) {
                chd.BaseClassDescriptors[i] = RTTIBaseClassDescriptor.Read(reader);
            }

            reader.ReadBytes(4); //Read padding bytes

            return chd;
        }
    }
}
