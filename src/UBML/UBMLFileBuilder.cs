using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Modicite.Utilities;
using Modicite.Unity.Serialization;

namespace Modicite.UBML {

    static class UBMLFileBuilder {

        //Approximation. Files can be anywhere from 0x to 1.5x this value;
        private static readonly int CLASS_POINTS_PER_FILE = 50;
        private static readonly int BYTES_PER_CLASS_POINT = 16;

        public static void Build(string outputDirectory, UnityFile uf) {
            if (UnityClassIDDatabase.Classes == null || UnityClassIDDatabase.Classes.Count < 1) {
                throw new InvalidOperationException("The class ID database must be loaded before the UBML deconstructor can be used.");
            }

            if (UnityRTTIDatabase.Version == -1) {
                throw new InvalidOperationException("The RTTI database must be loaded before the UBML deconstructor can be used.");
            }

            if (uf.Metadata.ClassHierarchyDescriptor.NumberOfBaseClasses > 0) {
                throw new InvalidDataException("Runtime Type Information is not supported by the UBML deconstructor.");
            }

            string baseDirectory = outputDirectory.Replace("\\", "/").TrimEnd('/');

            if (!Directory.Exists(baseDirectory)) {
                Directory.CreateDirectory(baseDirectory);
            }

            #region Write Header File

            StringBuilder mainBuilder = new StringBuilder();
            mainBuilder.AppendLine("Header {");
            mainBuilder.AppendLine("  Version: " + uf.Header.Version.ToString() + ";");
            mainBuilder.AppendLine("  Signature: \"" + uf.Metadata.ClassHierarchyDescriptor.Signature + "\";");
            mainBuilder.AppendLine("  ClassHierarchyAttributes: 0x" + uf.Metadata.ClassHierarchyDescriptor.Attributes.ToString("X8") + ";");
            mainBuilder.AppendLine("}\n");
            AppendIncludes(uf.Metadata.FileIdentifiers, mainBuilder);
            File.WriteAllText(baseDirectory + "/header.ubml", mainBuilder.ToString().Replace("\r", ""));

            #endregion

            int fileIndex = 0;
            StringBuilder fileBuilder = new StringBuilder();
            int membersSize = 0;

            foreach (ObjectInfo oi in uf.Metadata.ObjectInfoList) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Object " + oi.ObjectID.ToString() + " (ClassID: " + oi.ClassID.ToString() + ")");

                TypeNode baseNode = null;
                try {
                    baseNode = UnityRTTIDatabase.GetNewestTypeForClass(oi.ClassID);
                } catch {
                    #region RawData Object

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Raw Data");

                    int memberSize = oi.ByteSize / (BYTES_PER_CLASS_POINT * 16);

                    if (memberSize + membersSize > CLASS_POINTS_PER_FILE) {
                        if (memberSize > CLASS_POINTS_PER_FILE) {
                            throw new FormatException("Object member is larger than the specified maximum size.");
                        } else {
                            File.WriteAllText(baseDirectory + "/objects." + fileIndex.ToString() + ".ubml", fileBuilder.ToString());

                            fileBuilder = new StringBuilder();

                            fileIndex++;
                            membersSize = 0;
                        }
                    }

                    fileBuilder.Append("Object ");
                    fileBuilder.Append(oi.ObjectID.ToString());
                    fileBuilder.Append(" {\n");

                    fileBuilder.Append("  Class: \"");
                    fileBuilder.Append(UnityClassIDDatabase.Classes[oi.ClassID]);
                    fileBuilder.Append("\";\n");

                    if (oi.ClassID != oi.TypeID) {
                        fileBuilder.Append("  TypeID: ");
                        fileBuilder.Append(oi.TypeID.ToString());
                        fileBuilder.Append(";\n");
                    }

                    if (oi.IsDestroyed != 0) {
                        fileBuilder.Append("  IsDestroyed: ");
                        fileBuilder.Append(oi.IsDestroyed.ToString());
                        fileBuilder.Append(";\n");
                    }

                    fileBuilder.Append("  RawData:");

                    for (int i = 0; i < oi.ByteSize; i++) {
                        fileBuilder.Append(" ");
                        fileBuilder.Append(uf.ObjectData[oi.ByteStart + i].ToString());
                    }

                    fileBuilder.Append(";\n}\n\n");

                    membersSize += memberSize;

                    #endregion
                }
                
                #region Create Object Properties

                StringBuilder propertiesBuilder = new StringBuilder();

                propertiesBuilder.Append("Object ");
                propertiesBuilder.Append(oi.ObjectID.ToString());
                propertiesBuilder.Append(" {\n");

                propertiesBuilder.Append("  Class: \"");
                propertiesBuilder.Append(UnityClassIDDatabase.Classes[oi.ClassID]);
                propertiesBuilder.Append("\";\n");

                if (oi.ClassID != oi.TypeID) {
                    propertiesBuilder.Append("  TypeID: ");
                    propertiesBuilder.Append(oi.TypeID.ToString());
                    propertiesBuilder.Append(";\n");
                }

                if (oi.IsDestroyed != 0) {
                    propertiesBuilder.Append("  IsDestroyed: ");
                    propertiesBuilder.Append(oi.IsDestroyed.ToString());
                    propertiesBuilder.Append(";\n");
                }
                
                #endregion

                if (baseNode.NumberOfChildren < 1) {
                    fileBuilder.Append(propertiesBuilder.ToString());
                    fileBuilder.Append("  HasNoData: ");
                    fileBuilder.Append(true.ToString());
                    fileBuilder.Append(";\n}\n\n");
                    continue;
                }

                propertiesBuilder.Append("  Data: {\n");

                byte[] objectData = new byte[oi.ByteSize];
                for (int i = 0; i < oi.ByteSize; i++) {
                    objectData[i] = uf.ObjectData[i + oi.ByteStart];
                }
                DataReader objectDataReaderA = DataReader.FromBytes(objectData, uf.Header.Endianness == 0);
                DataReader objectDataReaderB = DataReader.FromBytes(objectData, uf.Header.Endianness == 0);

                bool hasStartedObject = false;

                foreach (TypeNode memberNode in baseNode.Children) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Member " + memberNode.Name + " (" + memberNode.Type + ")");

                    if (memberNode.NumberOfChildren > 0 && memberNode.Children[0].IsArray == 1) {
                        #region Array

                        if (memberNode.Children[0].NumberOfChildren != 2 ||
                            memberNode.Children[0].Children[0].Name != "size" ||
                            memberNode.Children[0].Children[1].Name != "data") {
                            throw new FormatException("An array node must have two children: 'size' and 'data'.");
                        }

                        #region Create Array Properties

                        StringBuilder arrayPropertiesBuilder = new StringBuilder();

                        arrayPropertiesBuilder.Append("    .Array .ArrayType=");
                        arrayPropertiesBuilder.Append(memberNode.Children[0].Children[1].Type);
                        arrayPropertiesBuilder.Append(" ");
                        arrayPropertiesBuilder.Append(memberNode.Type);
                        arrayPropertiesBuilder.Append(" ");
                        arrayPropertiesBuilder.Append(memberNode.Name);
                        arrayPropertiesBuilder.Append(": {\n");

                        #endregion

                        int size = objectDataReaderA.ReadInt32();
                        objectDataReaderB.ReadInt32();

                        TypeNode dataNode = memberNode.Children[0].Children[1];
                        bool hasStartedArrayMember = false;

                        for (int i = 0; i < size; i++) {
                            int byteSize = CalculateByteSize(dataNode, objectDataReaderA);
                            int memberSize = (byteSize / BYTES_PER_CLASS_POINT) + (byteSize % BYTES_PER_CLASS_POINT > 0 ? 1 : 0);

                            if (memberSize + membersSize > CLASS_POINTS_PER_FILE) {
                                if (memberSize > CLASS_POINTS_PER_FILE) {
                                    throw new FormatException("Object member is larger than the specified maximum size.");
                                } else {
                                    if (hasStartedArrayMember) {
                                        fileBuilder.Append("    }\n");
                                    }
                                    if (hasStartedObject) {
                                        fileBuilder.Append("  }\n}");
                                    }

                                    File.WriteAllText(baseDirectory + "/objects." + fileIndex.ToString() + ".ubml", fileBuilder.ToString());
                                        
                                    fileIndex++;
                                    membersSize = 0;

                                    fileBuilder = new StringBuilder();
                                    fileBuilder.Append(propertiesBuilder.ToString());
                                }
                            }

                            if (!hasStartedObject) {
                                fileBuilder.Append(propertiesBuilder.ToString());
                                hasStartedObject = true;
                            }

                            if (!hasStartedArrayMember) {
                                fileBuilder.Append(arrayPropertiesBuilder.ToString());
                                hasStartedArrayMember = true;
                            }

                            AppendTypeNode(dataNode, objectDataReaderB, fileBuilder, "      ", true);
                            membersSize += memberSize;
                        }

                        if (hasStartedArrayMember) {
                            fileBuilder.Append("    }\n");
                        }

                        #endregion
                    } else {
                        #region Object
                            
                        int byteSize = CalculateByteSize(memberNode, objectDataReaderA);
                        int memberSize = (byteSize / BYTES_PER_CLASS_POINT) + (byteSize % BYTES_PER_CLASS_POINT > 0 ? 1 : 0);

                        if (memberSize + membersSize > CLASS_POINTS_PER_FILE) {
                            if (memberSize > CLASS_POINTS_PER_FILE) {
                                throw new FormatException("Object member is larger than the specified maximum size.");
                            } else {
                                if (hasStartedObject) {
                                    fileBuilder.Append("  }\n}");
                                }

                                File.WriteAllText(baseDirectory + "/objects." + fileIndex.ToString() + ".ubml", fileBuilder.ToString());

                                fileIndex++;
                                membersSize = 0;

                                fileBuilder = new StringBuilder();
                                fileBuilder.Append(propertiesBuilder.ToString());
                            }
                        }

                        if (!hasStartedObject) {
                            fileBuilder.Append(propertiesBuilder.ToString());
                            hasStartedObject = true;
                        }

                        AppendTypeNode(memberNode, objectDataReaderB, fileBuilder, "    ");
                        membersSize += memberSize;

                        #endregion
                    }
                }

