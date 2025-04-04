// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace Files.App.Utils
{
	public interface IListedItem
	{
		GitItem AsGitItem { get; }
		RecycleBinItem AsRecycleBinItem { get; }
		bool ContainsFilesOrFolders { get; set; }
		string ContextualProperty { get; set; }
		BitmapImage CustomIcon { get; set; }
		Uri CustomIconSource { get; set; }
		ObservableCollection<FileProperty> FileDetails { get; set; }
		string FileExtension { get; set; }
		ulong? FileFRN { get; set; }
		BitmapImage FileImage { get; set; }
		string FileSize { get; set; }
		long FileSizeBytes { get; set; }
		string FileSizeDisplay { get; }
		string[] FileTags { get; set; }
		IList<TagViewModel>? FileTagsUI { get; }
		string FileVersion { get; set; }
		string FolderRelativeId { get; set; }
		bool HasTags { get; set; }
		BitmapImage IconOverlay { get; set; }
		string ImageDimensions { get; set; }
		bool IsAlternateStream { get; }
		bool IsArchive { get; }
		bool IsDriveRoot { get; }
		bool IsElevationRequired { get; set; }
		bool IsExecutable { get; }
		bool IsFolder { get; }
		bool IsFtpItem { get; }
		bool IsGitItem { get; }
		bool IsHiddenItem { get; set; }
		bool IsItemPinnedToStart { get; }
		bool IsLibrary { get; }
		bool IsLinkItem { get; }
		bool IsPinned { get; }
		bool IsRecycleBinItem { get; }
		bool IsScriptFile { get; }
		bool IsShortcut { get; }
		string ItemDateAccessed { get; }
		DateTimeOffset ItemDateAccessedReal { get; set; }
		string ItemDateCreated { get; }
		DateTimeOffset ItemDateCreatedReal { get; set; }
		string ItemDateModified { get; }
		DateTimeOffset ItemDateModifiedReal { get; set; }
		BaseStorageFile ItemFile { get; set; }
		string ItemNameRaw { get; set; }
		string ItemPath { get; set; }
		ObservableCollection<FileProperty> ItemProperties { get; set; }
		bool ItemPropertiesInitialized { get; set; }
		string ItemTooltipText { get; }
		string ItemType { get; set; }
		string Key { get; set; }
		bool LoadCustomIcon { get; set; }
		bool LoadFileIcon { get; set; }
		ByteSizeLib.ByteSize MaxSpace { get; set; }
		string MediaDuration { get; set; }
		string Name { get; }
		bool NeedsPlaceholderGlyph { get; set; }
		double Opacity { get; set; }
		StorageItemTypes PrimaryItemAttribute { get; set; }
		BitmapImage ShieldIcon { get; set; }
		bool ShowDriveStorageDetails { get; set; }
		ByteSizeLib.ByteSize SpaceUsed { get; set; }
		string SyncStatusString { get; }
		CloudDriveSyncStatusUI SyncStatusUI { get; set; }

		string ToString();
		void UpdateContainsFilesFolders();
	}
}