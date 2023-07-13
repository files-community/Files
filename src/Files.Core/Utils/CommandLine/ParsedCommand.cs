// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Utils.CommandLine
{
	/// <summary>
	/// Represents a parsed command node on Windows.
	/// </summary>
	public class ParsedCommand
	{
		/// <summary>
		/// Gets or sets parsed command type.
		/// </summary>
		public ParsedCommandType Type { get; set; }

		/// <summary>
		/// Gets or sets list of arguments.
		/// </summary>
		public List<string> Args { get; set; }

		/// <summary>
		/// Gets first argument item.
		/// </summary>
		public string Payload
			=> Args.First();

		/// <summary>
		/// Initialize a parsed command class.
		/// </summary>
		public ParsedCommand()
		{
			Args = new();
		}
	}
}
