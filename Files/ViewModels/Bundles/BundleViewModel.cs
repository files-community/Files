using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Files.ViewModels.Bundles
{
    /// <summary>
    /// Bundles list View Model
    /// </summary>
    public class BundleViewModel : ObservableObject, IDisposable
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

        public Visibility NoBundlesTextVisibility
        {
            get => Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Commands

        public ICommand AddBundleCommand { get; set; }

        #endregion

        #region Constructor

        public BundleViewModel()
        {
            // Create commands
            AddBundleCommand = new RelayCommand(AddBundle);
        }

        #endregion

        #region Command Implementation

        private void AddBundle()
        {
            string bundleName = $"Bundle{Items.Count}";

            if (JsonSettings.SavedBundles == null || (JsonSettings.SavedBundles?.ContainsKey(bundleName) ?? false)) // Init
            {
                JsonSettings.SavedBundles = new Dictionary<string, List<string>>()
                {
                    { bundleName, new List<string>() { null } }
                };
            }

            Items.Add(new BundleContainerViewModel(associatedInstance)
            {
                BundleName = bundleName
            });
        }

        #endregion

        #region Public Helpers

        public async Task Load(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
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
                            bundleItems.Add(new BundleItemViewModel(associatedInstance)
                            {
                                Path = bundleItem, // As Path
                                TargetType = await StorageItemHelpers.GetTypeFromPath(bundleItem, associatedInstance)
                            });
                        }
                    }

                    // Fill current bundle with collected bundle items
                    Items.Add(new BundleContainerViewModel(associatedInstance)
                    {
                        BundleName = bundle.Key
                    }.SetBundleItems(bundleItems));
                }
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
