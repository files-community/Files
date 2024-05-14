// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
