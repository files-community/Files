// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;
using IO = System.IO;

namespace Files.App.Utils
{
	public class StorageFolderWithPath : IStorageItemWithPath
	{
		public string Path { get; }
		public string Name => Item?.Name ?? IO.Path.GetFileName(Path);

		IStorageItem IStorageItemWithPath.Item => Item;
		public BaseStorageFolder Item { get; }

		public FilesystemItemType ItemType => FilesystemItemType.Directory;

		public StorageFolderWithPath(BaseStorageFolder folder)
			: this(folder, folder.Path) { }
		public StorageFolderWithPath(BaseStorageFolder folder, string path)
			=> (Item, Path) = (folder, path);
	}
}
