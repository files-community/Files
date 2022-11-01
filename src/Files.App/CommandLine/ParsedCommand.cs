using System.Collections.Generic;
using System.Linq;

namespace Files.App.CommandLine
{
	internal class ParsedCommand
	{
		public ParsedCommandType Type { get; set; }

		public string Payload => Args.FirstOrDefault();

		public List<string> Args { get; set; }

		public ParsedCommand() =>
			Args = new List<string>();
	}
}