using System;

internal static class CommandLineInterface {

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
        Console.WriteLine("==== Command Usages: ====\n");

        Console.ResetColor();
        Console.WriteLine("--help (-h)    | Displays this argument list");
    }

    static void Decompile(CommandLineArguments arguments) {

    }
}
