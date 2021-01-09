using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Files.Helpers;
using Files.SettingsInterfaces;
using Windows.UI.Xaml.Input;
using Windows.System;

namespace Files.ViewModels.Bundles
{
	/// <summary>
	/// Bundles list View Model
	/// </summary>
	public class BundlesViewModel : ObservableObject, IDisposable
	{
		#region Singleton

		private IJsonSettings JsonSettings => associatedInstance?.InstanceViewModel.JsonSettings;

		#endregion

		#region Private Members

		private IShellPage associatedInstance;

		#endregion

		#region Public Properties

		/// <summary>
		/// Collection of all bundles
		/// </summary>
		public ObservableCollection<BundleContainerViewModel> Items { get; set; } = new ObservableCollection<BundleContainerViewModel>();

		private string pBundleNameTextInput = string.Empty;
		public string BundleNameTextInput
		{
			get => pBundleNameTextInput;
			set => SetProperty(ref pBundleNameTextInput, value);
		}

		private string pAddBundleErrorText = string.Empty;
		public string AddBundleErrorText
		{
			get => pAddBundleErrorText;
			set => SetProperty(ref pAddBundleErrorText, value);
		}

		public Visibility pNoBundlesAddItemVisibility = Visibility.Collapsed;
		public Visibility NoBundlesAddItemVisibility
		{
			get => pNoBundlesAddItemVisibility;
			set => SetProperty(ref pNoBundlesAddItemVisibility, value);
		}

		#endregion

		#region Commands

		public ICommand InputTextKeyDownCommand { get; set; }

		public ICommand AddBundleCommand { get; set; }

		#endregion

		#region Constructor

		public BundlesViewModel()
		{
			// Create commands
			InputTextKeyDownCommand = new RelayCommand<KeyRoutedEventArgs>(InputTextKeyDown);
			AddBundleCommand = new RelayCommand(AddBundle);
		}

		#endregion

		#region Command Implementation

		private void InputTextKeyDown(KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				AddBundle();
				e.Handled = true;
			}
		}

		private void AddBundle()
		{
			if (!CanAddBundle(BundleNameTextInput))
			{
				return;
			}

			string savedBundleNameTextInput = BundleNameTextInput;
			BundleNameTextInput = string.Empty;

			if (JsonSettings.SavedBundles == null || (JsonSettings.SavedBundles?.ContainsKey(savedBundleNameTextInput) ?? false)) // Init
			{
				JsonSettings.SavedBundles = new Dictionary<string, List<string>>()
				{
					{ savedBundleNameTextInput, new List<string>() { null } }
				};
			}

			Items.Add(new BundleContainerViewModel(associatedInstance)
			{
				BundleName = savedBundleNameTextInput,
				BundleRenameText = savedBundleNameTextInput,
				NotifyItemRemoved = NotifyItemRemovedHandle,
				NotifyItemRenamed = NotifyItemRenamedHandle
			});
			NoBundlesAddItemVisibility = Visibility.Collapsed;

			// Save bundles
			Save();
		}

		#endregion

		#region Handlers

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
				NoBundlesAddItemVisibility = Visibility.Visible;
			}
		}

		/// <summary>
		/// This function gets called when an item is renamed to update the collection
		/// </summary>
		/// <param name="item"></param>
		private void NotifyItemRenamedHandle(BundleContainerViewModel item)
		{
			//Items[Items.IndexOf(item)].
		}

		/// <summary>
		/// This function gets called when an item is renamed to update the collection
		/// </summary>
		/// <param name="item"></param>
		private void NotifyBundleItemRemovedHandle(BundleItemViewModel item)
		{
			foreach (var bundle in Items)
			{
				if (bundle.BundleName == item.OriginBundleName)
				{
					bundle.Contents.Remove(item);
					item?.Dispose();

					if (bundle.Contents.Count == 0)
					{
						bundle.NoBundleContentsTextVisibility = Visibility.Visible;
					}
				}
			}
		}

		#endregion

		#region Public Helpers

		public void Save()
		{
			if (JsonSettings.SavedBundles != null)
			{
				Dictionary<string, List<string>> bundles = new Dictionary<string, List<string>>();

				// For every bundle in items bundle collection:
				foreach (var bundle in Items)
				{
					List<string> bundleItems = new List<string>();

					// For every bundleItem in current bundle
					foreach (var bundleItem in bundle.Contents)
					{
						if (bundleItem != null)
						{
							bundleItems.Add(bundleItem.Path);
						}
					}

					bundles.Add(bundle.BundleName, bundleItems);
				}

				JsonSettings.SavedBundles = bundles; // Calls Set()
			}
		}

		public async Task Load()
		{
			if (JsonSettings.SavedBundles != null)
			{
				Items.Clear();

				// For every bundle in saved bundle collection:
				foreach (var bundle in JsonSettings.SavedBundles)
				{
					List<BundleItemViewModel> bundleItems = new List<BundleItemViewModel>();

					// For every bundleItem in current bundle
					foreach (var bundleItem in bundle.Value)
					{
						if (bundleItems.Count <= Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
						{
							if (bundleItem != null)
							{
								bundleItems.Add(new BundleItemViewModel(associatedInstance, bundleItem, await StorageItemHelpers.GetTypeFromPath(bundleItem, associatedInstance))
								{
									OriginBundleName = bundle.Key,
									NotifyItemRemoved = NotifyBundleItemRemovedHandle
								});
							}
						}
					}

					// Fill current bundle with collected bundle items
					Items.Add(new BundleContainerViewModel(associatedInstance)
					{
						BundleName = bundle.Key,
						BundleRenameText = bundle.Key,
						NotifyItemRemoved = NotifyItemRemovedHandle,
						NotifyItemRenamed = NotifyItemRenamedHandle
					}.SetBundleItems(bundleItems));
				}

				if (Items.Count == 0)
				{
					NoBundlesAddItemVisibility = Visibility.Visible;
				}
				else
				{
					NoBundlesAddItemVisibility = Visibility.Collapsed;
				}
			}
			else // Null, therefore no items :)
			{
				NoBundlesAddItemVisibility = Visibility.Visible;
			}
		}

		public void Initialize(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;
		}

		public bool CanAddBundle(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				AddBundleErrorText = "Input field cannot be empty!";
				return false;
			}

			if (!Items.Any((item) => item.BundleName == name))
			{
				AddBundleErrorText = string.Empty;
				return true;
			}
			else
			{
				AddBundleErrorText = "Bundle with the same name already exists!";
				return false;
			}
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			foreach (var item in Items)
			{
				item.NotifyItemRemoved -= NotifyItemRemovedHandle;
				item.NotifyItemRenamed -= NotifyItemRenamedHandle;
				item?.Dispose();
			}

			associatedInstance = null;
			Items = null;
		}

		#endregion
	}
}
