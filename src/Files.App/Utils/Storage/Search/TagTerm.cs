// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Utils.Storage
{
	public class TagTerm
	{
		public HashSet<string> TagUids { get; set; } = new();

		public bool IsExclude { get; set; }
	}
}
