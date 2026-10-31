// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public sealed partial class LibraryLocationItem : LocationItem
	{
		public string? DefaultSaveFolder { get; }

		public ReadOnlyCollection<string> Folders { get; }

		public bool IsEmpty => string.IsNullOrEmpty(DefaultSaveFolder) || Folders.Count is 0;

		public LibraryLocationItem(ShellLibraryItem shellLibrary)
		{
			Section = SectionType.Library;
			MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowShellItems = true,
				ShowUnpinItem = !shellLibrary.IsPinned,
			};
			Text = shellLibrary.DisplayName
				?? throw new InvalidOperationException("The library does not have a display name.");
			Path = shellLibrary.FullPath
				?? throw new InvalidOperationException("The library does not have a path.");
			DefaultSaveFolder = shellLibrary.DefaultSaveFolder;
			Folders = new ReadOnlyCollection<string>(shellLibrary.Folders ?? []);
			IsDefaultLocation = shellLibrary.IsPinned;
		}

		public async Task<bool> CheckDefaultSaveFolderAccess()
		{
			var defaultSaveFolder = DefaultSaveFolder;
			if (string.IsNullOrEmpty(defaultSaveFolder) || Folders.Count is 0)
				return false;

			var res = (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(defaultSaveFolder);

			if (!res)
			{
				var item = await FilesystemTasks.WrapNullable(() => DriveHelpers.GetRootFromPathAsync(defaultSaveFolder));
				res = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(defaultSaveFolder, item));
			}

			return res;
		}

		public async Task LoadLibraryIconAsync()
		{
			var path = this.GetRequiredPath();
			var result = await FileThumbnailHelper.GetIconAsync(
				path,
				Constants.ShellIconSizes.Small,
				false,
				IconOptions.ReturnIconOnly);

			var bitmapImage = await result.ToBitmapAsync();
			if (bitmapImage is not null)
				Icon = bitmapImage;
		}

		public override int GetHashCode()
			=> this.GetRequiredPath().GetHashCode(System.StringComparison.OrdinalIgnoreCase);

		public override bool Equals(object? obj)
			=> obj is LibraryLocationItem other && GetType() == obj.GetType() && string.Equals(Path, other.Path, System.StringComparison.OrdinalIgnoreCase);
	}
}
