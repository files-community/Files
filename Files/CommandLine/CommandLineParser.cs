using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.CommandLine
{
    internal class CommandLineParser
    {
        public static ParsedCommands ParseUntrustedCommands(string cmdLineString)
        {
            var commands = new ParsedCommands();

            var parsedArgs = Parse(cmdLineString);

            foreach (var kvp in parsedArgs)
            {
                Debug.WriteLine("arg {0} = {1}", kvp.Key, kvp.Value);

                var command = new ParsedCommand();

                switch (kvp.Key)
                {
                    case "-Directory":
                        command.Type = ParsedCommandType.OpenDirectory;
                        break;

                    default:
                        command.Type = ParsedCommandType.Unkwon;
                        break;
                }

                command.Payload = kvp.Value;
                commands.Add(command);
            }

            return commands;
        }

        public static List<KeyValuePair<string, string>> Parse(string argString = null)
        {
            var parsedArgs = new List<KeyValuePair<string, string>>();

            string[] args = argString.Split(" ");

            if (args != null)
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

            return key != null ? new KeyValuePair<string, string>(key, val) : default(KeyValuePair<string, string>);
        }
    }
}