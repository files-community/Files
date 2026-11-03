// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	public sealed class FolderAliasItem
	{
		[JsonPropertyName("Name")]
		public string Name { get; }

		[JsonPropertyName("Path")]
		public string Path { get; }

		[JsonConstructor]
		public FolderAliasItem(string name, string path)
		{
			Name = name;
			Path = path;
		}
	}
}
