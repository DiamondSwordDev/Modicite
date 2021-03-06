using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Modicite.Utilities;
using Modicite.Unity.RTTI;

namespace Modicite.Unity {

    class UnityFile {

        private string filename;

        public int FormatVersion = 0;
        public bool IsLittleEndian = true;

        public string UnityVersion = null;
        public int RTTIAttributes = 0;
        public Dictionary<int, TypeNode> BaseClasses = new Dictionary<int, TypeNode>();

        public List<FileIdentifier> ExternalFiles = new List<FileIdentifier>();
        public List<ObjectData> Objects = new List<ObjectData>();

        public List<ObjectInfo> ObjectInfos = new List<ObjectInfo>();
        public byte[] __Metadata__ = new byte[0];
        

        private UnityFile(string filename) {
            this.filename = filename;
        }

        public static UnityFile Load(string filename) {
            UnityFile uf = new UnityFile(filename);

            DataReader reader = DataReader.OpenFile(filename, false);

            #region Header

            int metadataSize = reader.ReadInt32();
            int fileSize = reader.ReadInt32();
            uf.FormatVersion = reader.ReadInt32();
            int dataOffset = reader.ReadInt32();

            if (uf.FormatVersion > 8) {
                uf.IsLittleEndian = reader.ReadByte() == 0;
                reader.ReadBytes(3);
            }

            reader.IsLittleEndian = uf.IsLittleEndian;

            #endregion

            #region Metadata

            if (uf.FormatVersion < 9) {
                reader.JumpTo(fileSize - metadataSize); //Used to be 'F - M + 1'
            }

            uf.__Metadata__ = reader.ReadBytes(metadataSize);

            reader.JumpTo(16 + (uf.FormatVersion > 8 ? 4 : 0));

            if (uf.FormatVersion < 9) {
                reader.JumpTo(fileSize - metadataSize); //Used to be 'F - M + 1'
            }

            #region Class Hierarchy Descriptor

            if (uf.FormatVersion > 7) {
                uf.UnityVersion = reader.ReadString();
                uf.RTTIAttributes = reader.ReadInt32();
            }

            int numberOfBaseClasses = reader.ReadInt32();

            for (int i = 0; i < numberOfBaseClasses; i++) {
                int classId = reader.ReadInt32();
                TypeNode classNode = TypeNode.Read(reader);
                uf.BaseClasses[classId] = classNode;
            }

            if (uf.FormatVersion > 7) {
                reader.ReadBytes(4);
            }

            #endregion

            int numberOfObjectDataInstances = reader.ReadInt32();

            List<ObjectInfo> objectDataPointers = new List<ObjectInfo>();
            for (int i = 0; i < numberOfObjectDataInstances; i++) {
                objectDataPointers.Add(ObjectInfo.Read(reader, uf.FormatVersion > 13));
            }

            uf.ObjectInfos = objectDataPointers;

            int numberOfExternalFiles = reader.ReadInt32();
            
            for (int i = 0; i < numberOfExternalFiles; i++) {
                uf.ExternalFiles.Add(FileIdentifier.Read(reader, uf.FormatVersion > 5));
            }

            #endregion

            foreach (ObjectInfo oi in objectDataPointers) {
                uf.Objects.Add(ObjectData.Read(reader, oi, dataOffset));
            }

            reader.Close();

            return uf;
        }

