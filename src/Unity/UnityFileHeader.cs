using Modicite.Utilities;

namespace Modicite.Unity {

    class UnityFileHeader {

        public int MetadataSize;
        public int FileSize;
        public int Version;
        public int DataOffset;
        public byte Endianness;


        private UnityFileHeader() {

        }

        public static UnityFileHeader Read(DataReader reader) {
            UnityFileHeader ufh = new UnityFileHeader();

            ufh.MetadataSize = reader.ReadInt32();
            ufh.FileSize = reader.ReadInt32();
            ufh.Version = reader.ReadInt32();
            ufh.DataOffset = reader.ReadInt32();
            ufh.Endianness = reader.ReadByte();

            reader.ReadBytes(3); //Read reserved bytes

            return ufh;
        }
    }
}
