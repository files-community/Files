using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public abstract class PropertyTab : Page, IDisposable
	{
		public IShellPage AppInstance = null;

		public BaseViewModel BaseProperties { get; set; }

		public SelectedItemsPropertiesViewModel ViewModel { get; set; }

		protected virtual void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			if (BaseProperties is not null)
			{
				BaseProperties.GetSpecialProperties();
			}
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = e.Parameter as Views.MainPropertyPage.PropertyNavParam;

			AppInstance = np.AppInstanceArgument;
			ViewModel = new SelectedItemsPropertiesViewModel();

			if (np.navParameter is LibraryItem library)
			{
				BaseProperties = new LibraryViewModel(ViewModel, np.tokenSource, DispatcherQueue, library, AppInstance);
			}
			else if (np.navParameter is ListedItem item)
			{
				if (item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
				{
					BaseProperties = new FilePropertiesViewModel(ViewModel, np.tokenSource, DispatcherQueue, item, AppInstance);
				}
				else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
				{
					BaseProperties = new FolderPropertiesViewModel(ViewModel, np.tokenSource, DispatcherQueue, item, AppInstance);
				}
			}
			else if (np.navParameter is List<ListedItem> items)
			{
				BaseProperties = new CombinedViewModel(ViewModel, np.tokenSource, DispatcherQueue, items, AppInstance);
			}
			else if (np.navParameter is DriveItem drive)
			{
				BaseProperties = new DrivePropertyViewModel(ViewModel, drive, AppInstance);
			}

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (BaseProperties is not null && BaseProperties.TokenSource is not null)
			{
				//BaseProperties.TokenSource.Cancel();
			}

			base.OnNavigatedFrom(e);
		}

		/// <summary>
		/// Tries to save changed properties to file.
		/// </summary>
		/// <returns>Returns true if properties have been saved successfully.</returns>
		public abstract Task<bool> SaveChangesAsync();

		public abstract void Dispose();
	}
}
