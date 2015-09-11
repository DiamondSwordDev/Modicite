using System;
using System.Collections;
using System.Text;

namespace Modicite.Utilities {

    public static class VersionComparison {

        public static string GetNewest(string[] versions) {
            string newest = "0";
            foreach (string version in versions) {
                for (int i = 0; i < Math.Max(version.Length, newest.Length); i++) {
                    if (version.Length <= i && newest.Length > i) {
                        break;
                    } else if (newest.Length <= i && version.Length > i) {
                        newest = version;
                        break;
                    }

                    if (Char.IsDigit(version[i])) {
                        if (Convert.ToInt32(version[i]) > Convert.ToInt32(newest[i])) {
                            newest = version;
                            break;
                        }
                    } else if (Char.IsLetter(version[i])) {
                        if (Encoding.UTF8.GetBytes(new char[] { version[i] })[0] > Encoding.UTF8.GetBytes(new char[] { newest[i] })[0]) {
                            newest = version;
                            break;
                        }
                    }
                }
            }
            return newest;
        }
    }
}
