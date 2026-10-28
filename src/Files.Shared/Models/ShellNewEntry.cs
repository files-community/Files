// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Shared
{
	public sealed class ShellNewEntry
	{
		public string? Extension { get; set; }

		public string? Name { get; set; }

		public string? Command { get; set; }

		public byte[]? IconData { get; set; }

		public byte[]? Data { get; set; }

		public string? Template { get; set; }
	}
}