        public void Save(string newFilename = null) {
            //This always saves in little-endian format:
            //- Metadata will always be little-endian.
            //- Header will always be big-endian as per the file format.
            //- Object Data follows this.IsLittleEndian

            #region Metadata

            List<byte> metadataSection = new List<byte>();
            DataWriter metadataWriter = DataWriter.FromList(metadataSection, true);

            #region Class Hierarchy Descriptor

            if (FormatVersion > 7) {
                metadataWriter.WriteString(UnityVersion);
                metadataWriter.WriteInt32(RTTIAttributes);
            }

            metadataWriter.WriteInt32(BaseClasses.Count);

            foreach (int baseClass in BaseClasses.Keys) {
                metadataWriter.WriteInt32(baseClass);
                BaseClasses[baseClass].Write(metadataWriter);
            }

            if (FormatVersion > 7) {
                metadataWriter.WriteBytes(new byte[] { 0, 0, 0, 0 });
            }

            #endregion

            metadataWriter.WriteInt32(Objects.Count);

            int currentOffset = 0;
            foreach (ObjectData od in Objects) {
                od.Write(metadataWriter, currentOffset, FormatVersion > 13);
                metadataWriter.WriteInt64(0);
                currentOffset += od.Bytes.Length + 8;
            }

            metadataWriter.WriteInt32(ExternalFiles.Count);

            foreach (FileIdentifier f in ExternalFiles) {
                f.Write(metadataWriter, FormatVersion > 5);
            }

            metadataWriter.WriteByte(0);

            #endregion

            #region Header and Object Data

            DataWriter fileWriter = DataWriter.OpenFile(newFilename == null ? filename : newFilename, false);

            fileWriter.WriteInt32(metadataSection.Count);

            int headerSize = 16 + (FormatVersion > 8 ? 4 : 0);
            fileWriter.WriteInt32(metadataSection.Count + currentOffset + headerSize);

            fileWriter.WriteInt32(FormatVersion);

            if (FormatVersion > 8) {
                fileWriter.WriteInt32(headerSize + metadataSection.Count);

                fileWriter.WriteByte(IsLittleEndian ? (byte)0 : (byte)1);
                fileWriter.WriteBytes(new byte[] { 0, 0, 0 });

                fileWriter.IsLittleEndian = IsLittleEndian;

                fileWriter.WriteBytes(metadataSection.ToArray());

                foreach (ObjectData od in Objects) {
                    fileWriter.WriteBytes(od.Bytes);
                }
            } else {
                fileWriter.WriteInt32(headerSize);

                fileWriter.IsLittleEndian = IsLittleEndian;

                foreach (ObjectData od in Objects) {
                    fileWriter.WriteBytes(od.Bytes);
                }

                fileWriter.WriteBytes(metadataSection.ToArray());
            }

            #endregion

            fileWriter.Close();
        }

        public void DumpMeta(int max) {
            string metaDump = "ID   | Type | Index   | Size    | Extra\n";

            for (int i = 0; i < ObjectInfos.Count && i < max; i++) {
                ObjectInfo oi = ObjectInfos[i];

                metaDump += oi.ObjectID.ToString().PadLeft(4, ' ') + " | ";
                metaDump += oi.ClassID.ToString().PadLeft(4, ' ') + " | ";
                metaDump += oi.ByteStart.ToString().PadLeft(7, ' ') + " | ";
                metaDump += oi.ByteSize.ToString().PadLeft(7, ' ') + " | ";
                if (i != ObjectInfos.Count - 1) {
                    metaDump += (ObjectInfos[i+1].ByteStart - (oi.ByteSize + oi.ByteStart)).ToString().PadLeft(7, ' ');
                } else { 
                    metaDump += "N/A";
                }
                metaDump += "\n";
            }

            File.WriteAllText("./metadump.txt", metaDump);
        }
        

