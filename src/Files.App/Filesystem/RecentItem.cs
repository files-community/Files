using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Storage.NativeStorage;
using Files.App.UserControls.Widgets;
using Files.Sdk.Storage.LocatableStorage;
using Files.Shared;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Filesystem
{
	public class RecentItem : WidgetCardItem, IEquatable<RecentItem>, ILocatableStorable
	{
		private BitmapImage _fileImg;
		public BitmapImage FileImg
		{
			get => _fileImg;
			set => SetProperty(ref _fileImg, value);
		}
		public string Id => LinkPath;
		public string LinkPath { get; set; }    // path of shortcut item (this is unique)
		public string RecentPath { get; set; }  // path to target item
		public string Name { get; set; }
		public StorageItemTypes Type { get; set; }
		public bool FolderImg { get; set; }
		public bool EmptyImgVis { get; set; }
		public bool FileIconVis { get; set; }
		public bool IsFile { get => Type == StorageItemTypes.File; }
		public DateTime LastModified { get; set; }
		public byte[] PIDL { get; set; }
		public string Path { get => RecentPath; }

		public RecentItem()
		{
			EmptyImgVis = true; // defer icon load to LoadRecentItemIcon()
		}

		/// <summary>
		/// Create a RecentItem instance from a link path.
		/// This is usually needed if a shortcut is deleted -- the metadata is lost (i.e. the target item).
		/// </summary>
		/// <param name="linkPath">The location that shortcut lives/lived in</param>
		public RecentItem(string linkPath) : base()
		{
			LinkPath = linkPath;
		}

		/// <summary>
		/// Create a RecentItem from a ShellLinkItem (usually from shortcuts in `Windows\Recent`)
		/// </summary>
		public RecentItem(ShellLinkItem linkItem) : base()
		{
			LinkPath = linkItem.FilePath;
			RecentPath = linkItem.TargetPath;
			Name = NameOrPathWithoutExtension(linkItem.FileName);
			Type = linkItem.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
			FolderImg = linkItem.IsFolder;
			FileIconVis = !linkItem.IsFolder;
			LastModified = linkItem.ModifiedDate;
			PIDL = linkItem.PIDL;
		}

		/// <summary>
		/// Create a RecentItem from a ShellFileItem (usually from enumerating Quick Access directly).
		/// </summary>
		/// <param name="fileItem">The shell file item</param>
		public RecentItem(ShellFileItem fileItem) : base()
		{
			LinkPath = ShellStorageFolder.IsShellPath(fileItem.FilePath) ? fileItem.RecyclePath : fileItem.FilePath; // use true path on disk for shell items
			RecentPath = LinkPath; // intentionally the same
			Name = NameOrPathWithoutExtension(fileItem.FileName);
			Type = fileItem.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
			FolderImg = fileItem.IsFolder;
			FileIconVis = !fileItem.IsFolder;
			LastModified = fileItem.ModifiedDate;
			PIDL = fileItem.PIDL;
		}

		public async Task LoadRecentItemIcon()
		{
			var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(RecentPath, 96u, ThumbnailMode.SingleItem);
			if (iconData is null)
			{
				EmptyImgVis = true;
			}
			else
			{
				EmptyImgVis = false;
				FileImg = await iconData.ToBitmapAsync();
			}
		}

		/// <summary>
		/// Test equality for generic collection methods such as Remove(...)
		/// </summary>
		public bool Equals(RecentItem other)
		{
			if (other is null)
			{
				return false;
			}

			// do not include LastModified or anything else here; otherwise, Remove(...) will fail since we lose metadata on deletion!
			// when constructing a RecentItem from a deleted link, the only thing we have is the LinkPath (where the link use to be)
			return LinkPath == other.LinkPath &&
				   RecentPath == other.RecentPath;
		}

		public override int GetHashCode() => (LinkPath, RecentPath).GetHashCode();
		public override bool Equals(object? o) => o is RecentItem other && Equals(other);

		/**
		 * Strips a name from an extension while aware of some edge cases.
		 *
		 *   example.min.js => example.min
		 *   example.js     => example
		 *   .gitignore     => .gitignore
		 */
		private static string NameOrPathWithoutExtension(string nameOrPath)
		{
			string strippedExtension = System.IO.Path.GetFileNameWithoutExtension(nameOrPath);
			return string.IsNullOrEmpty(strippedExtension) ? System.IO.Path.GetFileName(nameOrPath) : strippedExtension;
		}

		private const string QuickAccessGuid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

		public async Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			return await Task.FromResult(new NativeFolder(QuickAccessGuid));
		}
	}
}
