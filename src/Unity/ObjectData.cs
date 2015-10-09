using System;
using System.Collections.Generic;
using Modicite.Utilities;

namespace Modicite.Unity {

    class ObjectData {

        public int ObjectID;
        public int TypeID;
        public short ClassID;
        public short ScriptTypeIndex;
        public bool IsStripped;
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
    }
}
