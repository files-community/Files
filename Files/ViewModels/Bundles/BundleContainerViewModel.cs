using Files.Commands;
using Files.Settings;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.ViewModels.Bundles
{
	/// <summary>
	/// Bundle's contents view model
	/// </summary>
	public class BundleContainerViewModel : ObservableObject, IDisposable
	{
		#region Singleton

		private IJsonSettings JsonSettings => associatedInstance?.InstanceViewModel.JsonSettings;

		#endregion

		#region Private Members

		private readonly IShellPage associatedInstance;

		#endregion

		#region Public Properties

		/// <summary>
		/// A list of Bundle's contents
		/// </summary>
		public ObservableCollection<BundleItemViewModel> Contents { get; private set; } = new ObservableCollection<BundleItemViewModel>();

		public string BundleName { get; set; } = "Bundle1";

		#endregion

		#region Commands

		public ICommand DragOverCommand { get; set; }

		public ICommand DropCommand { get; set; }

		#endregion

		#region Constructor

		public BundleContainerViewModel(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;

			// Create commands
			DragOverCommand = new RelayParameterizedCommand((e) => DragOver(e as DragEventArgs));
			DropCommand = new RelayParameterizedCommand((e) => Drop(e as DragEventArgs));
		}

		#endregion

		#region Command Implementation

		private void DragOver(DragEventArgs e)
		{
			if (e.DataView.Contains(StandardDataFormats.StorageItems))
			{
				e.AcceptedOperation = DataPackageOperation.Move;
				e.Handled = true;
			}
		}

		private async void Drop(DragEventArgs e)
		{
			if (e.DataView.Contains(StandardDataFormats.StorageItems))
			{
				bool itemAdded = false;
				IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

				foreach (IStorageItem item in items)
				{
					if (items.Count < Constants.Widgets.Bundles.MaxAmountOfItemsInBundle)
					{
						if (!Contents.Any((i) => i.Path == item.Path)) // Don't add existing items!
						{
							AddBundleItem(new BundleItemViewModel(associatedInstance)
							{
								Path = item.Path,
								TargetType = item.IsOfType(StorageItemTypes.Folder) ? Filesystem.FilesystemItemType.Directory : Filesystem.FilesystemItemType.File
							});
							itemAdded = true;
						}
					}
				}
				e.Handled = true;

				if (itemAdded)
				{
					SaveBundle();
					// Log here?
				}
			}
		}

		#endregion

		#region Private Helpers

		private bool SaveBundle()
		{
			if (JsonSettings.SavedBundles.ContainsKey(BundleName))
			{
				Dictionary<string, List<string>> allBundles = JsonSettings.SavedBundles; // We need to do it this way for Set() to be called
				allBundles[BundleName] = Contents.Select((item) => item.Path).ToList();
				JsonSettings.SavedBundles = allBundles;

				return true;
			}

			return false;
		}

		#endregion

		#region Public Helpers

		public BundleContainerViewModel AddBundleItem(BundleItemViewModel bundleItem)
		{
			Contents.Add(bundleItem);

			return this;
		}

		public BundleContainerViewModel SetBundleItems(List<BundleItemViewModel> items)
		{
			Contents = new ObservableCollection<BundleItemViewModel>(items);

			return this;
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			foreach (var item in Contents)
			{
				item?.Dispose();
			}

			Contents = null;
		}

		#endregion
	}
}
