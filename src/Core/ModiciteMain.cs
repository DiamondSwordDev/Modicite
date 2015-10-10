using System;
using System.IO;
using System.Collections.Generic;
using Modicite.Unity;
using Modicite.Unity.RTTI;
using System.Security.Cryptography;

namespace Modicite.Core {

    internal static class ModiciteMain {

        static void Main(string[] args) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Modicite v1.0.0\nby Greenlock and Nathan2055\n");
            Console.ResetColor();
            
            UnityFile uf = UnityFile.Load("./level0");
            uf.Save("./level0mod");
            
            Console.ReadKey();

            return;

            CommandLineArguments arguments = CommandLineArguments.Parse(new string[] { "-d", "./game", "./output" });
            if (arguments == null) {
                return;
            }

            if (arguments.Mode == ModeArgument.Help) {
                Help(arguments);
            } else if (arguments.Mode == ModeArgument.Decompile) {
                Decompile(arguments);
            } else {
                Help(arguments);
            }

            Console.ReadKey();
        }

        static string GetSHA256(string filename) {
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(File.ReadAllBytes(filename));
            string hashString = string.Empty;
            foreach (byte x in hash) {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }


        static void Help(CommandLineArguments arguments) {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("==== Available Arguments: ====");

            Console.ResetColor();

            Console.WriteLine("--help (-h)       | Displays this argument list");
            Console.WriteLine("                  |  Usage:  modicite --help");
            Console.WriteLine("--decompile (-d)  | Decompiles the Magicite resources and assemblies");
            Console.WriteLine("                  |  Usage:  modicite --decompile <gameDir> <outputDir>");
            Console.WriteLine("--version (-V)    | Specifies the version of Magicite which is to be decompiled");
            Console.WriteLine("                  |  Usage:  modicite --decompile [--version=<ver>] <gameDir>");
            Console.WriteLine("                  |          <outputDir>");
        }

        static void Decompile(CommandLineArguments arguments) {
            if (arguments.PlainArguments.Count < 1) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Expected Magicite game directory.");
                Console.WriteLine("Correct Usage:  modicite --decompile [--targetVersion=<version>] <gameDir> <outputDir>");
                Console.ResetColor();
                return;
            }

            if (arguments.PlainArguments.Count < 2) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Expected output directory.");
                Console.WriteLine("Correct Usage:  modicite --decompile <gameDir> <outputDir>");
                Console.ResetColor();
                return;
            }
            
            #region Get Target Version

            string targetVersion = ModiciteInfo.NewestSupportedMagiciteVersion;

            if (arguments.OptionalArguments.Contains(OptionalArgument.Version)) {
                if (arguments.OptionalArgumentParameters.ContainsKey(OptionalArgument.Version)) {
                    targetVersion = arguments.OptionalArgumentParameters[OptionalArgument.Version];
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Expected version name.");
                    Console.WriteLine("Correct Usage:  modicite --decompile --version=<ver> <gameDir> <outputDir>");
                    Console.ResetColor();
                    return;
                }
            }

