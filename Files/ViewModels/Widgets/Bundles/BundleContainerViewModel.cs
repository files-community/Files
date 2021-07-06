using Files.Dialogs;
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

        private Visibility noBundleContentsTextVisibility;

        public Visibility NoBundleContentsTextVisibility
        {
            get => noBundleContentsTextVisibility;
            set => SetProperty(ref noBundleContentsTextVisibility, value);
        }

        #endregion Public Properties

        #region Commands

        public ICommand OpenItemCommand { get; private set; }

        public ICommand RemoveBundleCommand { get; private set; }

        public ICommand RenameBundleCommand { get; private set; }

        public ICommand DragOverCommand { get; private set; }

        public ICommand DropCommand { get; private set; }

        public ICommand DragItemsStartingCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public BundleContainerViewModel()
        {
            // Create commands
            RemoveBundleCommand = new RelayCommand(RemoveBundle);
            RenameBundleCommand = new RelayCommand(RenameBundle);
            DragOverCommand = new RelayCommand<DragEventArgs>(DragOver);
            DropCommand = new RelayCommand<DragEventArgs>(Drop);

            internalCollectionCount = Contents.Count;
            Contents.CollectionChanged += Contents_CollectionChanged;
            OpenItemCommand = new RelayCommand<ItemClickEventArgs>((e) =>
            {
                (e.ClickedItem as BundleItemViewModel).OpenItem();
            });
            DragItemsStartingCommand = new RelayCommand<DragItemsStartingEventArgs>(DragItemsStarting);
        }

        #endregion Constructor

        #region Command Implementation

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

        private async void RenameBundle()
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

        private async void Drop(DragEventArgs e)
        {
            var deferral = e?.GetDeferral();
            try
            {
                bool itemsAdded = false;

                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

                    foreach (IStorageItem item in items)
                    {
                        if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
                        {
                            if (!Contents.Any((i) => i.Path == item.Path)) // Don't add existing items!
                            {
                                await AddBundleItem(new BundleItemViewModel(item.Path, item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File)
                                {
                                    ParentBundleName = BundleName,
                                    NotifyItemRemoved = NotifyItemRemovedHandle,
                                    OpenPath = OpenPath,
                                    OpenPathInNewPane = OpenPathInNewPane,
                                });
                                itemsAdded = true;
                            }
                        }
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

                    if (item != null || itemPath.EndsWith(".lnk"))
                    {
                        if (Contents.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
                        {
                            if (!Contents.Any((i) => i.Path == itemPath)) // Don't add existing items!
                            {
                                await AddBundleItem(new BundleItemViewModel(itemPath, itemPath.EndsWith(".lnk") ? FilesystemItemType.File : (item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File))
                                {
                                    ParentBundleName = BundleName,
                                    NotifyItemRemoved = NotifyItemRemovedHandle,
                                    OpenPath = OpenPath,
                                    OpenPathInNewPane = OpenPathInNewPane,
                                });
                                itemsAdded = true;
                            }
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
                NoBundleContentsTextVisibility = Visibility.Visible;
            }
        }

        private void Contents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (internalCollectionCount < Contents.Count && !itemAddedInternally)
            {
                SaveBundle();
            }

            internalCollectionCount = Contents.Count;
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

        #endregion Private Helpers

        #region Public Helpers

        public async Task AddBundleItem(BundleItemViewModel bundleItem)
        {
            if (bundleItem != null)
            {
                itemAddedInternally = true;
                Contents.Add(bundleItem);
                itemAddedInternally = false;
                NoBundleContentsTextVisibility = Visibility.Collapsed;
                await bundleItem.UpdateIcon();
            }
        }

        public BundleContainerViewModel SetBundleItems(List<BundleItemViewModel> items)
        {
            Contents.CollectionChanged -= Contents_CollectionChanged;

            Contents = new ObservableCollection<BundleItemViewModel>(items);
            internalCollectionCount = Contents.Count;
            Contents.CollectionChanged += Contents_CollectionChanged;

            if (Contents.Count > 0)
            {
                NoBundleContentsTextVisibility = Visibility.Collapsed;
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

            BundleName = null;
            NotifyBundleItemRemoved = null;
            NotifyItemRemoved = null;
            RemoveBundleCommand = null;
            RenameBundleCommand = null;
            DragOverCommand = null;
            DropCommand = null;
            OpenItemCommand = null;
            DragItemsStartingCommand = null;

            OpenPath = null;
            OpenPathInNewPane = null;

            Contents.CollectionChanged -= Contents_CollectionChanged;
            Contents = null;
        }

        #endregion IDisposable
    }
}