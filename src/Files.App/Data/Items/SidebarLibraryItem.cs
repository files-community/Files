// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	public class LibraryLocationItem : LocationItem
	{
		public string DefaultSaveFolder { get; }

		public ReadOnlyCollection<string> Folders { get; }

		public bool IsEmpty => DefaultSaveFolder is null || Folders is null || Folders.Count is 0;

		public LibraryLocationItem(ShellLibraryItem shellLibrary)
		{
			Section = SectionType.Library;
			MenuOptions = new SidebarContextFlyoutOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowShellItems = true,
				ShowUnpinItem = !shellLibrary.IsPinned,
			};
			Text = shellLibrary.DisplayName is not null ? shellLibrary.DisplayName : "";
			Path = shellLibrary.FullPath;
			DefaultSaveFolder = shellLibrary.DefaultSaveFolder;
			Folders = shellLibrary.Folders is null ? null : new ReadOnlyCollection<string>(shellLibrary.Folders);
			IsDefaultLocation = shellLibrary.IsPinned;
		}

		public async Task<bool> CheckDefaultSaveFolderAccess()
		{
			if (IsEmpty)
				return false;

			var res = (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(DefaultSaveFolder);

			if (!res)
			{
				var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(DefaultSaveFolder));
				res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(DefaultSaveFolder, item));
			}

			return res;
		}

		public async Task LoadLibraryIconAsync()
		{
			var result = await FileThumbnailHelper.GetIconAsync(
				Path,
				Constants.ShellIconSizes.Small,
				false,
				false,
				IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

			var bitmapImage = await result.IconData.ToBitmapAsync();
			if (bitmapImage is not null)
				Icon = bitmapImage;
		}

		public override int GetHashCode() => Path.GetHashCode(System.StringComparison.OrdinalIgnoreCase);

		public override bool Equals(object obj)
			=> obj is LibraryLocationItem other && GetType() == obj.GetType() && string.Equals(Path, other.Path, System.StringComparison.OrdinalIgnoreCase);
	}
}
