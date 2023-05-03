// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class LayoutModeEventArgs
	{
		public readonly FolderLayoutModes LayoutMode;

		public readonly int GridViewSize;

		internal LayoutModeEventArgs(FolderLayoutModes layoutMode, int gridViewSize)
		{
			LayoutMode = layoutMode;
			GridViewSize = gridViewSize;
		}
	}
}
