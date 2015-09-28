using System;
using System.Collections.Generic;

sealed internal class CommandLineArguments {

    public List<string> PlainArguments = new List<string>();
    public ModeArgument Mode = ModeArgument.None;
    public List<OptionalArgument> OptionalArguments = new List<OptionalArgument>();
    public Dictionary<OptionalArgument, string> OptionalArgumentParameters = new Dictionary<OptionalArgument, string>();


    private CommandLineArguments() {

    }

    public static CommandLineArguments Parse(string[] args) {
        Dictionary<string, ModeArgument> modeAliases = new Dictionary<string, ModeArgument>();
        modeAliases["help"] = ModeArgument.Help;
        modeAliases["h"] = ModeArgument.Help;
        modeAliases["?"] = ModeArgument.Help;

        Dictionary<string, OptionalArgument> optionalAliases = new Dictionary<string, OptionalArgument>();

        CommandLineArguments arguments = new CommandLineArguments();

        foreach (string _arg in args) {
            string arg = _arg.Contains("=") ? _arg.Split('=')[0] : _arg;

            if (arg.StartsWith("--")) {
                if (modeAliases.ContainsKey(arg.Substring(2).ToLower())) {
                    arguments.Mode = modeAliases[arg.Substring(2).ToLower()];
                } else if (optionalAliases.ContainsKey(arg.Substring(2).ToLower())) {
                    arguments.OptionalArguments.Add(optionalAliases[arg.Substring(2).ToLower()]);
                    if (_arg.Contains("=")) {
                        arguments.OptionalArgumentParameters[optionalAliases[arg.Substring(2).ToLower()]] = _arg.Substring(arg.Length + 1);
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Unrecognized argument '" + arg + "'.\nRun 'modicite --help' for a list of available arguments.");
                    Console.ResetColor();
                    return null;
                }
            } else if (arg.StartsWith("-")) {
                char[] shortArgs = arg.Substring(1).ToCharArray();
                foreach (char shortArg in shortArgs) {
                    if (modeAliases.ContainsKey(shortArg.ToString())) {
                        arguments.Mode = modeAliases[shortArg.ToString()];
                    } else if (optionalAliases.ContainsKey(shortArg.ToString())) {
                        arguments.OptionalArguments.Add(optionalAliases[shortArg.ToString()]);
                        if (_arg.Contains("=")) {
                            arguments.OptionalArgumentParameters[optionalAliases[shortArg.ToString()]] = _arg.Substring(3);
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Unrecognized argument '" + arg + "'.\nRun 'modicite --help' for a list of available arguments.");
                        Console.ResetColor();
                        return null;
                    }
                }
            } else if (arg.StartsWith("/")) {
                if (modeAliases.ContainsKey(arg.Substring(1))) {
                    arguments.Mode = modeAliases[arg.Substring(1)];
                } else if (optionalAliases.ContainsKey(arg.Substring(1))) {
                    arguments.OptionalArguments.Add(optionalAliases[arg.Substring(1)]);
                    if (_arg.Contains("=")) {
                        arguments.OptionalArgumentParameters[optionalAliases[arg.Substring(1)]] = _arg.Substring(arg.Length + 1);
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Unrecognized argument '" + arg + "'.\nRun 'modicite --help' for a list of available arguments.");
                    Console.ResetColor();
                    return null;
                }
            }
        }

        return arguments;
    }
}

internal enum ModeArgument {
    None, Help, Decompile
}

internal enum OptionalArgument {
    //None
}
