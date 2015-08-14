using System;
using System.IO;
using System.Collections.Generic;

using Modicite.Utilities;
using System.Diagnostics;

namespace Modicite.Binaries.Unity.Assets {

    class AssetFile : FileHandler {

        private static readonly Logger L = null; //TODO: Need to get Logger instance in here.
    
        private static readonly int METADATA_PADDING = 4096;

        // collection fields
        private readonly LinkedDictionary<long, ObjectInfo> objectInfoMap = new LinkedDictionary<long, ObjectInfo>();
        private readonly LinkedDictionary<int, BaseClass> typeTreeMap = new LinkedDictionary<int, BaseClass>();
        private readonly List<FileIdentifier> externals = new List<FileIdentifier>();
        private readonly List<ObjectData> objectList = new List<ObjectData>();
        private readonly List<ObjectData> objectListBroken= new List<ObjectData>();

        // struct fields
        private readonly VersionInfo versionInfo = new VersionInfo();
        private readonly AssetHeader header = new AssetHeader(versionInfo);
        private readonly ObjectInfoTable objectInfoStruct = new ObjectInfoTable(versionInfo, objectInfoMap);
        private readonly TypeTree typeTreeStruct = new TypeTree(versionInfo, typeTreeMap);
        private readonly FileIdentifierTable externalsStruct = new FileIdentifierTable(versionInfo, externals);

        // data block fields
        private readonly DataBlock headerBlock = new DataBlock();
        private readonly DataBlock objectInfoBlock = new DataBlock();
        private readonly DataBlock objectDataBlock = new DataBlock();
        private readonly DataBlock typeTreeBlock = new DataBlock();
        private readonly DataBlock externalsBlock = new DataBlock();

        // misc fields
        private ByteBuffer audioBuffer;
        
        public override void load(FilePath file) { //TODO: Used to be 'throws IOException'
            sourceFile = file;

            string fileName = file.getFileName().toString();
            string fileExt = FilenameUtils.getExtension(fileName);

            DataReader reader;
        
            // join split asset files before loading
            if (fileExt.StartsWith("split")) {
                L.fine("Found split asset file");

                fileName = FilenameUtils.removeExtension(fileName);
                List<FilePath> parts = new List<FilePath>();
                int splitIndex = 0;

                // collect all files with .split0 to .splitN extension
                while (true) {
                    string splitName = String.Format("{0}.split{1}", fileName, splitIndex);
                    FilePath part = file.resolveSibling(splitName);
                    if (!File.Exists(part)) {
                        break;
                    }

                    L.log(Level.FINE, String.Format("Adding splinter {0}", part.getFileName()));

                    splitIndex++;
                    parts.Add(part);
                }

                // load all parts to one byte buffer
                reader = DataReaders.forByteBuffer(ByteBufferUtils.load(parts));
            } else {
                reader = DataReaders.forFile(file, READ);
            }

            // load audio buffer if existing        
            loadResourceStream(file.resolveSibling(fileName + ".streamingResourceImage"));
            loadResourceStream(file.resolveSibling(fileName + ".resS"));

            load(reader);
        }

        private void loadResourceStream(FilePath streamFile) { //TODO: Used to be 'throws IOException'
        if (Files.exists(streamFile)) {
                L.log(Level.FINE, "Found sound stream file {0}", streamFile.getFileName());
                audioBuffer = ByteBufferUtils.openReadOnly(streamFile);
            }
        }
        
        public override void load(DataReader input) { //TODO: Used to be 'throws IOException'
            loadHeader(input);

            // read as little endian from now on
            input.order(ByteOrder.LITTLE_ENDIAN);
        
            // older formats store the object data before the structure data
            if (header.version() < 9) {
                input.position(header.fileSize() - header.metadataSize() + 1);
            }

            loadMetadata(input);
            loadObjects(input);
            checkBlocks();
        }

        public void loadExternals() { //TODO: Used to be 'throws IOException'
            loadExternals(new Dictionary<FilePath, AssetFile>());
        }

