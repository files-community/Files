using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Helpers;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ViewModels.Widgets.Bundles
{
	/// <summary>
	/// Bundle's contents view model
	/// </summary>
	public class BundleContainerViewModel : ObservableObject, IDisposable
	{
		#region Singleton

		private IBundlesSettingsService BundlesSettingsService { get; } = Ioc.Default.GetService<IBundlesSettingsService>();

		#endregion Singleton

		#region Private Members

		private bool itemAddedInternally;

		private int internalCollectionCount;

		#endregion Private Members

		#region Actions

		public Action<BundleContainerViewModel> NotifyItemRemoved { get; set; }

		public Action<string, string> NotifyBundleItemRemoved { get; set; }

		public Action<string, FilesystemItemType, bool, bool, IEnumerable<string>> OpenPath { get; set; }

		public Action<string> OpenPathInNewPane { get; set; }

		#endregion Actions

		#region Public Properties

		/// <summary>
		/// A list of Bundle's contents
		/// </summary>
		public ObservableCollection<BundleItemViewModel> Contents { get; private set; } = new ObservableCollection<BundleItemViewModel>();

		private string bundleName = "DefaultBundle";
		public string BundleName
		{
			get => bundleName;
			set => SetProperty(ref bundleName, value);
		}

		private bool noBundleContentsTextLoad;
		public bool NoBundleContentsTextLoad
		{
			get => noBundleContentsTextLoad;
			set => SetProperty(ref noBundleContentsTextLoad, value);
		}

		private bool isAddItemOptionEnabled = true;
		public bool IsAddItemOptionEnabled
		{
			get => isAddItemOptionEnabled;
			set => SetProperty(ref isAddItemOptionEnabled, value);
		}

		#endregion Public Properties

		#region Commands

		public ICommand OpenItemCommand { get; private set; }

		public ICommand RemoveBundleCommand { get; private set; }

		public ICommand RenameBundleCommand { get; private set; }

		public ICommand DragOverCommand { get; private set; }

		public ICommand DropCommand { get; private set; }

		public ICommand DragItemsStartingCommand { get; private set; }

		public ICommand AddFileCommand { get; private set; }

		public ICommand AddFolderCommand { get; private set; }

		#endregion Commands

		#region Constructor

		public BundleContainerViewModel()
		{
			internalCollectionCount = Contents.Count;
			Contents.CollectionChanged += Contents_CollectionChanged;

			RemoveBundleCommand = new RelayCommand(RemoveBundle);
			RenameBundleCommand = new AsyncRelayCommand(RenameBundle);
			DragOverCommand = new RelayCommand<DragEventArgs>(DragOver);
			DropCommand = new AsyncRelayCommand<DragEventArgs>(Drop);
			DragItemsStartingCommand = new RelayCommand<DragItemsStartingEventArgs>(DragItemsStarting);
			OpenItemCommand = new RelayCommand<ItemClickEventArgs>((e) =>
			{
				(e.ClickedItem as BundleItemViewModel).OpenItem();
			});
			AddFileCommand = new AsyncRelayCommand(AddFile);
			AddFolderCommand = new AsyncRelayCommand(AddFolder);
		}

		#endregion Constructor

		#region Command Implementation

		private async Task AddFolder()
		{
			FolderPicker folderPicker = InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");

			StorageFolder folder = await folderPicker.PickSingleFolderAsync();

			if (folder is not null)
			{
				await AddItemFromPath(folder.Path, FilesystemItemType.Directory);
				SaveBundle();
			}
		}

		// WINUI3
		private FolderPicker InitializeWithWindow(FolderPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private async Task AddFile()
		{
			FileOpenPicker filePicker = InitializeWithWindow(new FileOpenPicker());
			filePicker.FileTypeFilter.Add("*");

			StorageFile file = await filePicker.PickSingleFileAsync();

			if (file is not null)
			{
				await AddItemFromPath(file.Path, FilesystemItemType.File);
				SaveBundle();
			}
		}

		// WINUI3
		private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private void RemoveBundle()
		{
			if (BundlesSettingsService.SavedBundles.ContainsKey(BundleName))
			{
				Dictionary<string, List<string>> allBundles = BundlesSettingsService.SavedBundles;
				allBundles.Remove(BundleName);
				BundlesSettingsService.SavedBundles = allBundles;
				NotifyItemRemoved(this);
			}
		}

		private async Task RenameBundle()
		{
			TextBox inputText = new TextBox()
			{
				PlaceholderText = "DesiredName".GetLocalizedResource()
			};

			TextBlock tipText = new TextBlock()
			{
				Text = string.Empty,
				Visibility = Visibility.Collapsed
			};

			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				DisplayControl = new Grid()
				{
					Children =
					{
						new StackPanel()
						{
							Spacing = 4d,
							Children =
							{
								inputText,
								tipText
							}
						}
					}
				},
				TitleText = string.Format("BundlesWidgetRenameBundleDialogTitleText".GetLocalizedResource(), BundleName),
				SubtitleText = "BundlesWidgetRenameBundleDialogSubtitleText".GetLocalizedResource(),
				PrimaryButtonText = "Confirm".GetLocalizedResource(),
				CloseButtonText = "BundlesWidgetRenameBundleDialogCloseButtonText".GetLocalizedResource(),
				PrimaryButtonAction = (vm, e) =>
				{
					if (!CanAddBundleSetErrorMessage())
					{
						e.Cancel = true;
						return;
					}

					RenameBundleConfirm(inputText.Text);
				},
				CloseButtonAction = (vm, e) =>
				{
					// Cancel the rename
					vm.HideDialog();
				},
				KeyDownAction = (vm, e) =>
				{
					if (e.Key == VirtualKey.Enter)
					{
						if (!CanAddBundleSetErrorMessage())
						{
							return;
						}

						RenameBundleConfirm(inputText.Text);
					}
					else if (e.Key == VirtualKey.Escape)
					{
						// Cancel the rename
						vm.HideDialog();
					}
				},
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			await dialog.ShowAsync();

			bool CanAddBundleSetErrorMessage()
			{
				var (result, reason) = CanRenameBundle(inputText.Text);

				tipText.Text = reason;
				tipText.Visibility = result ? Visibility.Collapsed : Visibility.Visible;

				return result;
			}
		}

		private void RenameBundleConfirm(string bundleRenameText)
		{
			if (CanRenameBundle(bundleRenameText).result)
			{
				if (BundlesSettingsService.SavedBundles.ContainsKey(BundleName))
				{
					// We need to do it this way for Set() to be called
					Dictionary<string, List<string>> allBundles = BundlesSettingsService.SavedBundles;

					Dictionary<string, List<string>> newBundles = new Dictionary<string, List<string>>();

					foreach (var item in allBundles)
					{
						// Item matches to-rename name
						if (item.Key == BundleName)
						{
							newBundles.Add(bundleRenameText, item.Value);

							// We need to remember to change BundleItemViewModel.OriginBundleName!
							foreach (var bundleItem in Contents)
							{
								bundleItem.ParentBundleName = bundleRenameText;
							}
						}
						// Ignore, and add existing values
						else
						{
							newBundles.Add(item.Key, item.Value);
						}
					}

					BundlesSettingsService.SavedBundles = newBundles;
					BundleName = bundleRenameText;
				}
			}
		}

		private void DragOver(DragEventArgs e)
		{
			if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView) || e.DataView.Contains(StandardDataFormats.Text))
			{
				// Don't exceed the limit!
				if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
				{
					e.AcceptedOperation = DataPackageOperation.Move;
				}
			}
			else
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}

			e.Handled = true;
		}

		private async Task Drop(DragEventArgs e)
		{
			var deferral = e?.GetDeferral();
			try
			{
				bool itemsAdded = false;

				if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				{
					var items = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

					if (await AddItemsFromPath(items.ToDictionary((item) => item.Path, (item) => item.ItemType)))
					{
						itemsAdded = true;
					}
				}
				else if (e.DataView.Contains(StandardDataFormats.Text))
				{
					string itemText = await e.DataView.GetTextAsync();

					if (string.IsNullOrWhiteSpace(itemText))
					{
						return;
					}

					bool dragFromBundle = false;
					string itemPath = null;
					string originBundle = null;

					if (itemText.Contains('|', StringComparison.Ordinal))
					{
						dragFromBundle = true;

						originBundle = itemText.Split('|')[0];
						itemPath = itemText.Split('|')[1];
					}
					else
					{
						dragFromBundle = false;
						itemPath = itemText;
					}

					IStorageItem item = await StorageHelpers.ToStorageItem<IStorageItem>(itemPath);

					if (item is not null || FileExtensionHelpers.IsShortcutOrUrlFile(itemPath))
					{
						if (await AddItemFromPath(itemPath,
							FileExtensionHelpers.IsShortcutOrUrlFile(itemPath) ? FilesystemItemType.File : (item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File)))
						{
							itemsAdded = true;
						}
					}

					if (itemsAdded && dragFromBundle)
					{
						// Also remove the item from the collection
						if (BundlesSettingsService.SavedBundles.ContainsKey(BundleName))
						{
							Dictionary<string, List<string>> allBundles = BundlesSettingsService.SavedBundles;
							allBundles[originBundle].Remove(itemPath);
							BundlesSettingsService.SavedBundles = allBundles;

							NotifyBundleItemRemoved(originBundle, itemPath);
						}
					}
				}

				if (itemsAdded)
				{
					SaveBundle();
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				deferral?.Complete();
			}
		}

		private void DragItemsStarting(DragItemsStartingEventArgs e)
		{
			string itemPathAndData = $"{BundleName}|{(e.Items.First() as BundleItemViewModel).Path}";
			e.Data.SetData(StandardDataFormats.Text, itemPathAndData);
		}

		#endregion Command Implementation

		#region Handlers

		/// <summary>
		/// This function gets called when an item is removed to update the collection
		/// </summary>
		/// <param name="item"></param>
		private void NotifyItemRemovedHandle(BundleItemViewModel item)
		{
			Contents.Remove(item);
			item?.Dispose();

			if (Contents.Count == 0)
			{
				NoBundleContentsTextLoad = true;
			}
		}

		private void Contents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (internalCollectionCount < Contents.Count && !itemAddedInternally)
			{
				SaveBundle();
			}

			internalCollectionCount = Contents.Count;

			UpdateAddItemOption();
		}

		#endregion Handlers

		#region Private Helpers

		private bool SaveBundle()
		{
			if (BundlesSettingsService.SavedBundles.ContainsKey(BundleName))
			{
				Dictionary<string, List<string>> allBundles = BundlesSettingsService.SavedBundles;
				allBundles[BundleName] = Contents.Select((item) => item.Path).ToList();

				BundlesSettingsService.SavedBundles = allBundles;

				return true;
			}

			return false;
		}

		private Task<bool> AddItemFromPath(string path, FilesystemItemType itemType)
		{
			// Make sure we don't exceed maximum amount && make sure we don't make duplicates
			if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle && !Contents.Any((item) => item.Path == path))
			{
				return AddBundleItem(new BundleItemViewModel(path, itemType)
				{
					ParentBundleName = BundleName,
					NotifyItemRemoved = NotifyItemRemovedHandle,
					OpenPath = OpenPath,
					OpenPathInNewPane = OpenPathInNewPane,
				});
			}

			return Task.FromResult(false);
		}

		private Task<bool> AddItemsFromPath(IDictionary<string, FilesystemItemType> paths)
		{
			return AddBundleItems(paths.Select((item) => new BundleItemViewModel(item.Key, item.Value)
			{
				ParentBundleName = BundleName,
				NotifyItemRemoved = NotifyItemRemovedHandle,
				OpenPath = OpenPath,
				OpenPathInNewPane = OpenPathInNewPane
			}));
		}

		private void UpdateAddItemOption()
		{
			IsAddItemOptionEnabled = Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle;
		}

		#endregion Private Helpers

		#region Public Helpers

		public async Task<bool> AddBundleItem(BundleItemViewModel bundleItem)
		{
			// Make sure we don't exceed maximum amount && make sure we don't make duplicates
			if (bundleItem is not null && Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle && !Contents.Any((item) => item.Path == bundleItem.Path))
			{
				itemAddedInternally = true;
				Contents.Add(bundleItem);
				itemAddedInternally = false;
				NoBundleContentsTextLoad = false;
				IsAddItemOptionEnabled = Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle;
				await bundleItem.UpdateIcon();
				return true;
			}

			return false;
		}

		public async Task<bool> AddBundleItems(IEnumerable<BundleItemViewModel> bundleItems)
		{
			List<Task<bool>> taskDelegates = new List<Task<bool>>();

			foreach (var item in bundleItems)
			{
				taskDelegates.Add(AddBundleItem(item));
			}

			IEnumerable<bool> result = await Task.WhenAll(taskDelegates);

			return result.Any((item) => item);
		}

		public async Task<BundleContainerViewModel> SetBundleItems(IEnumerable<BundleItemViewModel> items)
		{
			Contents.Clear();

			await AddBundleItems(items);

			return this;
		}

		public (bool result, string reason) CanRenameBundle(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return (false, "ErrorInputEmpty".GetLocalizedResource());
			}

			if (!BundlesSettingsService.SavedBundles.Any((item) => item.Key == name))
			{
				return (true, string.Empty);
			}
			else
			{
				return (false, "BundlesWidgetAddBundleErrorAlreadyExists".GetLocalizedResource());
			}
		}

		#endregion Public Helpers

		#region IDisposable

		public void Dispose()
		{
			foreach (var item in Contents)
			{
				item?.Dispose();
			}

			NotifyBundleItemRemoved = null;
			NotifyItemRemoved = null;
			OpenPath = null;
			OpenPathInNewPane = null;

			Contents.CollectionChanged -= Contents_CollectionChanged;
		}

		#endregion IDisposable
	}
}