                fileBuilder.Append("  }\n}\n\n");
                
            }
            
            if (fileBuilder.Length > 0) {
                File.WriteAllText(baseDirectory + "/objects." + fileIndex.ToString() + ".ubml", fileBuilder.ToString());
            }
        }
        
        private static void AppendIncludes(FileIdentifier[] identifiers, StringBuilder builder) {
            foreach (FileIdentifier fi in identifiers) {
                bool hasGUID = false;
                foreach (byte b in fi.GUID) {
                    if (b != 0) {
                        hasGUID = true;
                        break;
                    }
                }

                if (fi.AssetPath == "" && fi.Type == 0 && !hasGUID) {
                    builder.AppendLine("Include \"" + fi.FilePath + "\";\n");
                } else {
                    builder.AppendLine("Include {");
                    builder.AppendLine("  FilePath: \"" + fi.FilePath + "\";");

                    if (fi.AssetPath != "") {
                        builder.AppendLine("  AssetPath: \"" + fi.AssetPath + "\";");
                    }

                    if (fi.Type != 0) {
                        builder.AppendLine("  Type: " + fi.Type.ToString() + ";");
                    }

                    if (hasGUID) {
                        builder.Append("  GUID:");
                        foreach (byte b in fi.GUID) {
                            builder.Append(" " + b.ToString("X2"));
                        }
                        builder.AppendLine(";");
                    }

                    builder.AppendLine("}\n");
                }
            }
        }

        private static int CalculateByteSize(TypeNode node, DataReader objectDataReader) {
            if (node.IsArray == 1) {
                int size = objectDataReader.ReadInt32();
                int ret = 4;
                for (int i = 0; i < size; i++) {
                    ret += CalculateByteSize(node.Children[1], objectDataReader);
                }
                return ret;
            } else {
                if (node.ByteSize == -1) {
                    int ret = 0;
                    foreach (TypeNode child in node.Children) {
                        ret += CalculateByteSize(child, objectDataReader);
                    }
                    return ret;
                } else {
                    objectDataReader.ReadBytes(node.ByteSize);
                    return node.ByteSize;
                }
            }
        }
        
        private static void AppendTypeNode(TypeNode tn, DataReader reader, StringBuilder builder, string indentation, bool skipName = false) {
            if (tn.IsArray == 1) {

                TypeNode dataNode = tn.Children[1];

                builder.Append(indentation + ".Array " + dataNode.Type + ":");

                int size = reader.ReadInt32();

                if (size == 0) {
                    builder.AppendLine(" .Empty;");
                    return;
                }

                if (dataNode.NumberOfChildren < 1) {
                    for (int i = 0; i < size; i++) {
                        byte[] data = reader.ReadBytes(dataNode.ByteSize);
                        builder.Append(" " + GetStringFromTypedBytes(dataNode.Type, data));
                    }
                    builder.AppendLine(";");
                } else {
                    builder.AppendLine(" {");
                    for (int i = 0; i < size; i++) {
                        AppendTypeNode(dataNode, reader, builder, indentation + "  ", true);
                    }
                    builder.AppendLine(indentation + "}");
                }
            } else {

                if (skipName) {
                    builder.Append(indentation + ".: ");
                } else {
                    builder.Append(indentation + tn.Type + " " + tn.Name + ": ");
                }

                if (tn.Type == "string") {
                    if (tn.NumberOfChildren < 1 || tn.Children[0].IsArray != 1 || tn.Children[0].Children[1].Type != "char") {
                        throw new FormatException("Children of TypeNode are not in the correct format for a string field.");
                    }

                    int size = reader.ReadInt32();

                    builder.Append("\"");
                    for (int i = 0; i < size; i++) {
                        builder.Append(Encoding.UTF8.GetString(new byte[] { reader.ReadByte() }));
                    }
                    builder.AppendLine("\";");

                    return;
                }

                if (tn.NumberOfChildren < 1) {
                    byte[] data = reader.ReadBytes(tn.ByteSize);
                    builder.AppendLine(GetStringFromTypedBytes(tn.Type, data) + ";");
                } else {
                    builder.AppendLine("{");
                    foreach (TypeNode child in tn.Children) {
                        AppendTypeNode(child, reader, builder, indentation + "  ");
                    }
                    builder.AppendLine(indentation + "}");
                }
            }
        }

        private static string GetStringFromTypedBytes(string type, byte[] data) {
            switch (type) {
                case "bool":
                    return data[0] == 0 ? "False" : "True";
                case "SInt8":
                    return ((sbyte)data[0]).ToString();
                case "UInt8":
                    return data[0].ToString();
                case "char":
                    return data[0] > 31 && data[0] < 127 ? Encoding.UTF8.GetString(new byte[] { data[0] }) : data[0].ToString();
                case "SInt16":
                    return BitConverter.ToInt16(data, 0).ToString();
                case "short":
                    return BitConverter.ToInt16(data, 0).ToString();
                case "UInt16":
                    return BitConverter.ToUInt16(data, 0).ToString();
                case "unsigned short":
                    return BitConverter.ToUInt16(data, 0).ToString();
                case "SInt32":
                    return BitConverter.ToInt32(data, 0).ToString();
                case "int":
                    return BitConverter.ToInt32(data, 0).ToString();
                case "UInt32":
                    return BitConverter.ToUInt32(data, 0).ToString();
                case "unsigned int":
                    return BitConverter.ToUInt32(data, 0).ToString();
                case "float":
                    return BitConverter.ToSingle(data, 0).ToString();
                case "SInt64":
                    return BitConverter.ToInt64(data, 0).ToString();
                case "long":
                    return BitConverter.ToInt64(data, 0).ToString();
                case "UInt64":
                    return BitConverter.ToUInt64(data, 0).ToString();
                case "unsigned long":
                    return BitConverter.ToUInt64(data, 0).ToString();
                case "double":
                    return BitConverter.ToDouble(data, 0).ToString();
                default:
                    throw new InvalidOperationException("Unable to convert bytes to a string of the given type.");
            }
        }
    }
}