        private void loadExternals(Dictionary<FilePath, AssetFile> loadedAssets) { //TODO: Used to be 'throws IOException'
            loadedAssets[sourceFile] = this;
        
            for (FileIdentifier external : externals) {
                string filePath = external.filePath();

                if (filePath == null || filePath.Length < 1) {
                    continue;
                }

                filePath = filePath.Replace("library/", "resources/");

                FilePath refFile = sourceFile.resolveSibling(filePath);
                if (File.Exists(refFile)) {
                    AssetFile childAsset = loadedAssets[refFile];

                    if (childAsset == null) {
                        L.log(Level.FINE, "Loading dependency {0} for {1}",
                                new Object[] { filePath, sourceFile.getFileName() });
                        childAsset = new AssetFile();
                        childAsset.load(refFile);
                        childAsset.loadExternals(loadedAssets);
                        external.assetFile(childAsset);
                    }
                }
            }
        }

        private void loadHeader(DataReader input) { //TODO: Used to be 'throws IOException'
            headerBlock.markBegin(input);
            input.readStruct(header);
            headerBlock.markEnd(input);
            L.log(Level.FINER, "headerBlock: {0}", headerBlock);
        }

        private void loadMetadata(DataReader input) { //TODO: Used to be 'throws IOException'
            input.order(versionInfo.order());

            // read structure data
            typeTreeBlock.markBegin(input);
            input.readStruct(typeTreeStruct);
            typeTreeBlock.markEnd(input);
            L.log(Level.FINER, "typeTreeBlock: {0}", typeTreeBlock);

            objectInfoBlock.markBegin(input);
            input.readStruct(objectInfoStruct);
            objectInfoBlock.markEnd(input);
            L.log(Level.FINER, "objectInfoBlock: {0}", objectInfoBlock);
        
            // unknown block for Unity 5
            if (header.version() > 13) {
                input.align(4);
                int num = input.readInt();
                for (int i = 0; i < num; i++) {
                    input.readInt();
                    input.readInt();
                    input.readInt();
                }
            }

            externalsBlock.markBegin(input);
            input.readStruct(externalsStruct);
            externalsBlock.markEnd(input);
            L.log(Level.FINER, "externalsBlock: {0}", externalsBlock);
        }

        private void loadObjects(DataReader input) { //TODO: Used to be 'throws IOException'
            long ofsMin = Int64.MaxValue;
            long ofsMax = Int64.MinValue;
        
            foreach (LinkedDictionaryEntry<long, ObjectInfo> infoEntry in objectInfoMap.entrySet()) {
                ObjectInfo info = infoEntry.getValue();
                long id = infoEntry.getKey();

                ByteBuffer buf = ByteBufferUtils.allocate((int)info.length());

                long ofs = header.dataOffset() + info.offset();

                ofsMin = Math.Min(ofsMin, ofs);
                ofsMax = Math.Max(ofsMax, ofs + info.length());

                input.position(ofs);
                input.readBuffer(buf);

                TypeNode typeNode = null;

                BaseClass typeClass = typeTreeMap.get(info.typeID());
                if (typeClass != null) {
                    typeNode = typeClass.typeTree();
                }
            
                // get type from database if the embedded one is missing
                if (typeNode == null) {
                    typeNode = TypeTreeUtils.getTypeNode(info.unityClass(),
                            versionInfo.unityRevision(), false);
                }

                ObjectData data = new ObjectData(id, versionInfo);
                data.info(info);
                data.buffer(buf);
                data.typeTree(typeNode);
            
                ObjectSerializer serializer = new ObjectSerializer();
                serializer.setSoundData(audioBuffer);
                data.serializer(serializer);
            
                // Add typeless objects to an internal list. They can't be
                // (de)serialized, but can still be written to the file.
                if (typeNode == null) {
                    // log warning if it's not a MonoBehaviour
                    if (info.classID() != 114) {
                        L.log(Level.WARNING, "{0} has no type information!", data.toString());
                    }
                    objectListBroken.Add(data);
                } else {
                    objectList.Add(data);
                }
            }
        
            objectDataBlock.offset(ofsMin);
            objectDataBlock.endOffset(ofsMax);
            L.log(Level.FINER, "objectDataBlock: {0}", objectDataBlock);
        }
    
