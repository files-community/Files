// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public sealed class ShellNewEntry
	{
		public required string Extension { get; set; }

		public required string Name { get; set; }

		public string? Command { get; set; }

		public string? IconBase64 { get; set; }

		public byte[]? Data { get; set; }

		public string? Template { get; set; }
	}
}
