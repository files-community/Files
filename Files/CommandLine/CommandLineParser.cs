using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Files.CommandLine
{
    internal class CommandLineParser
    {
        public static ParsedCommands ParseUntrustedCommands(string cmdLineString)
        {
            var parsedArgs = Parse(SplitArguments(cmdLineString));
            return ParseSplitArguments(parsedArgs);
        }

        public static ParsedCommands ParseUntrustedCommands(string[] cmdLineStrings)
        {
            var parsedArgs = Parse(cmdLineStrings);
            return ParseSplitArguments(parsedArgs);
        }

        private static ParsedCommands ParseSplitArguments(List<KeyValuePair<string, string>> parsedArgs)
        {
            var commands = new ParsedCommands();

            foreach (var kvp in parsedArgs)
            {
                Debug.WriteLine("arg {0} = {1}", kvp.Key, kvp.Value);

                var command = new ParsedCommand();

                switch (kvp.Key)
                {
                    case string s when "-Directory".Equals(s, StringComparison.OrdinalIgnoreCase):
                        command.Type = ParsedCommandType.OpenDirectory;
                        break;

                    case string s when "-OutputPath".Equals(s, StringComparison.OrdinalIgnoreCase):
                        command.Type = ParsedCommandType.OutputPath;
                        break;

                    case string s when "-Select".Equals(s, StringComparison.OrdinalIgnoreCase):
                        command.Type = ParsedCommandType.SelectItem;
                        break;

                    default:
                        //case "-Cmdless":
                        try
                        {
                            if (kvp.Value.StartsWith("::{") || kvp.Value.StartsWith("shell:"))
                            {
                                command.Type = ParsedCommandType.ExplorerShellCommand;
                            }
                            else if (Path.IsPathRooted(kvp.Value))
                            {
                                command.Type = ParsedCommandType.OpenPath;
                            }
                            else
                            {
                                command.Type = ParsedCommandType.Unknown;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Exception in CommandLineParser.cs\\ParseUntrustedCommands with message: {ex.Message}");
                            command.Type = ParsedCommandType.Unknown;
                        }
                        break;
                }

                command.Payload = kvp.Value;
                commands.Add(command);
            }

            return commands;
        }

        private static string[] SplitArguments(string commandLine)
        {
            char[] commandLineCharArray = commandLine.ToCharArray();
            bool isInQuote = false;

            for (int i = 0; i < commandLineCharArray.Length; i++)
            {
                if (commandLineCharArray[i] == '"')
                {
                    isInQuote = !isInQuote;
                }

                if (!isInQuote && commandLineCharArray[i] == ' ')
                {
                    commandLineCharArray[i] = '\n';
                }
            }

            return new string(commandLineCharArray).Replace("\"", "").Split('\n');
        }

        public static List<KeyValuePair<string, string>> Parse(string[] args = null)
        {
            var parsedArgs = new List<KeyValuePair<string, string>>();
            //Environment.GetCommandLineArgs() IS better but... I haven't tested this enough.

            if (args != null)
            {
                //if - or / are not used then add the command as-is

                if (args.Length > 2)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                        {
                            var data = ParseData(args, i);

                            if (data.Key != null)
                            {
                                for (int j = 0; j < parsedArgs.Count; j++)
                                {
                                    if (parsedArgs[j].Key == data.Key)
                                    {
                                        parsedArgs.RemoveAt(j);
                                    }
                                }

                                parsedArgs.Add(data);
                            }
                        }
                    }
                }
            }

            if (parsedArgs.Count == 0 && args.Length >= 2)
            {
                parsedArgs.Add(new KeyValuePair<string, string>("-Cmdless", string.Join(" ", args.Skip(1)).TrimStart()));
            }

            return parsedArgs;
        }

        private static KeyValuePair<string, string> ParseData(string[] args, int index)
        {
            string key = null;
            string val = null;
            if (args[index].StartsWith("-") || args[index].StartsWith("/"))
            {
                if (args[index].Contains(":"))
                {
                    string argument = args[index];
                    int endIndex = argument.IndexOf(':');
                    key = argument.Substring(1, endIndex - 1);   // trim the '/' and the ':'.
                    int valueStart = endIndex + 1;
                    val = valueStart < argument.Length ? argument.Substring(
                        valueStart, argument.Length - valueStart) : null;
                }
                else
                {
                    key = args[index];
                    int argIndex = 1 + index;
                    if (argIndex < args.Length && !(args[argIndex].StartsWith("-") || args[argIndex].StartsWith("/")))
                    {
                        val = args[argIndex];
                    }
                    else
                    {
                        val = null;
                    }
                }
            }

            return key != null ? new KeyValuePair<string, string>(key, val) : default;
        }
    }
}