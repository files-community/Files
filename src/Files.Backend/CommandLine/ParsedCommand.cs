// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Files.Backend.CommandLine
{
	public class ParsedCommand
	{
		public ParsedCommandType Type { get; set; }

		public string Payload
			=> Args.FirstOrDefault();

		public List<string> Args { get; set; }

		public ParsedCommand() =>
			Args = new List<string>();
	}
}
