using System;
using System.Collections.Generic;

namespace Modicite.Binaries.Unity.Assets {

    class AssetHeader : UnityStruct {

        // size of the structure data
        private long metadataSize;

        // size of the whole asset file
        private long fileSize;

        // offset to the serialized data
        private long dataOffset;

        // byte order of the serialized data?
        private byte endianness;

        // unused
        private readonly byte[] reserved = new byte[3];

        public AssetHeader(VersionInfo versionInfo) : base(versionInfo) {

        }
        
        public override void read(DataReader input) { //TODO: Used to be 'throws IOException'
            metadataSize = input.readInt();
            fileSize = input.readUnsignedInt();
            versionInfo.assetVersion(input.readInt());
            dataOffset = input.readUnsignedInt();
            if (versionInfo.assetVersion() >= 9) {
                endianness = input.readByte();
                input.readBytes(reserved);
            }
        }

        @Override
    public void write(DataWriter output) { //TODO: Used to be 'throws IOException'
            output.writeUnsignedInt(metadataSize);
            output.writeUnsignedInt(fileSize);
            output.writeInt(versionInfo.assetVersion());
            output.writeUnsignedInt(dataOffset);
        if (versionInfo.assetVersion() >= 9) {
                output.writeByte(endianness);
                output.writeBytes(reserved);
            }
        }

        public long metadataSize() {
            return metadataSize;
        }

        public void metadataSize(long metadataSize) {
            this.metadataSize = metadataSize;
        }

        public long fileSize() {
            return fileSize;
        }

        public void fileSize(long fileSize) {
            this.fileSize = fileSize;
        }

        public int version() {
            return versionInfo.assetVersion();
        }

        public void version(int version) {
            versionInfo.assetVersion(version);
        }

        public long dataOffset() {
            return dataOffset;
        }

        public void dataOffset(long dataOffset) {
            this.dataOffset = dataOffset;
        }

        public byte endianness() {
            return endianness;
        }

        public void endianness(byte endianness) {
            this.endianness = endianness;
        }

    }
}
