using Files.Dialogs;
using Files.Enums;
using Files.SettingsInterfaces;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Bundles
{
    /// <summary>
    /// Bundle's contents view model
    /// </summary>
    public class BundleContainerViewModel : ObservableObject, IDisposable
    {
        #region Singleton

        private IBundlesSettings BundlesSettings => App.BundlesSettings;

        #endregion Singleton

        #region Private Members

        private IShellPage associatedInstance;

        #endregion Private Members

        #region Actions

        public Action<BundleContainerViewModel> NotifyItemRemoved { get; set; }

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

        private Visibility noBundleContentsTextVisibility;

        public Visibility NoBundleContentsTextVisibility
        {
            get => noBundleContentsTextVisibility;
            set => SetProperty(ref noBundleContentsTextVisibility, value);
        }

        #endregion Public Properties

        #region Commands

        public ICommand RemoveBundleCommand { get; private set; }

        public ICommand RenameBundleCommand { get; private set; }

        public ICommand DragOverCommand { get; private set; }

        public ICommand DropCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public BundleContainerViewModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;

            // Create commands
            RemoveBundleCommand = new RelayCommand(RemoveBundle);
            RenameBundleCommand = new RelayCommand(RenameBundle);
            DragOverCommand = new RelayCommand<DragEventArgs>(DragOver);
            DropCommand = new RelayCommand<DragEventArgs>(Drop);
        }

        #endregion Constructor

        #region Command Implementation

        private void RemoveBundle()
        {
            if (BundlesSettings.SavedBundles.ContainsKey(BundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles; // We need to do it this way for Set() to be called
                allBundles.Remove(BundleName);
                BundlesSettings.SavedBundles = allBundles;
                NotifyItemRemoved(this);
            }
        }

        private async void RenameBundle()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                DisplayControl = new TextBox()
                {
                    PlaceholderText = "BundlesWidgetRenameBundleDialogInputPlaceholderText".GetLocalized()
                },
                TitleText = string.Format("BundlesWidgetRenameBundleDialogTitleText".GetLocalized(), BundleName),
                SubtitleText = "BundlesWidgetRenameBundleDialogSubtitleText".GetLocalized(),
                PrimaryButtonText = "BundlesWidgetRenameBundleDialogPrimaryButtonText".GetLocalized(),
                CloseButtonText = "BundlesWidgetRenameBundleDialogCloseButtonText".GetLocalized(),
                PrimaryButtonAction = (vm, e) =>
                {
                    RenameBundleConfirm((vm.DisplayControl as TextBox).Text);
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
                        RenameBundleConfirm((vm.DisplayControl as TextBox).Text);
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
        }

        private void RenameBundleConfirm(string bundleRenameText)
        {
            if (CanRenameBundle(bundleRenameText))
            {
                if (BundlesSettings.SavedBundles.ContainsKey(BundleName))
                {
                    Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles; // We need to do it this way for Set() to be called
                    Dictionary<string, List<string>> newBundles = new Dictionary<string, List<string>>();

                    foreach (var item in allBundles)
                    {
                        if (item.Key == BundleName) // Item matches to-rename name
                        {
                            newBundles.Add(bundleRenameText, item.Value);

                            // We need to remember to change BundleItemViewModel.OriginBundleName!
                            foreach (var bundleItem in Contents)
                            {
                                bundleItem.ParentBundleName = bundleRenameText;
                            }
                        }
                        else // Ignore, and add existing values
                        {
                            newBundles.Add(item.Key, item.Value);
                        }
                    }

                    BundlesSettings.SavedBundles = newBundles;
                    BundleName = bundleRenameText;
                }
            }
        }

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
                bool itemsAdded = false;
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

                foreach (IStorageItem item in items)
                {
                    if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
                    {
                        if (!System.IO.Path.GetExtension(item.Path).ToLower().EndsWith(".lnk") && !System.IO.Path.GetExtension(item.Path).ToLower().EndsWith(".url"))
                        {
                            if (!Contents.Any((i) => i.Path == item.Path)) // Don't add existing items!
                            {
                                AddBundleItem(new BundleItemViewModel(associatedInstance, item.Path, item.IsOfType(StorageItemTypes.Folder) ? Filesystem.FilesystemItemType.Directory : Filesystem.FilesystemItemType.File)
                                {
                                    ParentBundleName = BundleName,
                                    NotifyItemRemoved = NotifyItemRemovedHandle
                                });
                                itemsAdded = true;
                            }
                        }
                    }
                }
                e.Handled = true;

                if (itemsAdded)
                {
                    SaveBundle();
                }
            }
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
                NoBundleContentsTextVisibility = Visibility.Visible;
            }
        }

        #endregion Handlers

        #region Private Helpers

        private bool SaveBundle()
        {
            if (BundlesSettings.SavedBundles.ContainsKey(BundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles; // We need to do it this way for Set() to be called
                allBundles[BundleName] = Contents.Select((item) => item.Path).ToList();
                BundlesSettings.SavedBundles = allBundles;

                return true;
            }

            return false;
        }

        #endregion Private Helpers

        #region Public Helpers

        public BundleContainerViewModel AddBundleItem(BundleItemViewModel bundleItem)
        {
            if (bundleItem != null)
            {
                Contents.Add(bundleItem);
                NoBundleContentsTextVisibility = Visibility.Collapsed;
            }

            return this;
        }

        public BundleContainerViewModel SetBundleItems(List<BundleItemViewModel> items)
        {
            Contents = new ObservableCollection<BundleItemViewModel>(items);

            if (Contents.Count > 0)
            {
                NoBundleContentsTextVisibility = Visibility.Collapsed;
            }

            return this;
        }

        public bool CanRenameBundle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (!BundlesSettings.SavedBundles.Any((item) => item.Key == name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion Public Helpers

        #region IDisposable

        public void Dispose()
        {
            foreach (var item in Contents)
            {
                item.NotifyItemRemoved -= NotifyItemRemovedHandle;
                item?.Dispose();
            }

            BundleName = null;

            RemoveBundleCommand = null;
            RenameBundleCommand = null;
            DragOverCommand = null;
            DropCommand = null;

            associatedInstance = null;
            Contents = null;
        }

        #endregion IDisposable
    }
}