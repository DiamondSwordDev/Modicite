using System;
using System.IO;
using System.Text;
using Modicite.Utilities;
using System.Collections.Generic;

using Modicite.Unity.RTTI;

namespace Modicite.Unity {

    class UnityFile {

        private string filename;
        
        public UnityFileHeader Header;
        public UnityFileMetadata Metadata;
        public byte[] ObjectData = new byte[0];


        private UnityFile(string filename) {
            this.filename = filename;
        }

        public static UnityFile Load(string filename, bool loadObjectData) {
            UnityFile uf = new UnityFile(filename);

            DataReader reader = DataReader.OpenFile(filename, 1000000);
            reader.IsLittleEndian = false;

            uf.Header = UnityFileHeader.Read(reader);

            if (uf.Header.Version < 9) {
                throw new FormatException("This does not support deserialization of files for Unity versions 3.4 and older.");
            }

            if (uf.Header.Version >= 14) {
                throw new FormatException("This does not support deserialization of files for Unity versions 5.0 and newer.");
            }

            reader.IsLittleEndian = uf.Header.Endianness == 0;

            uf.Metadata = UnityFileMetadata.Read(reader);
            
            if (loadObjectData) {
                reader.JumpTo(uf.Header.DataOffset);
                uf.ObjectData = reader.ReadRemainingBytes();
            }

            reader.Close();

            return uf;
        }

        
        public void ExportHeaderToFile(string fileName) {
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
        
        public void ExportObjectToFile(ObjectInfo objectInfo, string fileName, string failureFileName) {
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
        }
    }
}
