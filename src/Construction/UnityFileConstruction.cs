using System;
using System.IO;
using System.Text;
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
            builder.AppendLine("    Version: " + uf.Header.Version.ToString() + ";");
            builder.AppendLine("    Signature: \"" + uf.Metadata.ClassHierarchyDescriptor.Signature + "\";");
            builder.AppendLine("    ClassHierarchyAttributes: " + uf.Metadata.ClassHierarchyDescriptor.Attributes.ToString("X8") + ";");
            builder.AppendLine("}\n");

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
                    builder.AppendLine("    FilePath: \"" + fi.FilePath + "\";");

                    if (fi.AssetPath != "") {
                        builder.AppendLine("    AssetPath: \"" + fi.AssetPath + "\";");
                    }

                    if (fi.Type != 0) {
                        builder.AppendLine("    Type: " + fi.Type.ToString() + ";");
                    }

                    if (hasGUID) {
                        builder.Append("    GUID:");
                        foreach (byte b in fi.GUID) {
                            builder.Append(" " + b.ToString("X2"));
                        }
                        builder.AppendLine(";");
                    }

                    builder.AppendLine("}\n");
                }
            }

            foreach (ObjectInfo oi in uf.Metadata.ObjectInfoList) {
                builder.AppendLine("Object " + oi.ObjectID.ToString() + " {");

                if (oi.ClassID == 114) {
                    builder.AppendLine("    Class: \"" + UnityClassIDDatabase.Classes[oi.ClassID] + "\";");
                    builder.AppendLine("    TypeID: " + oi.TypeID.ToString() + ";");
                } else {
                    builder.AppendLine("    Class: \"" + UnityClassIDDatabase.Classes[oi.ClassID] + "\";");
                }

                if (oi.IsDestroyed != 0) {
                    builder.AppendLine("    IsDestroyed: " + oi.IsDestroyed.ToString() + ";");
                }
                
                switch (oi.ClassID) {
                    default:
                        try {
                            TypeNode tn = UnityRTTIDatabase.GetNewestTypeForClass(oi.ClassID);
                            StringBuilder dataBuilder = new StringBuilder();
                            dataBuilder.AppendLine("    Data: {");
                            AppendTypeNode(tn, oi, uf.ObjectData, dataBuilder, 2, uf.Header.Endianness == 0);
                            dataBuilder.AppendLine("    }");
                            builder.AppendLine(dataBuilder.ToString());
                        } catch (Exception ex) {
                            builder.Append("    HexData:");
                            for (int i = 0; i < oi.ByteSize; i++) {
                                builder.Append(" " + uf.ObjectData[i + oi.ByteStart].ToString("X2"));
                            }
                            builder.AppendLine(";");
                        }
                        break;
                }

                builder.AppendLine("}\n");
            }

            File.WriteAllText(outputDir.Replace("\\", "/").TrimEnd('/') + "/main.ubml", builder.ToString().Replace("\r", ""));
        }

        private static void AppendTypeNode(TypeNode tn, ObjectInfo oi, byte[] objectData, StringBuilder builder, int indentation, bool isLittleEndian) {
            string indent = "";
            for (int i = 0; i < indentation; i++) {
                indent += "    ";
            }
            
            if (tn.IsArray == 1) {
                builder.Append(indent + ".Array: ");
            } else {
                builder.Append(indent + tn.Type + " " + tn.Name + ": ");
            }
            
            if (tn.NumberOfChildren < 1) {
                byte[] data = new byte[tn.ByteSize];
                for (int i = 0; i < tn.ByteSize; i++) {
                    data[i] = objectData[i + tn.Index + oi.ByteStart];
                }

                if (BitConverter.IsLittleEndian != isLittleEndian) {
                    Array.Reverse(data);
                }
                
                builder.AppendLine(GetStringFromTypedBytes(tn.Type, data) + ";");
            } else {
                builder.AppendLine("{");
                foreach (TypeNode node in tn.Children) {
                    AppendTypeNode(node, oi, objectData, builder, indentation + 1, isLittleEndian);
                }
                builder.AppendLine(indent + "}");
            }
        }

        private static string GetStringFromTypedBytes(string type, byte[] data) {
            int byteLength;

            switch (type) {
                case "bool":
                    return data[0] == 0 ? "False" : "True";
                case "SInt8":
                    return ((sbyte)data[0]).ToString();
                case "UInt8":
                    return data[0].ToString();
                case "char":
                    return data[0] > 31 && data[0] < 127 ? Encoding.ASCII.GetString(new byte[] { data[0] }) : "0x" + data[0].ToString("X2");
                case "SInt16":
                case "short":
                case "UInt16":
                case "unsigned short":
                    byteLength = 2;
                    break;
                case "SInt32":
                case "int":
                case "UInt32":
                case "unsigned int":
                case "float":
                    byteLength = 4;
                    break;
                case "SInt64":
                case "long":
                case "UInt64":
                case "unsigned long":
                case "double":
                    byteLength = 8;
                    break;
                default:
                    throw new InvalidOperationException("Unable to convert bytes to a string of the given type");
            }

            byte[] bytes = new byte[byteLength];

            if (BitConverter.IsLittleEndian) {
                for (int i = 0; i < data.Length; i++) {
                    bytes[i] = data[i];
                }
            } else {
                for (int i = 0; i < data.Length; i++) {
                    bytes[bytes.Length - 1 - i] = data[data.Length - 1 - i];
                }
            }

            switch (type) {
                case "SInt16":
                    return BitConverter.ToInt16(bytes, 0).ToString();
                case "short":
                    return BitConverter.ToInt16(bytes, 0).ToString();
                case "UInt16":
                    return BitConverter.ToUInt16(bytes, 0).ToString();
                case "unsigned short":
                    return BitConverter.ToUInt16(bytes, 0).ToString();
                case "SInt32":
                    return BitConverter.ToInt32(bytes, 0).ToString();
                case "int":
                    return BitConverter.ToInt32(bytes, 0).ToString();
                case "UInt32":
                    return BitConverter.ToUInt32(bytes, 0).ToString();
                case "unsigned int":
                    return BitConverter.ToUInt32(bytes, 0).ToString();
                case "float":
                    return BitConverter.ToSingle(bytes, 0).ToString();
                case "SInt64":
                    return BitConverter.ToInt64(bytes, 0).ToString();
                case "long":
                    return BitConverter.ToInt64(bytes, 0).ToString();
                case "UInt64":
                    return BitConverter.ToUInt64(bytes, 0).ToString();
                case "unsigned long":
                    return BitConverter.ToUInt64(bytes, 0).ToString();
                case "double":
                    return BitConverter.ToDouble(bytes, 0).ToString();
                default:
                    throw new InvalidOperationException("Unable to convert bytes to a string of the given type");
            }
        }
    }
}
