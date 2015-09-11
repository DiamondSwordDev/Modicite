using System;
using System.IO;
using System.Collections.Generic;

namespace Modicite.Unity.Serialization {

    static class UnityClassIDDatabase {

        public static Dictionary<int, string> Classes = new Dictionary<int, string>();

        public static void Load(string filename) {
            string contents = File.ReadAllText(filename).Replace("\r", "");
            contents = contents.Replace("\t", " ");
            while (contents.Contains("  ")) {
                contents = contents.Replace("  ", " ");
            }
            foreach (string line in contents.Split('\n')) {
                if (line != "") {
                    Classes[Convert.ToInt32(line.Split(' ')[0])] = line.Split(' ')[1];
                }
            }
        }
    }
}
