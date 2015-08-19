using System;
using System.IO;
using System.Text;
using Modicite.Utilities;
using Modicite.Unity.Serialization;

namespace Modicite.Construction {

    static class UnityFileConstruction {

        public static void Deconstruct(string outputDir, UnityFile uf) {
            if (UnityClassIDDatabase.Classes == null || UnityClassIDDatabase.Classes.Count < 1) {
                throw new InvalidOperationException("The class ID database must be loaded before the UBML deconstructor can be used");
            }

            if (UnityRTTIDatabase.Version == -1) {
                throw new InvalidOperationException("The RTTI database must be loaded before the UBML deconstructor can be used");
            }

            if (uf.Metadata.ClassHierarchyDescriptor.NumberOfBaseClasses > 0) {
                throw new InvalidDataException("Runtime Type Information is not supported by the UBML deconstructor");
            }

            if (!Directory.Exists(outputDir)) {
                Directory.CreateDirectory(outputDir);
            }
            
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Header {");
            builder.AppendLine("  Version: " + uf.Header.Version.ToString() + ";");
            builder.AppendLine("  Signature: \"" + uf.Metadata.ClassHierarchyDescriptor.Signature + "\";");
            builder.AppendLine("  ClassHierarchyAttributes: 0x" + uf.Metadata.ClassHierarchyDescriptor.Attributes.ToString("X8") + ";");
            builder.AppendLine("}\n");

            #region Includes
            foreach (FileIdentifier fi in uf.Metadata.FileIdentifiers) {
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
            #endregion
            
            foreach (ObjectInfo oi in uf.Metadata.ObjectInfoList) {
                builder.AppendLine("Object " + oi.ObjectID.ToString() + " {");

                if (oi.ClassID == 114) {
                    builder.AppendLine("  Class: \"" + UnityClassIDDatabase.Classes[oi.ClassID] + "\";");
                    builder.AppendLine("  TypeID: " + oi.TypeID.ToString() + ";");
                } else {
                    builder.AppendLine("  Class: \"" + UnityClassIDDatabase.Classes[oi.ClassID] + "\";");
                }

                if (oi.IsDestroyed != 0) {
                    builder.AppendLine("  IsDestroyed: " + oi.IsDestroyed.ToString() + ";");
                }
                
                try {
                    TypeNode tn = UnityRTTIDatabase.GetNewestTypeForClass(oi.ClassID);
                            
                    byte[] typeData = new byte[oi.ByteSize];
                    for (int i = 0; i < oi.ByteSize; i++) {
                        typeData[i] = uf.ObjectData[oi.ByteStart + i];
                    }
                    DataReader reader = DataReader.FromBytes(typeData);
                    reader.IsLittleEndian = uf.Header.Endianness == 0;

                    StringBuilder dataBuilder = new StringBuilder();
                    dataBuilder.AppendLine("  Data: {");

                    AppendTypeNode(tn, reader, dataBuilder, "    ");

                    dataBuilder.AppendLine("  }");
                    builder.AppendLine(dataBuilder.ToString());
                } catch (Exception ex) {
                    builder.Append("    HexData:");
                    for (int i = 0; i < oi.ByteSize; i++) {
                        builder.Append(" " + uf.ObjectData[i + oi.ByteStart].ToString("X2"));
                    }
                    builder.AppendLine(";");
                }

                builder.AppendLine("}\n");
            }

            File.WriteAllText(outputDir.Replace("\\", "/").TrimEnd('/') + "/main.ubml", builder.ToString().Replace("\r", ""));
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
                        throw new FormatException("Children of TypeNode are not in the correct format for a string field");
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
                    throw new InvalidOperationException("Unable to convert bytes to a string of the given type");
            }
        }
    }
}
