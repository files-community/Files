// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
	public sealed class LayoutModeEventArgs
	{
		public readonly FolderLayoutModes LayoutMode;

		internal LayoutModeEventArgs(FolderLayoutModes layoutMode)
		{
			LayoutMode = layoutMode;
		}
	}
}
