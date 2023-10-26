// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public abstract class BasePropertiesPage : Page, IDisposable
	{
		public IShellPage AppInstance = null;

		public BaseProperties BaseProperties { get; set; }

		public SelectedItemsPropertiesViewModel ViewModel { get; set; } = new();

		protected virtual void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			BaseProperties?.GetSpecialPropertiesAsync();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			AppInstance = np.AppInstance;

			// Library
			if (np.Parameter is LibraryItem library)
				BaseProperties = new LibraryProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, library, AppInstance);
			// Drive
			else if (np.Parameter is DriveItem drive)
			{
				var props = new DriveProperties(ViewModel, drive, AppInstance);
				BaseProperties = props;

				ViewModel.FormatVisibility = !(props.Drive.Type == DriveType.Network || string.Equals(props.Drive.Path, "C:\\", StringComparison.OrdinalIgnoreCase));
				ViewModel.CleanupDriveCommand = new AsyncRelayCommand(() => StorageSenseHelper.OpenStorageSenseAsync(props.Drive.Path));
				ViewModel.FormatDriveCommand = new RelayCommand(async () =>
				{
					try
					{
						await Win32API.OpenFormatDriveDialog(props.Drive.Path);
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
					BaseProperties = new CombinedFileProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, items, AppInstance);
				// Selection includes folders
				else
					BaseProperties = new CombinedProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, items, AppInstance);
			}
			// A storage object
			else if (np.Parameter is ListedItem item)
			{
				// File or Archive
				if (item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
					BaseProperties = new FileProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, item, AppInstance);
				// Folder
				else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
					BaseProperties = new FolderProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, item, AppInstance);
			}

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
