// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Shared
{
	public class ShellNewEntry
	{
		public string Extension { get; set; }
		public string Name { get; set; }
		public string Command { get; set; }
		public string IconBase64 { get; set; }
		public byte[] Data { get; set; }
		public string Template { get; set; }
	}
}
