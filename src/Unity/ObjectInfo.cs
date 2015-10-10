using Modicite.Utilities;

namespace Modicite.Unity {

    class ObjectInfo {

        public int ObjectID;
        public int ByteStart;
        public int ByteSize;
        public int TypeID;
        public short ClassID;
        public short ScriptTypeIndex = 0;
        public short IsStripped;


        private ObjectInfo() {

        }

        public static ObjectInfo Read(DataReader reader, bool unity5Formatting) {
            ObjectInfo oi = new ObjectInfo();

            oi.ObjectID = reader.ReadInt32();
            oi.ByteStart = reader.ReadInt32();
            oi.ByteSize = reader.ReadInt32();
            oi.TypeID = reader.ReadInt32();
            oi.ClassID = reader.ReadInt16();

            if (unity5Formatting) {
                oi.ScriptTypeIndex = reader.ReadInt16();
                oi.IsStripped = reader.ReadBoolean() ? (short)1 : (short)0;
            } else {
                oi.IsStripped = reader.ReadInt16();
            }

            return oi;
        }
    }
}