        /*public void ExportHeaderToFile(string fileName) {
            Dictionary<string, object> mainObject = new Dictionary<string, object>();

            Dictionary<string, object> headerObject = new Dictionary<string, object>();
            headerObject["version"] = Header.Version;
            headerObject["signature"] = Metadata.ClassHierarchyDescriptor.Signature;
            headerObject["classHierarchyAttributes"] = "0x" + Metadata.ClassHierarchyDescriptor.Attributes.ToString("X8");
            mainObject["header"] = headerObject;

            List<object> includesArray = new List<object>();
            foreach (FileIdentifier fi in Metadata.FileIdentifiers) {
                Dictionary<string, object> includeObject = new Dictionary<string, object>();

                includeObject["filePath"] = fi.FilePath;

                if (fi.AssetPath != "") {
                    includeObject["assetPath"] = fi.AssetPath;
                }

                if (fi.Type != 0) {
                    includeObject["type"] = fi.Type;
                }

                bool hasGUID = false;
                string guid = "";
                foreach (byte b in fi.GUID) {
                    if (guid == "") {
                        guid += b.ToString();
                    } else {
                        guid += " " + b.ToString();
                    }
                    if (b != 0) {
                        hasGUID = true;
                    }
                }
                if (hasGUID) {
                    includeObject["guid"] = guid;
                }

                includesArray.Add(includeObject);
            }
            mainObject["includes"] = includesArray;

            File.WriteAllText(fileName, GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(mainObject)));
        }
        
        /*public void ExportObjectToFile(ObjectInfo objectInfo, string fileName, string failureFileName) {
            Dictionary<string, object> fileObject = new Dictionary<string, object>();
            fileObject["objectID"] = objectInfo.ObjectID;
            if (objectInfo.ClassID == 114) {
                fileObject["classID"] = objectInfo.ClassID;
            } else {
                fileObject["class"] = ClassIDDatabase.Classes[objectInfo.ClassID];
            }
            fileObject["typeID"] = objectInfo.TypeID;
            if (objectInfo.IsDestroyed != 0) {
                fileObject["isDestroyed"] = objectInfo.IsDestroyed;
            }

            DataReader objectDataReader = DataReader.FromBytes(ObjectData, Header.Endianness == 0);
            
            try {
                objectDataReader.JumpTo(objectInfo.ByteStart);
                if (objectInfo.ClassID == 114) {
                    fileObject["rawDataFailure"] = "No failure; class type '114' has no structure definition.";
                    fileObject["rawData"] = objectDataReader.ReadBytes(objectInfo.ByteSize);
                    File.WriteAllText(failureFileName, GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(fileObject)));
                } else {
                    fileObject["data"] = ExportTypeNodesAsJsonObject(RTTIDatabase.GetTypeForClassVersion(objectInfo.ClassID, Metadata.ClassHierarchyDescriptor.Signature).Children, objectDataReader, Header.Endianness == 0);
                    File.WriteAllText(fileName, GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(fileObject)));
                }
            } catch (Exception ex) {
                objectDataReader.JumpTo(objectInfo.ByteStart);
                fileObject["rawDataFailure"] = ex.GetType().Name + ": " + ex.Message;
                fileObject["rawData"] = objectDataReader.ReadBytes(objectInfo.ByteSize);
                File.WriteAllText(failureFileName, GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(fileObject)));
            }

            objectDataReader.Close();
        }

        public void ExportRawObjectToFile(ObjectInfo objectInfo, string fileName) {
            Dictionary<string, object> fileObject = new Dictionary<string, object>();
            fileObject["objectID"] = objectInfo.ObjectID;
            if (objectInfo.ClassID == 114) {
                fileObject["classID"] = objectInfo.ClassID;
            } else {
                fileObject["class"] = ClassIDDatabase.Classes[objectInfo.ClassID];
            }
            fileObject["typeID"] = objectInfo.TypeID;
            if (objectInfo.IsDestroyed != 0) {
                fileObject["isDestroyed"] = objectInfo.IsDestroyed;
            }

            DataReader objectDataReader = DataReader.FromBytes(ObjectData, Header.Endianness == 0);
            
            objectDataReader.JumpTo(objectInfo.ByteStart);
            fileObject["rawDataFailure"] = "No failure; object was intended to exported as raw data.";
            fileObject["rawData"] = objectDataReader.ReadBytes(objectInfo.ByteSize);
            File.WriteAllText(fileName, GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(fileObject)));

            objectDataReader.Close();
        }

        private Dictionary<string, object> ExportTypeNodesAsJsonObject(TypeNode[] nodes, DataReader objectDataReader, bool isLittleEndian) {
            Dictionary<string, object> nodesObject = new Dictionary<string, object>();
            
            foreach (TypeNode node in nodes) {
                if (node.NumberOfChildren > 0 && node.Children[0].IsArray == 1) {
                    if (node.Type.ToLower() == "string") {
                        int size = objectDataReader.ReadInt32();
                        string nodeValue = Encoding.UTF8.GetString(objectDataReader.ReadBytes(size));
                        nodesObject[node.Type + " " + node.Name] = nodeValue;
                    } else {
                        List<object> arrayObjects = new List<object>();
                        int size = objectDataReader.ReadInt32();
                        for (int i = 0; i < size; i++) {
                            arrayObjects.Add(ExportTypeNodesAsJsonObject(node.Children[0].Children[1].Children, objectDataReader, isLittleEndian));
                        }
                        string nodeId = ".ArrayType=" + node.Children[0].Children[1].Type + " " + node.Type + " " + node.Name;
                        nodesObject[nodeId] = arrayObjects;
                    }
                    
                } else {
                    if (node.NumberOfChildren < 1) {
                        byte[] data = objectDataReader.ReadBytes(node.ByteSize);
                        if (BitConverter.IsLittleEndian != isLittleEndian) {
                            Array.Reverse(data);
                        }
                        string nodeId = node.Type + " " + node.Name;
                        nodesObject[nodeId] = GetObjectFromTypedBytes(node.Type, data);
                    } else {
                        if (node.NumberOfChildren < 1) {
                            throw new FormatException("A non-primitive object must have child nodes.");
                        }
                        string nodeId = node.Type + " " + node.Name;
                        nodesObject[nodeId] = ExportTypeNodesAsJsonObject(node.Children, objectDataReader, isLittleEndian);
                    }
                }
            }

            return nodesObject;
        }

        private static object GetObjectFromTypedBytes(string type, byte[] data) {
            switch (type) {
                case "bool":
                    return data[0] != 0;
                case "SInt8":
                    return (sbyte)data[0];
                case "UInt8":
                    return data[0];
                case "char":
                    return (char)data[0];
                case "SInt16":
                    return BitConverter.ToInt16(data, 0);
                case "short":
                    return BitConverter.ToInt16(data, 0);
                case "UInt16":
                    return BitConverter.ToUInt16(data, 0);
                case "unsigned short":
                    return BitConverter.ToUInt16(data, 0);
                case "SInt32":
                    return BitConverter.ToInt32(data, 0);
                case "int":
                    return BitConverter.ToInt32(data, 0);
                case "UInt32":
                    return BitConverter.ToUInt32(data, 0);
                case "unsigned int":
                    return BitConverter.ToUInt32(data, 0);
                case "float":
                    return BitConverter.ToSingle(data, 0);
                case "SInt64":
                    return BitConverter.ToInt64(data, 0);
                case "long":
                    return BitConverter.ToInt64(data, 0);
                case "UInt64":
                    return BitConverter.ToUInt64(data, 0);
                case "unsigned long":
                    return BitConverter.ToUInt64(data, 0);
                case "double":
                    return BitConverter.ToDouble(data, 0);
                default:
                    throw new InvalidOperationException("Unable to convert bytes to a string of the given type.");
            }
        }

        private string GetFormattedJson(string input) {
            string formattedString = "";
            string indentation = "";
            bool isStringLiteral = false;
            bool isPrimitiveArray = false;
            for (int i = 0; i < input.Length; i++) {
                char c = input[i];
                if (isStringLiteral) {
                    if (c == '\"') {
                        isStringLiteral = false;
                        formattedString += c;
                    } else if (c == '\\') {
                        if (input[i+1] == 'u') {
                            int end = i + 6;
                            for (; i < end; i++) {
                                formattedString += input[i];
                            }
                        } else {
                            formattedString += c + input[i+1];
                            i++;
                        }
                    } else {
                        formattedString += c;
                    }
                } else {
                    int currentIndent = indentation.Length / 4;
                    switch (input[i]) {
                        case '\"':
                            isStringLiteral = true;
                            formattedString += c;
                            break;
                        case '{':
                        case '[':
                            if (input[i] == '[' && i != input.Length - 1 && input[i+1] != '{') {
                                isPrimitiveArray = true;
                            }
                            formattedString += c + "\n";
                            indentation = "";
                            for (int ii = 0; ii < currentIndent + 1; ii++) {
                                indentation += "    ";
                            }
                            formattedString += indentation;
                            break;
                        case '}':
                        case ']':
                            isPrimitiveArray = false;
                            indentation = "";
                            for (int ii = 0; ii < currentIndent - 1; ii++) {
                                indentation += "    ";
                            }
                            formattedString += "\n" + indentation + c;
                            break;
                        case ',':
                            if (isPrimitiveArray) {
                                formattedString += c + " ";
                            } else {
                                formattedString += c + "\n" + indentation;
                            }
                            break;
                        case ':':
                            formattedString += c + " ";
                            break;
                        default:
                            formattedString += c;
                            break;
                    }
                }
            }
            return formattedString;
        }*/
    }
}
