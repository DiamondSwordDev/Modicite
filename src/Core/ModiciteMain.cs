using System;
using System.IO;

namespace Modicite.Core {

    internal static class ModiciteMain {

        static void Main(string[] args) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Modicite v1.0.0\nby Greenlock and Nathan2055\n");
            Console.ResetColor();

            CommandLineArguments arguments = CommandLineArguments.Parse(args);
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
        }


        static void Help(CommandLineArguments arguments) {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("==== Available Arguments: ====\n");

            Console.ResetColor();

            Console.WriteLine("--help (-h)       | Displays this argument list");
            Console.WriteLine("                  |  Usage:  modicite --help");
            Console.WriteLine("--decompile (-d)  | Decompiles the Magicite resources and assemblies");
            Console.WriteLine("                  |  Usage:  modicite --decompile <gameDir> <outputDir>");
            Console.WriteLine("--version (-V)    | Specifies the version of Magicite which is to be decompiled");
            Console.WriteLine("                  |  Usage:  modicite --decompile [--version=<ver>] <gameDir> <outputDir>");
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

            string targetVersion = "default";

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
        }
    }
}