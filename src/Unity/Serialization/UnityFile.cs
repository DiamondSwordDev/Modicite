using System;
using System.IO;
using System.Text;
using Modicite.Utilities;
using System.Collections.Generic;

namespace Modicite.Unity.Serialization {

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


        public void ExportToJSONDirectory(string directory) {
            if (UnityClassIDDatabase.Classes == null || UnityClassIDDatabase.Classes.Count < 1) {
                throw new InvalidOperationException("The class ID database must be loaded before the file can be exported.");
            }

            if (UnityRTTIDatabase.Version == -1) {
                throw new InvalidOperationException("The RTTI database must be loaded before the file can be exported.");
            }

            if (Metadata.ClassHierarchyDescriptor.NumberOfBaseClasses > 0) {
                throw new InvalidDataException("Runtime Type Information is not supported.");
            }

            if (ObjectData.Length < 1) {
                throw new InvalidOperationException("The object data must be loaded before the file can be exported.");
            }

            string baseDirectory = directory.Replace("\\", "/").TrimEnd('/');

            if (!Directory.Exists(baseDirectory)) {
                Directory.CreateDirectory(baseDirectory);
            }
            
            ExportHeaderToFile(baseDirectory);
            

            Dictionary<string, object> objectFileObject = new Dictionary<string, object>();

            int fileIndex = 0;

            const int maxFileSize = 512;
            int currentFileSize = 0;
                
            DataReader objectDataReader = DataReader.FromBytes(ObjectData);

            foreach (ObjectInfo oi in Metadata.ObjectInfoList) {
                Console.CursorLeft = 0;
                Console.Write("Object " + oi.ObjectID.ToString() + "/" + Metadata.ObjectInfoList.Length.ToString());

                if (currentFileSize + oi.ByteSize > maxFileSize) {
                    File.WriteAllText(baseDirectory + "/objects." + fileIndex.ToString() + ".json", GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(objectFileObject)));
                    objectFileObject.Clear();
                    currentFileSize = 0;
                    fileIndex++;
                }

                Dictionary<string, object> objectObject = new Dictionary<string, object>(); //Lol, the redundancy...
                if (UnityClassIDDatabase.Classes.ContainsKey(oi.ClassID)) {
                    objectObject["class"] = UnityClassIDDatabase.Classes[oi.ClassID];
                } else {
                    objectObject["classID"] = oi.ClassID;
                }
                if (oi.TypeID != oi.ClassID) {
                    objectObject["typeID"] = oi.TypeID;
                }
                if (oi.IsDestroyed != 0) {
                    objectObject["isDestroyed"] = oi.IsDestroyed;
                }
                if (oi.ByteSize > 0) {
                    try {
                        objectDataReader.JumpTo(oi.ByteStart);
                        objectObject["data"] = ExportTypeNodesAsJsonObject(UnityRTTIDatabase.GetTypeForClassVersion(oi.ClassID, Metadata.ClassHierarchyDescriptor.Signature).Children, objectDataReader, Header.Endianness == 0);
                    } catch (Exception ex) {
                        objectDataReader.JumpTo(oi.ByteStart);
                        objectObject["rawDataFailure"] = ex.GetType().Name + ": " + ex.Message;
                        objectObject["rawData"] = objectDataReader.ReadBytes(oi.ByteSize);
                    }
                } else {
                    objectObject["data"] = null;
                }

                objectFileObject[oi.ObjectID.ToString()] = objectObject;
                currentFileSize += oi.ByteSize;
            }

            Console.WriteLine("");
        }

        private void ExportHeaderToFile(string directory) {
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

            File.WriteAllText(directory + "/header.json", GetFormattedJson(SimpleJson.SimpleJson.SerializeObject(mainObject)));
        }
        
        private Dictionary<string, object> ExportTypeNodesAsJsonObject(TypeNode[] nodes, DataReader objectDataReader, bool isLittleEndian) {
            Dictionary<string, object> nodesObject = new Dictionary<string, object>();
            
            foreach (TypeNode node in nodes) {
                if (node.NumberOfChildren > 0 && node.Children[0].IsArray == 1) {
                    Dictionary<string, object> arrayData = ExportTypeNodesAsJsonObject(node.Children[0].Children, objectDataReader, isLittleEndian);
                    List<object> arrayObjects = new List<object>();
                    int size = (int)arrayData["int size"];
                    for (int i = 0; i < size; i++) {
                        arrayObjects.Add(ExportTypeNodesAsJsonObject(node.Children[0].Children[1].Children, objectDataReader, isLittleEndian));
                    }
                    string nodeId = "." + node.MetaFlag.ToString() + " .ArrayType=" + node.Children[0].Children[1].Type + " " + node.Type + " " + node.Name;
                    nodesObject[nodeId] = arrayObjects;
                } else {
                    if (node.NumberOfChildren < 1) {
                        byte[] data = objectDataReader.ReadBytes(node.ByteSize);
                        if (BitConverter.IsLittleEndian != isLittleEndian) {
                            Array.Reverse(data);
                        }
                        string nodeId = "." + node.MetaFlag.ToString() + " " + node.Type + " " + node.Name;
                        nodesObject[nodeId] = GetObjectFromTypedBytes(node.Type, data);
                    } else {
                        if (node.NumberOfChildren < 1) {
                            throw new FormatException("A non-primitive object must have child nodes.");
                        }
                        string nodeId = "." + node.MetaFlag.ToString() + " " + node.Type + " " + node.Name;
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
                            for (i = i; i < end; i++) {
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
