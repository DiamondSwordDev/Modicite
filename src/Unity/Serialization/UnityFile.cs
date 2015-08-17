using System;
using Modicite.Utilities;

namespace Modicite.Unity.Serialization {

    class UnityFile {

        private string filename;
        
        public UnityFileHeader Header;
        public UnityFileMetadata Metadata;
        public byte[] ObjectData = new byte[0];


        private UnityFile(string filename) {
            this.filename = filename;
        }

        public static UnityFile Load(string filename) {
            UnityFile uf = new UnityFile(filename);

            DataReader reader = DataReader.OpenFile(filename, 1000000);
            reader.IsLittleEndian = false;

            uf.Header = UnityFileHeader.Read(reader);

            if (uf.Header.Version < 9) {
                throw new FormatException("This does not support deserialization of files for Unity versions 3.4 and older");
            }

            if (uf.Header.Version >= 14) {
                throw new FormatException("This does not support deserialization of files for Unity versions 5.0 and newer");
            }

            reader.IsLittleEndian = uf.Header.Endianess == 0;

            uf.Metadata = UnityFileMetadata.Read(reader);
            
            uf.ObjectData = reader.ReadRemainingBytes();

            return uf;
        }
    }
}