            if (!Directory.Exists("./versions")) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Directory './versions' does not exist. You need to re-install Modicite.");
                Console.ResetColor();
                return;
            }

            if (!File.Exists("./versions/" + targetVersion + ".json")) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No configuration file found for target version '" + targetVersion + "'.");
                Console.WriteLine("If the target version is 'default', then you need to re-install Modicite.");
                Console.WriteLine("If the target version is newer than '" + ModiciteInfo.NewestSupportedMagiciteVersion + "', then you need to update to a newer version of Modicite.");
                Console.ResetColor();
                return;
            }

            TargetFile targetFile = null;
            try {
                targetFile = SimpleJson.SimpleJson.DeserializeObject<TargetFile>(File.ReadAllText("./versions/" + targetVersion + ".json"));
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Failed to load configuration file for target version due to " + ex.GetType().Name + ": " + ex.Message);
                Console.ResetColor();
                return;
            }

            Console.WriteLine("Decompiling with target version '" + targetVersion + "'.");

            #endregion

            #region Create Output Directory

            if (Directory.Exists(arguments.PlainArguments[1])
                && Directory.GetFiles(arguments.PlainArguments[1]).Length > 0
                && Directory.GetDirectories(arguments.PlainArguments[1]).Length > 0) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("The output directory is not empty!");
                Console.ResetColor();
                return;
            }

            if (!Directory.Exists(arguments.PlainArguments[1])) {
                try {
                    Directory.CreateDirectory(arguments.PlainArguments[1]);
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("The output directory could not be created due to " + ex.GetType().Name + ": " + ex.Message);
                    Console.ResetColor();
                    return;
                }
            }

            string outputDir = arguments.PlainArguments[1].TrimEnd(new char[] { '/', '\\' });

            #endregion

            ClassIDDatabase.Load("./classes.txt");

            RTTIDatabase.Load("./types.dat");

            #region Unity Binaries

            Directory.CreateDirectory(outputDir + "/unity-data");

            foreach (TargetFileEntry dataFileEntry in targetFile.unityDataFiles) {
                string entryPath = arguments.PlainArguments[0].TrimEnd(new char[] { '/', '\\' }) + dataFileEntry.path;

                if (!File.Exists(entryPath)) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Missing Unity data file '" + dataFileEntry.name + "'!");
                    Console.ResetColor();
                    continue;
                }

                Console.Write("Loading Unity data file '" + dataFileEntry.name + "'... ");
                UnityFile uf;
                try {
                    uf = UnityFile.Load(entryPath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done.");
                    Console.ResetColor();
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Failed due to " + ex.GetType().Name + ": " + ex.Message);
                    Console.ResetColor();
                    continue;
                }

                Directory.CreateDirectory(outputDir + "./unity-data/" + dataFileEntry.name);

                Console.Write("Exporting file header from '" + dataFileEntry.name + "'... ");
                try {
                    //uf.ExportHeaderToFile(outputDir + "./unity-data/" + dataFileEntry.name + "/header.json");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done.");
                    Console.ResetColor();
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Failed due to " + ex.GetType().Name + ": " + ex.Message);
                    Console.ResetColor();
                    continue;
                }

                List<string> exportFailures = new List<string>();

                int count = 1;
                /*foreach (ObjectInfo oi in uf.Metadata.ObjectInfoList) {
                    Console.CursorLeft = 0;
                    Console.Write("Exporting object data from '" + dataFileEntry.name + "'... " + count.ToString() + "/" + uf.Metadata.NumberOfObjectInfoListMembers.ToString());
                    
                    string objectClassName = oi.ClassID == 114 ? "114" : ClassIDDatabase.Classes[oi.ClassID];
                    
                    if (!Directory.Exists(outputDir + "./unity-data/" + dataFileEntry.name + "/" + objectClassName)) {
                        Directory.CreateDirectory(outputDir + "./unity-data/" + dataFileEntry.name + "/" + objectClassName);
                    }

                    if (oi.ClassID != 198) {
                        try {
                            uf.ExportObjectToFile(oi, outputDir + "./unity-data/" + dataFileEntry.name + "/" + objectClassName + "/" + oi.ObjectID.ToString() + ".json", outputDir + "./unity-data/" + dataFileEntry.name + "/" + objectClassName + "/raw-" + oi.ObjectID.ToString() + ".json");
                        } catch (Exception ex) {
                            exportFailures.Add("Object " + oi.ObjectID.ToString() + " failed to export due to " + ex.GetType().Name + ": " + ex.Message);
                        }
                    } else {
                        try {
                            uf.ExportRawObjectToFile(oi, outputDir + "./unity-data/" + dataFileEntry.name + "/" + objectClassName + "/raw-" + oi.ObjectID.ToString() + ".json");
                        } catch (Exception ex) {
                            exportFailures.Add("Object " + oi.ObjectID.ToString() + " failed to export due to " + ex.GetType().Name + ": " + ex.Message);
                        }
                    }
                    
                    count++;
                }*/
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Done.");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (string exportFailure in exportFailures) {
                    Console.WriteLine(exportFailure);
                }
                Console.ResetColor();
            }

            #endregion
        }
    }
}