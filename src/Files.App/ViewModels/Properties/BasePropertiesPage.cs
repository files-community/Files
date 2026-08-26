// Copyright(c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TagLib;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public abstract class BasePropertiesPage : Page, IDisposable
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private IShellPage? appInstance;
		public IShellPage AppInstance
			=> appInstance ?? throw new InvalidOperationException("The properties page has not been initialized.");

		public BaseProperties? BaseProperties { get; set; }

		public SelectedItemsPropertiesViewModel ViewModel { get; set; } = new();

		protected virtual void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			BaseProperties?.GetSpecialPropertiesAsync();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			var tokenSource = np.CancellationTokenSource
				?? throw new InvalidOperationException("The properties page does not have a cancellation token source.");
			var instance = np.AppInstance
				?? throw new InvalidOperationException("The properties page does not have an associated shell page.");
			var window = np.Window
				?? throw new InvalidOperationException("The properties page does not have an associated window.");
			appInstance = instance;

			// Library
			if (np.Parameter is LibraryItem library)
				BaseProperties = new LibraryProperties(ViewModel, tokenSource, DispatcherQueue, library, instance);
			// Drive
			else if (np.Parameter is DriveItem drive)
			{
				var props = new DriveProperties(ViewModel, tokenSource, DispatcherQueue, drive, instance);
				BaseProperties = props;
				var drivePath = props.Drive.GetRequiredPath();

				ViewModel.CleanupVisibility = props.Drive.Type != DriveType.Network && props.Drive.Type != DriveType.CloudDrive;
				ViewModel.FormatVisibility = !(props.Drive.Type == DriveType.Network || props.Drive.Type == DriveType.CloudDrive || string.Equals(props.Drive.Path, $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\", StringComparison.OrdinalIgnoreCase));
				ViewModel.CleanupDriveCommand = new AsyncRelayCommand(() => StorageSenseHelper.OpenStorageSenseAsync(drivePath));
				ViewModel.FormatDriveCommand = new RelayCommand(async () =>
				{
					try
					{
						await Win32Helper.OpenFormatDriveDialog(drivePath);
					}
					catch (Exception)
					{
					}
				});
			}
			// Storage objects (multi-selected)
			else if (np.Parameter is List<ListedItem> items)
			{
				// Selection only contains files
				if (items.All(item => item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive))
				{
					BaseProperties = new CombinedFileProperties(ViewModel, tokenSource, DispatcherQueue, items, instance);

					ViewModel.IsEditAlbumCoverVisible =
						items.All(item => item.FileExtension is not ".avi") && (
						items.All(item => FileExtensionHelpers.IsVideoFile(item.FileExtension)) ||
						items.All(item => FileExtensionHelpers.IsAudioFile(item.FileExtension)));
				}
				// Selection includes folders
				else
					BaseProperties = new CombinedProperties(ViewModel, tokenSource, DispatcherQueue, items, instance);
			}
			// A storage object
			else if (np.Parameter is ListedItem item)
			{
				// File or Archive
				if (item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
					BaseProperties = new FileProperties(ViewModel, tokenSource, DispatcherQueue, item, instance);
				// Folder
				else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
					BaseProperties = new FolderProperties(ViewModel, tokenSource, DispatcherQueue, item, instance);
			}

			ViewModel.EditAlbumCoverCommand = new RelayCommand(async () =>
			{
				var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);

				string[] extensions =
				[
					Strings.ImageFiles.GetLocalizedResource(), "*.bmp;*.jpg;*.jpeg;*.png",
					Strings.BitmapFiles.GetLocalizedResource(), "*.bmp",
					"JPEG", "*.jpg;*.jpeg",
					"PNG", "*.png",
				];

				var result = CommonDialogService.Open_FileOpenDialog(hWnd, false, extensions, Environment.SpecialFolder.Desktop, out var filePath);
				if (result)
				{
					ViewModel.IsAblumCoverModified = true;
					ViewModel.ModifiedAlbumCover = new Picture(filePath);

					var iconData = await FileThumbnailHelper.GetIconAsync(
						filePath,
						Constants.ShellIconSizes.ExtraLarge,
						false,
						IconOptions.None);

					ViewModel.IconData = iconData;
				}
			});

			ViewModel.RemoveAlbumCoverCommand = new RelayCommand(async () =>
			{
				ViewModel.IsAblumCoverModified = true;
				ViewModel.ModifiedAlbumCover = null;

				string? mediaPath = np.Parameter switch
				{
					ListedItem singleItem => singleItem.ItemPath,
					List<ListedItem> items => items.FirstOrDefault()?.ItemPath,
					_ => null
				};

				if (!string.IsNullOrEmpty(mediaPath))
				{
					// ReturnIconOnly skips the file's embedded thumbnail, previewing the generic icon.
					var iconData = await FileThumbnailHelper.GetIconAsync(
						mediaPath,
						Constants.ShellIconSizes.ExtraLarge,
						false,
						IconOptions.ReturnIconOnly);

					ViewModel.IconData = iconData;
				}
			});

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (BaseProperties is not null &&
				BaseProperties.TokenSource is not null)
			{
				//BaseProperties.TokenSource.Cancel();
			}

			base.OnNavigatedFrom(e);
		}

		/// <summary>
		/// Try to save changed properties to the file.
		/// </summary>
		/// <returns>Returns true if properties have been saved successfully</returns>
		public abstract Task<bool> SaveChangesAsync();

		/// <summary>
		/// Dispose unmanaged resources.
		/// </summary>
		public abstract void Dispose();
	}
}
