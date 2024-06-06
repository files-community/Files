// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents a control that displays a list of recent folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class RecentFilesWidget : UserControl
	{
		public RecentFilesWidgetViewModel ViewModel { get; set; } = Ioc.Default.GetRequiredService<RecentFilesWidgetViewModel>();

		public RecentFilesWidget()
		{
			InitializeComponent();
		}

		private void RecentFilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is not RecentItem item)
				return;

			ViewModel.NavigateToPath(item.RecentPath);
		}

		private void RecentFilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildItemContextMenu(e.OriginalSource, e);
		}

		private async void RecentFilesListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			var items = e.Items.OfType<RecentItem>().ToList();
			if (items.Count > 0)
			{
				var storageItems = new List<IStorageItem>();
				var fileContents = new List<string>();

				foreach (var item in items)
				{
					try
					{
						// Attempt to get the file from its path
						var file = await StorageFile.GetFileFromPathAsync(item.RecentPath);
						if (file != null)
							storageItems.Add(file);

						// Read the file text content
						var fileContent = await FileIO.ReadTextAsync(file);
						fileContents.Add(fileContent);

					}
					catch (Exception)
					{
						// Handle the case where the file might not be accessible or does not exist
						e.Cancel = true;
					}
				}

				if (storageItems.Count > 0)
				{
					// Create a new data package and set the storage items
					DataPackage dataPackage = new DataPackage();
					dataPackage.SetStorageItems(storageItems);
					e.Data.SetDataProvider(StandardDataFormats.StorageItems, request => request.SetData(storageItems));

					if (fileContents.Count > 0)
					{
						// Create a new data package and set the file contents as text
						dataPackage.SetText(string.Join("\n", fileContents));
						e.Data.SetDataProvider(StandardDataFormats.Text, request => request.SetData(string.Join("\n", fileContents)));

					}

					// Set the requested operation to Copy if dragging outside the application
					e.Data.RequestedOperation = DataPackageOperation.Copy;
				}
			}
		}
	}
}
