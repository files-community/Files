// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.StorageItems
{
	public class BaseBasicProperties : BaseStorageItemExtraProperties
	{
		public virtual ulong Size => 0;

		public virtual DateTimeOffset ItemDate => DateTimeOffset.Now;
		public virtual DateTimeOffset DateModified => DateTimeOffset.Now;
	}
}
