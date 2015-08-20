using System;
using System.IO;
using Modicite.Utilities;
using Modicite.Unity.Serialization;
using Modicite.UBML;

namespace Modicite.CLI {

    class Placeholder {

        static void Main(string[] args) {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Modicite - An API and toolkit for modding Magicite");
            Console.WriteLine("by Greenlock / Nathan2055");
            Console.WriteLine("");
            Console.ResetColor();
            
            Console.WriteLine("Loading class IDs...");
            UnityClassIDDatabase.Load("./classes.txt");

            Console.WriteLine("Loading Runtime Type Information database...");
            UnityRTTIDatabase.Load("./types.dat");
            
            Console.WriteLine("Loading 'level0'...");
            UnityFile uf = UnityFile.Load("./level0", true);

            Console.WriteLine("Cleaning output...");
            if (Directory.Exists("./output")) {
                Directory.Delete("./output", true);
            }
            
            Console.WriteLine("Deconstructing 'level0'...");
            UBMLFileBuilder.Build("./output", uf);

            Console.WriteLine("Done.");
            Console.ReadKey();
        }
    }
}
