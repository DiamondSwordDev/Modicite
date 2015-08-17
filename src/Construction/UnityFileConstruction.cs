using System.IO;
using System.Text;
using Modicite.Unity.Serialization;

namespace Modicite.Construction {

    static class UnityFileConstruction {

        public static void Deconstruct(string outputDir, UnityFile uf, bool deconstructExternals) {
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

                builder.AppendLine("    IsDestroyed: " + oi.IsDestroyed.ToString() + ";");

                if (oi.ByteSize > 0) {
                    switch (oi.ClassID) {
                        default:
                            builder.Append("    Data:");
                            for (int i = 0; i < oi.ByteSize; i++) {
                                builder.Append(" " + uf.ObjectData[i + oi.ByteStart].ToString("X2"));
                            }
                            builder.AppendLine(";");
                            builder.Append("    TextData: \"");
                            for (int i = 0; i < oi.ByteSize; i++) {
                                builder.Append(Encoding.ASCII.GetString(new byte[] { uf.ObjectData[i + oi.ByteStart] }));
                            }
                            builder.AppendLine("\";");
                            break;
                    }
                }

                builder.AppendLine("}\n");
            }

            File.WriteAllText(outputDir.Replace("\\", "/").TrimEnd('/') + "/main.ubml", builder.ToString().Replace("\r", ""));
        }
    }
}
