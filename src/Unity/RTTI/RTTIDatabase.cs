using System;
using System.Text;
using System.Collections.Generic;
using Modicite.Utilities;

namespace Modicite.Unity.RTTI {

    static class RTTIDatabase {

        public static int Version = -1;
        private static TypeNode[] Types;
        private static string[] Versions;
        private static RTTIDatabaseMapping[] Mappings;


        public static void Load(string filename) {
            if (ClassIDDatabase.Classes == null || ClassIDDatabase.Classes.Count < 1) {
                throw new InvalidOperationException("The class ID database must be loaded before the RTTI database is loaded.");
            }

            DataReader reader = DataReader.OpenFile(filename, 1000000);
            reader.IsLittleEndian = false;

            Version = reader.ReadInt32();
            
            Types = new TypeNode[reader.ReadInt32()];
            for (int i = 0; i < Types.Length; i++) {
                Types[i] = TypeNode.Read(reader);
            }
            
            Versions = new string[reader.ReadInt32()];
            for (int i = 0; i < Versions.Length; i++) {
                Versions[i] = reader.ReadString();
            }

            Mappings = new RTTIDatabaseMapping[reader.ReadInt32()];
            for (int i = 0; i < Mappings.Length; i++) {
                int nodeIndex = reader.ReadInt32();
                int classID = reader.ReadInt32();
                int versionIndex = reader.ReadInt32();
                Mappings[i] = new RTTIDatabaseMapping(nodeIndex, classID, versionIndex);
            }

            reader.Close();
        }
        

        public static TypeNode GetTypeForClassAndVersion(int classID, int versionIndex) {
            foreach (RTTIDatabaseMapping dbm in Mappings) {
                if (dbm.VersionIndex == versionIndex && dbm.ClassID == classID) {
                    return Types[dbm.NodeIndex];
                }
            }
            throw new ArgumentException("No RTTI exists for the given class ID and Unity version.");
        }

        public static TypeNode GetTypeForClassAndVersion(int classID, string versionName) {
            int index = Array.IndexOf(Versions, versionName);
            if (index == -1) {
                throw new ArgumentException("The specified Unity version does not exist in this database.");
            }
            return GetTypeForClassAndVersion(classID, index);
        }

        public static TypeNode GetTypeForClassAndVersion(string className, int versionIndex) {
            foreach (int key in ClassIDDatabase.Classes.Keys) {
                if (ClassIDDatabase.Classes[key] == className) {
                    return GetTypeForClassAndVersion(key, versionIndex);
                }  
            }
            throw new ArgumentException("The specified class does not exist.");
        }

        public static TypeNode GetTypeForClassAndVersion(string className, string versionName) {
            int index = Array.IndexOf(Versions, versionName);
            if (index == -1) {
                throw new ArgumentException("The specified Unity version does not exist in this database.");
            }
            return GetTypeForClassAndVersion(className, index);
        }


        public static TypeNode[] GetTypesForClass(int classID) {
            List<TypeNode> typeNodes = new List<TypeNode>();
            foreach (RTTIDatabaseMapping dbm in Mappings) {
                if (dbm.ClassID == classID) {
                    typeNodes.Add(Types[dbm.NodeIndex]);
                }
            }
            if (typeNodes.Count < 1) {
                throw new ArgumentException("No RTTI instances exist for the given class ID.");
            }
            return typeNodes.ToArray();
        }

        public static TypeNode[] GetTypesForClass(string className) {
            foreach (int key in ClassIDDatabase.Classes.Keys) {
                if (ClassIDDatabase.Classes[key] == className) {
                    return GetTypesForClass(key);
                }
            }
            throw new ArgumentException("The specified class does not exist.");
        }


        public static TypeNode GetNewestTypeForClass(int classID) {
            List<string> versions = new List<string>();
            foreach (RTTIDatabaseMapping dbm in Mappings) {
                if (dbm.ClassID == classID) {
                    versions.Add(Versions[dbm.VersionIndex]);
                }
            }
            if (versions.Count < 1) {
                throw new ArgumentException("No RTTI instances exist for the given class ID.");
            }
            string newest = VersionComparison.GetNewest(versions.ToArray());
            foreach (RTTIDatabaseMapping dbm in Mappings) {
                if (dbm.ClassID == classID && Versions[dbm.VersionIndex] == newest) {
                    return Types[dbm.NodeIndex];
                }
            }
            throw new ArgumentException("No RTTI instances exist for the given class ID and Unity version.");
        }

        public static TypeNode GetNewestTypeForClass(string className) {
            foreach (int key in ClassIDDatabase.Classes.Keys) {
                if (ClassIDDatabase.Classes[key] == className) {
                    return GetNewestTypeForClass(key);
                }
            }
            throw new ArgumentException("The specified class does not exist.");
        }


        public static TypeNode GetTypeForClassVersion(int classID, string version) {
            List<string> versions = new List<string>();
            foreach (RTTIDatabaseMapping dbm in Mappings) {
                if (dbm.ClassID == classID) {
                    versions.Add(Versions[dbm.VersionIndex]);
                }
            }
            if (versions.Count < 1) {
                throw new ArgumentException("No RTTI instances exist for the given class ID.");
            }

            List<string> possibleVersions = new List<string>();
            foreach (string v in versions) {
                if (v == version || VersionComparison.GetNewest(new string[] { v, version }) != v) {
                    possibleVersions.Add(v);
                }
            }

            string newest = VersionComparison.GetNewest(possibleVersions.ToArray());
            foreach (RTTIDatabaseMapping dbm in Mappings) {
                if (dbm.ClassID == classID && Versions[dbm.VersionIndex] == newest) {
                    return Types[dbm.NodeIndex];
                }
            }
            throw new ArgumentException("No RTTI instances exist for the given class ID and Unity version.");
        }

        public static TypeNode GetTypeForClassVersion(string className, string version) {
            foreach (int key in ClassIDDatabase.Classes.Keys) {
                if (ClassIDDatabase.Classes[key] == className) {
                    return GetTypeForClassVersion(key, version);
                }
            }
            throw new ArgumentException("The specified class does not exist.");
        }
    }
}
