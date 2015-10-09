using Modicite.Utilities;

namespace Modicite.Unity {

    class FileIdentifier {

        public string AssetPath;
        public byte[] GUID;
        public int Type;
        public string FilePath;


        private FileIdentifier() {

        }

        public static FileIdentifier Read(DataReader reader, bool includeAssetPath) {
            FileIdentifier fi = new FileIdentifier();

            if (includeAssetPath) {
                fi.AssetPath = reader.ReadString();
            }

            fi.GUID = reader.ReadBytes(16);
            fi.Type = reader.ReadInt32();
            fi.FilePath = reader.ReadString();

            return fi;
        }
    }
}
