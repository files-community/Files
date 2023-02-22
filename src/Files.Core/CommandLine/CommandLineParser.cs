using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Files.Core.CommandLine
{
	public class CommandLineParser
	{
		public static ParsedCommands ParseUntrustedCommands(string cmdLineString)
		{
			var parsedArgs = Parse(SplitArguments(cmdLineString, true));
			return ParseSplitArguments(parsedArgs);
		}

		public static ParsedCommands ParseUntrustedCommands(string[] cmdLineStrings)
		{
			var parsedArgs = Parse(cmdLineStrings);
			return ParseSplitArguments(parsedArgs);
		}

		private static ParsedCommands ParseSplitArguments(List<KeyValuePair<string, string[]>> parsedArgs)
		{
			var commands = new ParsedCommands();

			foreach (var kvp in parsedArgs)
			{
				Debug.WriteLine("arg {0} = {1}", kvp.Key, kvp.Value);

				var command = new ParsedCommand();

				switch (kvp.Key)
				{
					case string s when "Directory".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.OpenDirectory;
						break;

					case string s when "OutputPath".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.OutputPath;
						break;

					case string s when "Select".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.SelectItem;
						break;

					case string s when "Tag".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.TagFiles;
						break;

					default:
						//case "Cmdless":
						try
						{
							if (kvp.Value[0].StartsWith("::{", StringComparison.Ordinal) || kvp.Value[0].StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
							{
								command.Type = ParsedCommandType.ExplorerShellCommand;
							}
							else if (Path.IsPathRooted(kvp.Value[0]))
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

				command.Args.AddRange(kvp.Value);
				commands.Add(command);
			}

			return commands;
		}

		public static string[] SplitArguments(string commandLine, bool trimQuotes = false)
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

			return trimQuotes
				? new string(commandLineCharArray).Replace("\"", "", StringComparison.Ordinal).Split('\n')
				: new string(commandLineCharArray).Split('\n');
		}

		public static List<KeyValuePair<string, string[]>> Parse(string[] args = null)
		{
			var parsedArgs = new List<KeyValuePair<string, string[]>>();
			//Environment.GetCommandLineArgs() IS better but... I haven't tested this enough.

			if (args is not null)
			{
				//if - or / are not used then add the command as-is

				if (args.Length > 2)
				{
					for (int i = 0; i < args.Length; i++)
					{
						if (args[i].StartsWith('-') || args[i].StartsWith('/'))
						{
							var data = ParseData(args, i);

							if (data.Key is not null)
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
				parsedArgs.Add(new KeyValuePair<string, string[]>("Cmdless", new[] { string.Join(' ', args.Skip(1)).TrimStart() }));
			}

			return parsedArgs;
		}

		private static KeyValuePair<string, string[]> ParseData(string[] args, int index)
		{
			string? key = null;
			var val = new List<string>();

			if (args[index].StartsWith('-') || args[index].StartsWith('/'))
			{
				if (args[index].Contains(':', StringComparison.Ordinal))
				{
					string argument = args[index];
					int endIndex = argument.IndexOf(':');

					// Trim the '/' and the ':'
					key = argument.Substring(1, endIndex - 1);

					int valueStart = endIndex + 1;
					val.Add(valueStart < argument.Length
						 ? argument.Substring(valueStart, argument.Length - valueStart)
						 : null);
				}
				else
				{
					key = args[index].Substring(1);
				}

				int argIndex = 1 + index;
				while (argIndex < args.Length && !(args[argIndex].StartsWith('-') || args[argIndex].StartsWith('/')))
				{
					val.Add(args[argIndex++]);
				}
			}

			return key is not null ? new KeyValuePair<string, string[]>(key, val.ToArray()) : default;
		}
	}
}
