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
using Files.Commands;
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

		private string _BundleNameTextInput = string.Empty;
		public string BundleNameTextInput
		{
			get => _BundleNameTextInput;
			set => SetProperty(ref _BundleNameTextInput, value);
		}

		private string _AddBundleErrorText = string.Empty;
		public string AddBundleErrorText
		{
			get => _AddBundleErrorText;
			set => SetProperty(ref _AddBundleErrorText, value);
		}

		public Visibility NoBundlesTextVisibility
		{
			get => Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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
			InputTextKeyDownCommand = new RelayParameterizedCommand((e) => InputTextKeyDown(e as KeyRoutedEventArgs));
			AddBundleCommand = new RelayCommand(AddBundle);
		}

		#endregion

		#region Command Implementation

		private void InputTextKeyDown(KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				AddBundle();
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
				BundleRenameText = savedBundleNameTextInput
			});

			// Save bundles
			Save();
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
			Items.Clear();

			if (JsonSettings.SavedBundles != null)
			{
				// For every bundle in saved bundle collection:
				foreach (var bundle in JsonSettings.SavedBundles)
				{
					List<BundleItemViewModel> bundleItems = new List<BundleItemViewModel>();

					// For every bundleItem in current bundle
					foreach (var bundleItem in bundle.Value)
					{
						if (bundleItem != null)
						{
							bundleItems.Add(new BundleItemViewModel(associatedInstance, bundleItem, await StorageItemHelpers.GetTypeFromPath(bundleItem, associatedInstance))
							{
								OriginBundleName = bundle.Key,
							});
						}
					}

					// Fill current bundle with collected bundle items
					Items.Add(new BundleContainerViewModel(associatedInstance)
					{
						BundleName = bundle.Key,
						BundleRenameText = bundle.Key
					}.SetBundleItems(bundleItems));
				}
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
				item?.Dispose();
			}

			Items = null;
		}

		#endregion
	}
}
