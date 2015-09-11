using System;
using System.IO;
using System.Collections.Generic;
using Modicite.Utilities;
using Modicite.Unity.Serialization;
using Modicite.Json;

sealed internal class CommandLineInterface {

    static void Main(string[] args) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Modicite v1.0.0.0\nby Greenlock and Nathan2055\n");
        Console.ResetColor();

        //////////////////////////////////////////////////////////////////////

        Console.WriteLine("Loading class IDs...");
        UnityClassIDDatabase.Load("./classes.txt");

        Console.WriteLine("Loading Runtime Type Information database...");
        UnityRTTIDatabase.Load("./types.dat");

        Console.WriteLine("Loading Unity File...");
        UnityFile uf = UnityFile.Load("./level0", true);

        Console.WriteLine("Cleaning output...");
        if (Directory.Exists("./dump")) {
            Directory.Delete("./dump", true);
        }

        Console.WriteLine("Exporting Unity File...");
        uf.ExportToJSONDirectory("./dump");

        Console.ReadKey();
        Environment.Exit(0);

        /////////////////////////////////////////////////////////////////////

        if (args.Length < 1) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please specify an operation to perform.\nRun 'modicite help' for a list of available operations.");
            Console.ResetColor();
            Environment.Exit(0);
        }

        List<string> passedArguments = new List<string>();
        passedArguments.AddRange(args);

        if (args[0].ToLower() == "help") {
            Main_help(passedArguments);
        } else if (args[0].ToLower() == "unitybin") {
            Main_unitybin(passedArguments);
        } else {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Unknown operation '" + args[0] + "'.\nRun 'modicite help' for a list of available operations.");
        }
    }

    static void Main_help(List<string> args) {
        args.RemoveAt(0);
    }

    static void Main_unitybin(List<string> args) {
        args.RemoveAt(0);

        if (args.Count < 1) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please specify a sub-operation for the category 'unitybin'.\nRun 'modicite help' for a list of available operations.");
            Console.ResetColor();
            Environment.Exit(0);
        }

        if (args[0].ToLower() == "dumplengths") {

        }
    }

    static string GetNewestVersionDirectory() {
        List<string> versions = new List<string>();
        foreach (string dir in Directory.GetDirectories("./game")) {
            versions.Add(new DirectoryInfo(dir).Name);
        }
        return "./game/" + VersionComparison.GetNewest(versions.ToArray());
    }
}
