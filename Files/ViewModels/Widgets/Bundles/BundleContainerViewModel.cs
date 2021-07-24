﻿using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.SettingsInterfaces;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Widgets.Bundles
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

        private bool isAddItemOptionEnabled;
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

            // Create commands
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
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                await AddItemFromPath(folder.Path, FilesystemItemType.Directory);
                SaveBundle();
            }
        }

        private async Task AddFile()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add("*");

            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                await AddItemFromPath(file.Path, FilesystemItemType.File);
                SaveBundle();
            }
        }

        private void RemoveBundle()
        {
            if (BundlesSettings.SavedBundles.ContainsKey(BundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles;
                allBundles.Remove(BundleName);
                BundlesSettings.SavedBundles = allBundles;
                NotifyItemRemoved(this);
            }
        }

        private async Task RenameBundle()
        {
            TextBox inputText = new TextBox()
            {
                PlaceholderText = "BundlesWidgetRenameBundleDialogInputPlaceholderText".GetLocalized()
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
                TitleText = string.Format("BundlesWidgetRenameBundleDialogTitleText".GetLocalized(), BundleName),
                SubtitleText = "BundlesWidgetRenameBundleDialogSubtitleText".GetLocalized(),
                PrimaryButtonText = "BundlesWidgetRenameBundleDialogPrimaryButtonText".GetLocalized(),
                CloseButtonText = "BundlesWidgetRenameBundleDialogCloseButtonText".GetLocalized(),
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
            if (e.DataView.Contains(StandardDataFormats.StorageItems) || e.DataView.Contains(StandardDataFormats.Text))
            {
                if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle) // Don't exceed the limit!
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

                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

                    if (await AddItemsFromPath(items.ToDictionary((item) => item.Path, (item) => item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File)))
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

                    if (itemText.Contains("|"))
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

                    IStorageItem item = await StorageItemHelpers.ToStorageItem<IStorageItem>(itemPath);

                    if (item != null || (itemPath.EndsWith(".lnk") || itemPath.EndsWith(".url")))
                    {
                        if (await AddItemFromPath(itemPath,
                            itemPath.EndsWith(".lnk") || itemPath.EndsWith(".url") ? FilesystemItemType.File : (item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File)))
                        {
                            itemsAdded = true;
                        }
                    }

                    if (itemsAdded && dragFromBundle)
                    {
                        // Also remove the item from the collection
                        if (BundlesSettings.SavedBundles.ContainsKey(BundleName))
                        {
                            Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles;
                            allBundles[originBundle].Remove(itemPath);
                            BundlesSettings.SavedBundles = allBundles;

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
            if (BundlesSettings.SavedBundles.ContainsKey(BundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles;
                allBundles[BundleName] = Contents.Select((item) => item.Path).ToList();

                BundlesSettings.SavedBundles = allBundles;

                return true;
            }

            return false;
        }

        private async Task<bool> AddItemFromPath(string path, FilesystemItemType itemType)
        {
            // Make sure we don't exceed maximum amount && make sure we don't make duplicates
            if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle && !Contents.Any((item) => item.Path == path))
            {
                return await AddBundleItem(new BundleItemViewModel(path, itemType)
                {
                    ParentBundleName = BundleName,
                    NotifyItemRemoved = NotifyItemRemovedHandle,
                    OpenPath = OpenPath,
                    OpenPathInNewPane = OpenPathInNewPane,
                });
            }

            return false;
        }

        private async Task<bool> AddItemsFromPath(IDictionary<string, FilesystemItemType> paths)
        {
            return await AddBundleItems(paths.Select((item) => new BundleItemViewModel(item.Key, item.Value)
            {
                ParentBundleName = BundleName,
                NotifyItemRemoved = NotifyItemRemovedHandle,
                OpenPath = OpenPath,
                OpenPathInNewPane = OpenPathInNewPane
            }));
        }

        private void UpdateAddItemOption()
        {
            if (Contents.Count >= Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
            {
                IsAddItemOptionEnabled = false;
            }
            else
            {
                IsAddItemOptionEnabled = true;
            }
        }

        #endregion Private Helpers

        #region Public Helpers

        public async Task<bool> AddBundleItem(BundleItemViewModel bundleItem)
        {
            // Make sure we don't exceed maximum amount && make sure we don't make duplicates
            if (bundleItem != null && Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle && !Contents.Any((item) => item.Path == bundleItem.Path))
            {
                itemAddedInternally = true;
                Contents.Add(bundleItem);
                itemAddedInternally = false;
                NoBundleContentsTextLoad = false;
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

            if (Contents.Count > 0)
            {
                NoBundleContentsTextLoad = false;
            }

            return this;
        }

        public (bool result, string reason) CanRenameBundle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "BundlesWidgetAddBundleErrorInputEmpty".GetLocalized());
            }

            if (!BundlesSettings.SavedBundles.Any((item) => item.Key == name))
            {
                return (true, string.Empty);
            }
            else
            {
                return (false, "BundlesWidgetAddBundleErrorAlreadyExists".GetLocalized());
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