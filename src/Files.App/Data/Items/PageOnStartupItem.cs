// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	public class PageOnStartupItem
	{
		public string Text
			=> ShellHelpers.GetShellNameFromPath(Path);

		public string Path { get; }

		internal PageOnStartupItem(string path)
		{
			Path = path;
		}
	}
}
