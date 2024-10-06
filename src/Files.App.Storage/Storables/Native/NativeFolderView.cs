// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage.Storables
{
	/// <summary>
	/// Represents a folder object that is natively supported by Windows Shell API.
	/// </summary>
	public class NativeFolderView : IFolderView
    {
		public int GetSpacing()
        {
            return 0;
        }

		public int GetThumbnailSize()
        {
            return 0;
        }

		public FolderViewMode GetViewMode()
        {
            return 0;
        }

		public uint GetItemsCount()
        {
            return 0;
        }
	}
}
