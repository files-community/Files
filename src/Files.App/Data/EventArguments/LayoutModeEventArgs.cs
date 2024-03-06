// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data.Enums;

namespace Files.App.Data.EventArguments
{
	public class LayoutModeEventArgs
	{
		public readonly FolderLayoutModes LayoutMode;

		internal LayoutModeEventArgs(FolderLayoutModes layoutMode)
		{
			LayoutMode = layoutMode;
		}
	}
}
