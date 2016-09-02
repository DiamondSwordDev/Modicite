using System;
using System.Collections.Generic;
using Modicite.Utilities;

namespace Modicite.Unity {

    class ObjectData {

        public long ObjectID;
        public int TypeID;
        public short ClassID;
        public short ScriptTypeIndex;
        public short IsStripped;
        public byte[] Bytes;


        private ObjectData() {

        }

        public static ObjectData Read(DataReader reader, ObjectInfo info, int dataOffset) {
            ObjectData od = new ObjectData();

            od.ObjectID = info.ObjectID;
            od.TypeID = info.TypeID;
            od.ClassID = info.ClassID;
            od.ScriptTypeIndex = info.ScriptTypeIndex;
            od.IsStripped = info.IsStripped;

            reader.JumpTo(dataOffset + info.ByteStart);
            od.Bytes = reader.ReadBytes(info.ByteSize);

            return od;
        }

        public void Write(DataWriter writer, int offset, bool unity5Formatting) {
            if (unity5Formatting) {
                writer.WriteInt64(ObjectID);
            } else {
                writer.WriteInt32((int)ObjectID);
            }

            writer.WriteInt32(offset);
            writer.WriteInt32(Bytes.Length);
            writer.WriteInt32(TypeID);
            writer.WriteInt16(ClassID);

            if (unity5Formatting) {
                writer.WriteInt16(ScriptTypeIndex);
                writer.WriteBoolean(IsStripped != 0);
            } else {
                writer.WriteInt16(IsStripped);
            }
        }
    }
}
