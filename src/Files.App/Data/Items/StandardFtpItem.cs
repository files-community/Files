// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using FluentFTP;
using System.IO;
using Windows.Storage;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents standard item resides in FTP storage on Windows to be shown on UI.
	/// </summary>
	public sealed class StandardFtpItem : StandardStorageItem
	{
		/// <summary>
		/// Initializes an instance of <see cref="StandardFtpItem"/> class.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="folder"></param>
		public StandardFtpItem(FtpListItem item, string folder) : base()
		{
			var isFile = item.Type == FtpObjectType.File;
			ItemDateCreatedReal = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
			ItemDateModifiedReal = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
			ItemNameRaw = item.Name;
			FileExtension = Path.GetExtension(item.Name);
			ItemPath = PathNormalization.Combine(folder, item.Name);
			PrimaryItemAttribute = isFile ? StorageItemTypes.File : StorageItemTypes.Folder;
			ItemPropertiesInitialized = false;
			FileSizeBytes = item.Size;
			HasChildren = !isFile;
			FileImage = null;
			FileSize = isFile ? FileSizeBytes.ToSizeString() : null;
			Opacity = 1;
			IsHiddenItem = false;

			ItemType = isFile
				? Name.Contains('.', StringComparison.Ordinal)
					? FileExtension.Trim('.') + " " + "File".GetLocalizedResource()
					: "File".GetLocalizedResource()
				: "Folder".GetLocalizedResource();
		}

		[Obsolete("StorageItem is obsolete Storage Layer. Must not use furthermore.")]
		public async Task<IStorageItem> ToStorageItem() => PrimaryItemAttribute switch
		{
			StorageItemTypes.File => await new FtpStorageFile(ItemPath, ItemNameRaw, ItemDateCreatedReal).ToStorageFileAsync(),
			StorageItemTypes.Folder => new FtpStorageFolder(ItemPath, ItemNameRaw, ItemDateCreatedReal),
			_ => throw new InvalidDataException(),
		};
	}
}
