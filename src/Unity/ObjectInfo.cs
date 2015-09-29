using Modicite.Utilities;

namespace Modicite.Unity {

    class ObjectInfo {

        public int ObjectID;
        public int ByteStart;
        public int ByteSize;
        public int TypeID;
        public short ClassID;
        public short IsDestroyed;


        private ObjectInfo() {

        }

        public static ObjectInfo Read(DataReader reader) {
            ObjectInfo oi = new ObjectInfo();

            oi.ObjectID = reader.ReadInt32();
            oi.ByteStart = reader.ReadInt32();
            oi.ByteSize = reader.ReadInt32();
            oi.TypeID = reader.ReadInt32();
            oi.ClassID = reader.ReadInt16();
            oi.IsDestroyed = reader.ReadInt16();

            return oi;
        }
    }
}
