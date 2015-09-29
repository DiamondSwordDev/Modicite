using Modicite.Utilities;
using Modicite.Unity.RTTI;

namespace Modicite.Unity {

    class RTTIBaseClassDescriptor {

        public int ClassID;
        public TypeNode TypeTree;

        private RTTIBaseClassDescriptor() {

        }

        public static RTTIBaseClassDescriptor Read(DataReader reader) {
            RTTIBaseClassDescriptor bcd = new RTTIBaseClassDescriptor();

            bcd.ClassID = reader.ReadInt32();
            bcd.TypeTree = TypeNode.Read(reader);

            return bcd;
        }
    }
}
