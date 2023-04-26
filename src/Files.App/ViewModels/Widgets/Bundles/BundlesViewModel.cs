// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.EventArguments.Bundles;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ViewModels.Widgets.Bundles
{
	/// <summary>
	/// Bundles list View Model
	/// </summary>
	public class BundlesViewModel : ObservableObject, IDisposable
	{
		#region Private Members

		private bool itemAddedInternally;

		private int internalCollectionCount;

		#endregion Private Members

		public event EventHandler<BundlesOpenPathEventArgs> OpenPathEvent;

		public event EventHandler<string> OpenPathInNewPaneEvent;

		#region Properties

		private IBundlesSettingsService BundlesSettingsService { get; } = Ioc.Default.GetService<IBundlesSettingsService>();

		/// <summary>
		/// Collection of all bundles
		/// </summary>
		public ObservableCollection<BundleContainerViewModel> Items { get; private set; } = new ObservableCollection<BundleContainerViewModel>();

		private string bundleNameTextInput = string.Empty;

		public string BundleNameTextInput
		{
			get => bundleNameTextInput;
			set => SetProperty(ref bundleNameTextInput, value);
		}

		private string addBundleErrorText = string.Empty;

		public string AddBundleErrorText
		{
			get => addBundleErrorText;
			set => SetProperty(ref addBundleErrorText, value);
		}

		public bool noBundlesAddItemLoad = false;

		public bool NoBundlesAddItemLoad
		{
			get => noBundlesAddItemLoad;
			set => SetProperty(ref noBundlesAddItemLoad, value);
		}

		#endregion Properties

		#region Commands

		public ICommand InputTextKeyDownCommand { get; private set; }

		public ICommand OpenAddBundleDialogCommand { get; private set; }

		public ICommand AddBundleCommand { get; private set; }

		public ICommand ImportBundlesCommand { get; private set; }

		public ICommand ExportBundlesCommand { get; private set; }

		#endregion Commands

		#region Constructor

		public BundlesViewModel()
		{
			Items.CollectionChanged += Items_CollectionChanged;

			// Create commands
			InputTextKeyDownCommand = new RelayCommand<KeyRoutedEventArgs>(InputTextKeyDown);
			OpenAddBundleDialogCommand = new AsyncRelayCommand(OpenAddBundleDialog);
			AddBundleCommand = new RelayCommand(() => AddBundle(BundleNameTextInput));
			ImportBundlesCommand = new AsyncRelayCommand(ImportBundles);
			ExportBundlesCommand = new AsyncRelayCommand(ExportBundles);

			BundlesSettingsService.OnSettingImportedEvent += BundlesSettingsService_OnSettingImportedEvent;
		}

		#endregion Constructor

		#region Command Implementation

		private void InputTextKeyDown(KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				AddBundle(BundleNameTextInput);
				e.Handled = true;
			}
		}

		private async Task OpenAddBundleDialog()
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
				TitleText = "CreateBundle".GetLocalizedResource(),
				SubtitleText = "BundlesWidgetCreateBundleDialogSubtitleText".GetLocalizedResource(),
				PrimaryButtonText = "Confirm".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				PrimaryButtonAction = (vm, e) =>
				{
					var (result, reason) = CanAddBundle(inputText.Text);

					tipText.Text = reason;
					tipText.Visibility = result ? Visibility.Collapsed : Visibility.Visible;

					if (!result)
					{
						e.Cancel = true;
						return;
					}

					AddBundle(inputText.Text);
				},
				CloseButtonAction = (vm, e) =>
				{
					vm.HideDialog();
				},
				KeyDownAction = (vm, e) =>
				{
					if (e.Key == VirtualKey.Enter)
					{
						AddBundle(inputText.Text);
					}
					else if (e.Key == VirtualKey.Escape)
					{
						vm.HideDialog();
					}
				},
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});
			await dialog.TryShowAsync();
		}

		private void AddBundle(string name)
		{
			if (!CanAddBundle(name).result)
			{
				return;
			}

			string savedBundleNameTextInput = name;
			BundleNameTextInput = string.Empty;

			if (BundlesSettingsService.SavedBundles is null || (BundlesSettingsService.SavedBundles?.ContainsKey(savedBundleNameTextInput) ?? false)) // Init
			{
				BundlesSettingsService.SavedBundles = new Dictionary<string, List<string>>()
				{
					{ savedBundleNameTextInput, new List<string>() { null } }
				};
			}

			itemAddedInternally = true;
			Items.Add(new BundleContainerViewModel()
			{
				BundleName = savedBundleNameTextInput,
				NotifyItemRemoved = NotifyItemRemovedHandle,
				NotifyBundleItemRemoved = NotifyBundleItemRemovedHandle,
				OpenPath = OpenPathHandle,
				OpenPathInNewPane = OpenPathInNewPaneHandle,
			});

			NoBundlesAddItemLoad = false;
			itemAddedInternally = false;

			// Save bundles
			Save();
		}

		private async Task ImportBundles()
		{
			FileOpenPicker filePicker = InitializeWithWindow(new FileOpenPicker());
			filePicker.FileTypeFilter.Add(System.IO.Path.GetExtension(Constants.LocalSettings.BundlesSettingsFileName));

			StorageFile file = await filePicker.PickSingleFileAsync();
			if (file is not null)
			{
				try
				{
					string data = NativeFileOperationsHelper.ReadStringFromFile(file.Path);
					BundlesSettingsService.ImportSettings(data);
				}
				catch
				{
					// Couldn't deserialize, data is corrupted
				}
			}
		}
		private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private async Task ExportBundles()
		{
			FileSavePicker filePicker = InitializeWithWindow(new FileSavePicker());
			filePicker.FileTypeChoices.Add("Json File", new List<string>() { System.IO.Path.GetExtension(Constants.LocalSettings.BundlesSettingsFileName) });

			StorageFile file = await filePicker.PickSaveFileAsync();
			if (file is not null)
			{
				NativeFileOperationsHelper.WriteStringToFile(file.Path, (string)BundlesSettingsService.ExportSettings());
			}
		}

		private FileSavePicker InitializeWithWindow(FileSavePicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		#endregion Command Implementation

		#region Handlers

		private async void BundlesSettingsService_OnSettingImportedEvent(object sender, EventArgs e)
		{
			await Load();
		}

		private void OpenPathHandle(string path, FilesystemItemType itemType, bool openSilent, bool openViaApplicationPicker, IEnumerable<string> selectItems)
		{
			OpenPathEvent?.Invoke(this, new BundlesOpenPathEventArgs(path, itemType, openSilent, openViaApplicationPicker, selectItems));
		}

		private void OpenPathInNewPaneHandle(string path)
		{
			OpenPathInNewPaneEvent?.Invoke(this, path);
		}

		/// <summary>
		/// This function gets called when an item is removed to update the collection
		/// </summary>
		/// <param name="item"></param>
		private void NotifyItemRemovedHandle(BundleContainerViewModel item)
		{
			Items.Remove(item);
			item?.Dispose();

			if (Items.Count == 0)
			{
				NoBundlesAddItemLoad = true;
			}
		}

		/// <summary>
		/// This function gets called when an item is removed to update the collection
		/// </summary>
		/// <param name="bundleContainer"></param>
		/// <param name="bundleItemPath"></param>
		private void NotifyBundleItemRemovedHandle(string bundleContainer, string bundleItemPath)
		{
			BundleItemViewModel itemToRemove = Items.Where((item) => item.BundleName == bundleContainer).First().Contents.Where((item) => item.Path == bundleItemPath).First();
			itemToRemove.RemoveItem();
		}

		/// <summary>
		/// This function gets called when an item is renamed to update the collection
		/// </summary>
		/// <param name="item"></param>
		private void NotifyBundleItemRemovedHandle(BundleItemViewModel item)
		{
			foreach (var bundle in Items)
			{
				if (bundle.BundleName == item.ParentBundleName)
				{
					bundle.Contents.Remove(item);
					item?.Dispose();

					if (bundle.Contents.Count == 0)
					{
						bundle.NoBundleContentsTextLoad = true;
					}
				}
			}
		}

		private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (internalCollectionCount < Items.Count && !itemAddedInternally)
			{
				Save();
			}

			internalCollectionCount = Items.Count;
		}

		#endregion Handlers

		#region Public Helpers

		public void Save()
		{
			if (BundlesSettingsService.SavedBundles is not null)
			{
				Dictionary<string, List<string>> bundles = new Dictionary<string, List<string>>();

				// For every bundle in items bundle collection:
				foreach (var bundle in Items)
				{
					List<string> bundleItems = new List<string>();

					// For every bundleItem in current bundle
					foreach (var bundleItem in bundle.Contents)
					{
						if (bundleItem is not null)
						{
							bundleItems.Add(bundleItem.Path);
						}
					}

					bundles.Add(bundle.BundleName, bundleItems);
				}

				// Calls Set()
				BundlesSettingsService.SavedBundles = bundles;
			}
		}

		public async Task Load()
		{
			// Null, therefore no items :)
			if (BundlesSettingsService.SavedBundles is null)
			{
				NoBundlesAddItemLoad = true;
				return;
			}

			Items.Clear();

			// For every bundle in saved bundle collection:
			foreach (var bundle in BundlesSettingsService.SavedBundles)
			{
				var bundleItems = new List<BundleItemViewModel>();

				// For every bundleItem in current bundle
				foreach (var bundleItem in bundle.Value)
				{
					if (bundleItems.Count >= Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle || bundleItem is null)
						continue;

					bundleItems.Add(new BundleItemViewModel(bundleItem, await StorageHelpers.GetTypeFromPath(bundleItem))
					{
						ParentBundleName = bundle.Key,
						NotifyItemRemoved = NotifyBundleItemRemovedHandle,
						OpenPath = OpenPathHandle,
						OpenPathInNewPane = OpenPathInNewPaneHandle,
					});
				}

				// Fill current bundle with collected bundle items
				itemAddedInternally = true;
				Items.Add(await new BundleContainerViewModel()
				{
					BundleName = bundle.Key,
					NotifyItemRemoved = NotifyItemRemovedHandle,
					NotifyBundleItemRemoved = NotifyBundleItemRemovedHandle,
					OpenPath = OpenPathHandle,
					OpenPathInNewPane = OpenPathInNewPaneHandle,
				}
				.SetBundleItems(bundleItems));

				itemAddedInternally = false;
			}

			NoBundlesAddItemLoad = Items.Count == 0;
		}

		public Task Initialize()
			=> Load();

		public (bool result, string reason) CanAddBundle(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				AddBundleErrorText = "ErrorInputEmpty".GetLocalizedResource();
				return (false, "ErrorInputEmpty".GetLocalizedResource());
			}

			if (!Items.Any((item) => item.BundleName == name))
			{
				AddBundleErrorText = string.Empty;
				return (true, string.Empty);
			}
			else
			{
				AddBundleErrorText = "BundlesWidgetAddBundleErrorAlreadyExists".GetLocalizedResource();
				return (false, "BundlesWidgetAddBundleErrorAlreadyExists".GetLocalizedResource());
			}
		}

		#endregion Public Helpers

		#region IDisposable

		public void Dispose()
		{
			foreach (var item in Items)
			{
				item?.Dispose();
			}

			Items.CollectionChanged -= Items_CollectionChanged;
			BundlesSettingsService.OnSettingImportedEvent -= BundlesSettingsService_OnSettingImportedEvent;
		}

		#endregion IDisposable
	}
}