        public override void save(DataWriter output) { //TODO: Used to be 'throws IOException'
            saveHeader(output);

            // write as little endian from now on
            output.order(ByteOrder.LITTLE_ENDIAN);
        
            // older formats store the object data before the structure data
            if (header.version() < 9) {
                header.dataOffset(0);

                saveObjects(output);
                output.writeUnsignedByte(0);

                saveMetadata(output);
                output.writeUnsignedByte(0);
            } else {
                saveMetadata(output);

                // original files have a minimum padding of 4096 bytes after the
                // metadata
                if (output.position() < METADATA_PADDING) {
                    output.align(METADATA_PADDING);
                }

                output.align(16);
                header.dataOffset(output.position());

                saveObjects(output);

                // write updated path table
                output.position(objectInfoBlock.offset());
                output.writeStruct(objectInfoStruct);
            }

            // update header
            header.fileSize(output.size());
        
            // FIXME: the metadata size is slightly off in comparison to original files
            int metadataOffset = header.version() < 9 ? 2 : 1;

            header.metadataSize(typeTreeBlock.length()
                    + objectInfoBlock.length()
                    + externalsBlock.length()
                    + metadataOffset);
        
            // write updated header
            output.order(ByteOrder.BIG_ENDIAN);
            output.position(headerBlock.offset());
            output.writeStruct(header);

            checkBlocks();
        }

        private void saveHeader(DataWriter output) { //TODO: Used to be 'throws IOException'
            headerBlock.markBegin(output);
            output.writeStruct(header);
            headerBlock.markEnd(output);
            L.log(Level.FINER, "headerBlock: {0}", headerBlock);
        }

        private void saveMetadata(DataWriter output) { //TODO: Used to be 'throws IOException'
            output.order(versionInfo.order());

            typeTreeBlock.markBegin(output);
            output.writeStruct(typeTreeStruct);
            typeTreeBlock.markEnd(output);
            L.log(Level.FINER, "typeTreeBlock: {0}", typeTreeBlock);

            objectInfoBlock.markBegin(output);
            output.writeStruct(objectInfoStruct);
            objectInfoBlock.markEnd(output);
            L.log(Level.FINER, "objectInfoBlock: {0}", objectInfoBlock);

            externalsBlock.markBegin(output);
            output.writeStruct(externalsStruct);
            externalsBlock.markEnd(output);
            L.log(Level.FINER, "externalsBlock: {0}", externalsBlock);
        }

        private void saveObjects(DataWriter output) { //TODO: Used to be 'throws IOException'
            long ofsMin = UInt64.MaxValue;
                long ofsMax = UInt64.MinValue;

            // merge object lists
            objectList.addAll(objectListBroken);
        
                for (ObjectData data : objectList) {
                ByteBuffer bb = data.buffer();
                bb.rewind();

                output.align(8);

                ofsMin = Math.min(ofsMin, output.position());
                ofsMax = Math.max(ofsMax, output.position() + bb.remaining());

                ObjectInfo info = data.info();
                info.offset(output.position() - header.dataOffset());
                info.length(bb.remaining());

                output.writeBuffer(bb);
            }

            // separate object lists
            objectList.removeAll(objectListBroken);

            objectDataBlock.offset(ofsMin);
            objectDataBlock.endOffset(ofsMax);
            L.log(Level.FINER, "objectDataBlock: {0}", objectDataBlock);
        }

        private void checkBlocks() {
            // sanity check for the data blocks
            Debug.Assert(!headerBlock.isIntersecting(typeTreeBlock));
            Debug.Assert(!headerBlock.isIntersecting(objectInfoBlock));
            Debug.Assert(!headerBlock.isIntersecting(externalsBlock));
            Debug.Assert(!headerBlock.isIntersecting(objectDataBlock));

            Debug.Assert(!typeTreeBlock.isIntersecting(objectInfoBlock));
            Debug.Assert(!typeTreeBlock.isIntersecting(externalsBlock));
            Debug.Assert(!typeTreeBlock.isIntersecting(objectDataBlock));

            Debug.Assert(!objectInfoBlock.isIntersecting(externalsBlock));
            Debug.Assert(!objectInfoBlock.isIntersecting(objectDataBlock));

            Debug.Assert(!objectDataBlock.isIntersecting(externalsBlock));
        }

        public VersionInfo versionInfo() {
            return versionInfo;
        }

        public AssetHeader header() {
            return header;
        }

        public int typeTreeAttributes() {
            return typeTreeStruct.attributes();
        }

        public Map<Integer, BaseClass> typeTree() {
            return typeTreeMap;
        }

        public Map<Long, ObjectInfo> objectInfoMap() {
            return objectInfoMap;
        }

        public List<ObjectData> objects() {
            return objectList;
        }

        public List<FileIdentifier> externals() {
            return externals;
        }

        public bool isStandalone() {
            return !typeTreeStruct.embedded();
        }

        public void setStandalone() {
            typeTreeStruct.embedded(true);
        }
    }
}
