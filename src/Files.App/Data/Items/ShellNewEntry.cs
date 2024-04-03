﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	public sealed class ShellNewEntry
	{
		public string Extension { get; set; }

		public string Name { get; set; }

		public string Command { get; set; }

		public string IconBase64 { get; set; }

		public byte[] Data { get; set; }

		public string Template { get; set; }
	}
}
