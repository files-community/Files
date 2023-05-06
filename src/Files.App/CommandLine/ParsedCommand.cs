// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.CommandLine
{
	internal class ParsedCommand
	{
		public ParsedCommandType Type { get; set; }

		public string Payload
			=> Args.FirstOrDefault();

		public List<string> Args { get; set; }

		public ParsedCommand() =>
			Args = new List<string>();
	}
}
