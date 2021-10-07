using Files.Dialogs;
using Files.Enums;
using Files.EventArguments.Bundles;
using Files.Filesystem;
using Files.Helpers;
using Files.Services;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.ViewModels.Widgets.Bundles
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
                PlaceholderText = "BundlesWidgetAddBundleInputPlaceholderText".GetLocalized()
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
                TitleText = "BundlesWidgetCreateBundleDialogTitleText".GetLocalized(),
                SubtitleText = "BundlesWidgetCreateBundleDialogSubtitleText".GetLocalized(),
                PrimaryButtonText = "BundlesWidgetCreateBundleDialogPrimaryButtonText".GetLocalized(),
                CloseButtonText = "Cancel".GetLocalized(),
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
            await dialog.ShowAsync();
        }

        private void AddBundle(string name)
        {
            if (!CanAddBundle(name).result)
            {
                return;
            }

            string savedBundleNameTextInput = name;
            BundleNameTextInput = string.Empty;

            if (BundlesSettingsService.SavedBundles == null || (BundlesSettingsService.SavedBundles?.ContainsKey(savedBundleNameTextInput) ?? false)) // Init
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
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(System.IO.Path.GetExtension(Constants.LocalSettings.BundlesSettingsFileName));

            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    string data = NativeFileOperationsHelper.ReadStringFromFile(file.Path);
                    var deserialized = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data);
                    BundlesSettingsService.ImportSettings(JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data));
                    await Load(); // Update the collection
                }
                catch // Couldn't deserialize, data is corrupted
                {
                }
            }
        }

        private async Task ExportBundles()
        {
            FileSavePicker filePicker = new FileSavePicker();
            filePicker.FileTypeChoices.Add("Json File", new List<string>() { System.IO.Path.GetExtension(Constants.LocalSettings.BundlesSettingsFileName) });

            StorageFile file = await filePicker.PickSaveFileAsync();

            if (file != null)
            {
                NativeFileOperationsHelper.WriteStringToFile(file.Path, (string)BundlesSettingsService.ExportSettings());
            }
        }

        #endregion Command Implementation

        #region Handlers

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
            BundleItemViewModel itemToRemove = this.Items.Where((item) => item.BundleName == bundleContainer).First().Contents.Where((item) => item.Path == bundleItemPath).First();
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
            if (BundlesSettingsService.SavedBundles != null)
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

                BundlesSettingsService.SavedBundles = bundles; // Calls Set()
            }
        }

        public async Task Load()
        {
            if (BundlesSettingsService.SavedBundles != null)
            {
                Items.Clear();

                // For every bundle in saved bundle collection:
                foreach (var bundle in BundlesSettingsService.SavedBundles)
                {
                    List<BundleItemViewModel> bundleItems = new List<BundleItemViewModel>();

                    // For every bundleItem in current bundle
                    foreach (var bundleItem in bundle.Value)
                    {
                        if (bundleItems.Count < Constants.Widgets.Bundles.MaxAmountOfItemsPerBundle)
                        {
                            if (bundleItem != null)
                            {
                                bundleItems.Add(new BundleItemViewModel(bundleItem, await StorageItemHelpers.GetTypeFromPath(bundleItem))
                                {
                                    ParentBundleName = bundle.Key,
                                    NotifyItemRemoved = NotifyBundleItemRemovedHandle,
                                    OpenPath = OpenPathHandle,
                                    OpenPathInNewPane = OpenPathInNewPaneHandle,
                                });
                            }
                        }
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
                    }.SetBundleItems(bundleItems));

                    itemAddedInternally = false;
                }

                if (Items.Count == 0)
                {
                    NoBundlesAddItemLoad = true;
                }
                else
                {
                    NoBundlesAddItemLoad = false;
                }
            }
            else // Null, therefore no items :)
            {
                NoBundlesAddItemLoad = true;
            }
        }

        public async Task Initialize()
        {
            await Load();
        }

        public (bool result, string reason) CanAddBundle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                AddBundleErrorText = "BundlesWidgetAddBundleErrorInputEmpty".GetLocalized();
                return (false, "BundlesWidgetAddBundleErrorInputEmpty".GetLocalized());
            }

            if (!Items.Any((item) => item.BundleName == name))
            {
                AddBundleErrorText = string.Empty;
                return (true, string.Empty);
            }
            else
            {
                AddBundleErrorText = "BundlesWidgetAddBundleErrorAlreadyExists".GetLocalized();
                return (false, "BundlesWidgetAddBundleErrorAlreadyExists".GetLocalized());
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
        }

        #endregion IDisposable
    }
}