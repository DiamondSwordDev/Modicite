using System;
using System.Collections.Generic;

namespace Modicite.Binaries.Unity.Assets {

    class FileIdentifier : UnityStruct {

        // Path to the asset file? Unused in asset format <= 5.
        private String assetPath;

        // Globally unique identifier of the referred asset. Unity displays these
        // as simple 16 byte hex strings with each byte swapped, but they can also
        // be represented according to the UUID standard.
        private readonly UnityGUID guid = new UnityGUID();

        // Path to the asset file. Only used if "type" is 0.
        private String filePath;

        // Reference type. Possible values are probably 0 to 3.
        private int type;

        private AssetFile assetFile;

        public FileIdentifier(VersionInfo versionInfo) : base (versionInfo) {

        }
        
        public override void read(DataReader input) { //TODO: Used to be 'throws IOException'
            if (versionInfo.assetVersion() > 5) {
                assetPath = input.readStringNull();
            }

            guid.read(input);
            type = input.readInt();
            filePath = input.readStringNull();
        }
        
        public override void write(DataWriter output) { //TODO: Used to be 'throws IOException'
            if (versionInfo.assetVersion() > 5) {
                output.writeStringNull(assetPath);
            }

            guid.write(output);
            output.writeInt(type);
            output.writeStringNull(filePath);
        }

        public UUID guid() {
            return guid.UUID();
        }

        public void guid(UUID guid) {
            this.guid.UUID(guid);
        }

        public String filePath() {
            return filePath;
        }

        public void filePath(String filePath) {
            this.filePath = filePath;
        }

        public String assetPath() {
            return assetPath;
        }

        public void assetPath(String assetPath) {
            this.assetPath = assetPath;
        }

        public int type() {
            return type;
        }

        public void type(int type) {
            this.type = type;
        }

        public AssetFile assetFile() {
            return assetFile;
        }

        void assetFile(AssetFile assetFile) {
            this.assetFile = assetFile;
        }

    }
}
