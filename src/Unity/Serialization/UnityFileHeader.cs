using Modicite.Utilities;

namespace Modicite.Unity.Serialization {

    class UnityFileHeader {

        public int MetadataSize;
        public int FileSize;
        public int Version;
        public int DataOffset;
        public byte Endianess;


        private UnityFileHeader() {

        }

        public static UnityFileHeader Read(DataReader reader) {
            UnityFileHeader ufh = new UnityFileHeader();

            ufh.MetadataSize = reader.ReadInt32();
            ufh.FileSize = reader.ReadInt32();
            ufh.Version = reader.ReadInt32();
            ufh.DataOffset = reader.ReadInt32();
            ufh.Endianess = reader.ReadByte();

            reader.ReadBytes(3); //Read reserved bytes

            return ufh;
        }
    }
}
