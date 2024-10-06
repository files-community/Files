// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	public interface IFolderView
	{
		int GetSpacing();

		int GetThumbnailSize();

		FolderViewMode GetViewMode();

		uint GetItemsCount();
	}
}
