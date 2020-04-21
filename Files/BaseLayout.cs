using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Files.Views.Pages;
using Microsoft.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public abstract class BaseLayout : Page, INotifyPropertyChanged
    {
        public bool IsQuickLookEnabled { get; set; } = false;

        public ItemViewModel AssociatedViewModel = null;
        public Interaction AssociatedInteractions = null;
        public bool isRenamingItem = false;

        public MenuFlyout BaseLayoutItemContextFlyout { get; set; }


        private bool isItemSelected = false;

        public bool IsItemSelected
        {
            get
            {
                return isItemSelected;
            }
            internal set
            {
                if (value != isItemSelected)
                {
                    isItemSelected = value;
                    NotifyPropertyChanged("IsItemSelected");
                }
            }
        }

        private List<ListedItem> _SelectedItems;

        public List<ListedItem> SelectedItems
        {
            get
            {
                if (_SelectedItems == null)
                {
                    return new List<ListedItem>();
                }
                else
                {
                    return _SelectedItems;
                }
            }
            internal set
            {
                if (value != _SelectedItems)
                {
                    _SelectedItems = value;
                    if (value == null)
                    {
                        IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                    }
                    SetSelectedItemsOnUi(value);
                    NotifyPropertyChanged("SelectedItems");
                }
            }
        }

        private ListedItem _SelectedItem;

        public ListedItem SelectedItem
        {
            get
            {
                return _SelectedItem;
            }
            internal set
            {
                if (value != _SelectedItem)
                {
                    _SelectedItem = value;
                    if (value == null)
                    {
                        IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                    }
                    SetSelectedItemOnUi(value);
                    NotifyPropertyChanged("SelectedItem");
                }
            }
        }

        public BaseLayout()
        {
            this.Loaded += Page_Loaded;
            Page_Loaded(null, null);

            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                IsQuickLookEnabled = true;
            }
        }

        protected async virtual Task SetSelectedItemOnUi(ListedItem selectedItem)
        {
            var contextMenuItems = BaseLayoutItemContextFlyout.Items.Where(c => c.Tag != null && ParseContextMenuTag(c.Tag).commandKey != null);

            foreach (var contextMenuItem in contextMenuItems)
            {
                BaseLayoutItemContextFlyout.Items.Remove(contextMenuItem);
            }
            if (selectedItem != null)
            {
               
                var menuFlyoutItems = new RegistryReader().GetExtensionContextMenuForFiles(selectedItem.FileExtension);
                await LoadMenuFlyoutItem(menuFlyoutItems);
            }
           
        }

        protected async virtual Task SetSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            var contextMenuItems = BaseLayoutItemContextFlyout.Items.Where(c => c.Tag != null && ParseContextMenuTag(c.Tag).commandKey != null);

            foreach (var contextMenuItem in contextMenuItems)
            {
                BaseLayoutItemContextFlyout.Items.Remove(contextMenuItem);
            }
            if (selectedItems != null)
            {
                foreach (var selectedItem in selectedItems)
                {
                    var menuFlyoutItems = new RegistryReader().GetExtensionContextMenuForFiles(selectedItem.FileExtension);
                    await LoadMenuFlyoutItem(menuFlyoutItems);
                }
            }
        }

        public abstract void FocusSelectedItems();

        protected abstract ListedItem GetItemFromElement(object element);

        private void AppSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                App.CurrentInstance.ViewModel.IsLoadingItems = true;
                App.CurrentInstance.ViewModel.IsLoadingItems = false;
                if (App.AppSettings.LayoutMode == 0)
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.CurrentInstance.ViewModel.WorkingDirectory, null);
                }
                else
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), App.CurrentInstance.ViewModel.WorkingDirectory, null);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            App.AppSettings.LayoutModeChangeRequested += AppSettings_LayoutModeChangeRequested;
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            var parameters = (string)eventArgs.Parameter;
            if (App.AppSettings.FormFactor == Enums.FormFactorMode.Regular)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                InstanceTabsView instanceTabsView = rootFrame.Content as InstanceTabsView;
                instanceTabsView.TabStrip_SelectionChanged(null, null);
            }
            App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            IsItemSelected = false;
            AssociatedViewModel.EmptyTextState.IsVisible = Visibility.Collapsed;
            App.CurrentInstance.ViewModel.WorkingDirectory = parameters;

            if (App.CurrentInstance.ViewModel.WorkingDirectory == Path.GetPathRoot(App.CurrentInstance.ViewModel.WorkingDirectory))
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            }
            else
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = true;
            }

            await App.CurrentInstance.ViewModel.RefreshItems();

            App.Clipboard_ContentChanged(null, null);
            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = parameters;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            if (App.CurrentInstance.ViewModel._fileQueryResult != null)
            {
                App.CurrentInstance.ViewModel._fileQueryResult.ContentsChanged -= App.CurrentInstance.ViewModel.FileContentsChanged;
            }
            App.AppSettings.LayoutModeChangeRequested -= AppSettings_LayoutModeChangeRequested;
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            Windows.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(this.FindName(nameToUnload) as DependencyObject);
        }

        private async Task LoadMenuFlyoutItem(IEnumerable<(string commandKey,string commandName, string commandIcon, string command)> menuFlyoutItems)
        {
            foreach (var menuFlyoutItem in menuFlyoutItems)
            {
                if (BaseLayoutItemContextFlyout.Items.Any(c => ParseContextMenuTag(c.Tag).commandKey == menuFlyoutItem.commandKey))
                {
                    continue;
                }
               
                var menuLayoutItem = new MenuFlyoutItem()
                {
                    Text = menuFlyoutItem.commandName,
                    Tag = menuFlyoutItem
                };
                menuLayoutItem.Click += MenuLayoutItem_Click;
                
                BaseLayoutItemContextFlyout.Items.Add(menuLayoutItem);
            }
        }

        private (string commandKey, string commandName, string commandIcon, string command) ParseContextMenuTag(object tag)
        {
            if(tag is ValueTuple<string, string, string, string>)
            {
                (string commandKey, string commandName, string commandIcon, string command) = (ValueTuple<string, string, string, string>)tag;
                return (commandKey, commandName, commandIcon, command);
            }

            return (null, null, null, null);
        }

        private async void MenuLayoutItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedFileSystemItems = (App.CurrentInstance.ContentPage as BaseLayout).SelectedItems;
            var currentMenuLayoutItem = (MenuFlyoutItem)sender;
            if (currentMenuLayoutItem != null)
            {
                var (_, _, _, command) = ParseContextMenuTag(currentMenuLayoutItem.Tag);
                if (selectedFileSystemItems.Count > 1)
                {
                    var commandsToExecute = new List<string>();
                    foreach (var selectedDataItem in selectedFileSystemItems)
                    {
                        var commandToExecute = command?.Replace("%1", selectedDataItem.ItemPath);
                        if (!string.IsNullOrEmpty(commandToExecute))
                        {
                            commandsToExecute.Add(commandToExecute);
                        }
                    }
                    if(commandsToExecute.Count > 0)
                    {
                        await Interaction.InvokeWin32Components(commandsToExecute);
                    }
                }
                else if (selectedFileSystemItems.Count == 1)
                {
                    var selectedDataItem = selectedFileSystemItems[0] as ListedItem;

                    var commandToExecute = command?.Replace("%1", selectedDataItem.ItemPath);
                    if (!string.IsNullOrEmpty(commandToExecute))
                    {
                        await Interaction.InvokeWin32Component(commandToExecute);
                    }
                }
            }
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            var selectedFileSystemItems = (App.CurrentInstance.ContentPage as BaseLayout).SelectedItems;
          

            // Find selected items that are not folders
            if (selectedFileSystemItems.Cast<ListedItem>().Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (selectedFileSystemItems.Count == 1)
                {
                    var selectedDataItem = selectedFileSystemItems[0] as ListedItem;

                    if (selectedDataItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        UnloadMenuFlyoutItemByName("OpenItem");
                        this.FindName("UnzipItem");
                    }
                    else if (!selectedDataItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        this.FindName("OpenItem");
                        UnloadMenuFlyoutItemByName("UnzipItem");
                    }

                }
                else if (selectedFileSystemItems.Count > 1)
                {
                    UnloadMenuFlyoutItemByName("OpenItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }
            }
            else     // All are Folders
            {
                UnloadMenuFlyoutItemByName("OpenItem");
                if (selectedFileSystemItems.Count <= 5 && selectedFileSystemItems.Count > 0)
                {
                    this.FindName("SidebarPinItem");
                    this.FindName("OpenInNewTab");
                    this.FindName("OpenInNewWindowItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }
                else if (selectedFileSystemItems.Count > 5)
                {
                    this.FindName("SidebarPinItem");
                    UnloadMenuFlyoutItemByName("OpenInNewTab");
                    UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }
            }

            //check if the selected file is an image
            App.InteractionViewModel.CheckForImage();

            
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedViewModel == null && AssociatedInteractions == null)
            {
                AssociatedViewModel = App.CurrentInstance.ViewModel;
                AssociatedInteractions = App.CurrentInstance.InteractionOperations;
                if (App.CurrentInstance == null)
                {
                    App.CurrentInstance = ItemViewModel.GetCurrentSelectedTabInstance<ModernShellPage>();
                }
            }
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            char letterPressed = Convert.ToChar(args.KeyCode);
            App.CurrentInstance.InteractionOperations.PushJumpChar(letterPressed);
        }

        protected async void List_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();
                // As long as one file doesn't already belong to this folder
                if (draggedItems.Any(draggedItem => !Directory.GetParent(draggedItem.Path).FullName.Equals(App.CurrentInstance.ViewModel.WorkingDirectory, StringComparison.OrdinalIgnoreCase)))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                    e.Handled = true;
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
            }
        }

        protected async void List_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                await AssociatedInteractions.PasteItems(e.DataView, App.CurrentInstance.ViewModel.WorkingDirectory, e.AcceptedOperation);
                e.Handled = true;
            }
        }

        protected async void Item_DragStarting(object sender, DragStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();
            foreach (ListedItem item in App.CurrentInstance.ContentPage.SelectedItems)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    selectedStorageItems.Add(await StorageFile.GetFileFromPathAsync(item.ItemPath));
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                    selectedStorageItems.Add(await StorageFolder.GetFolderFromPathAsync(item.ItemPath));
            }

            if (selectedStorageItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Data.SetStorageItems(selectedStorageItems);
            e.DragUI.SetContentFromDataPackage();
        }

        protected async void Item_DragOver(object sender, DragEventArgs e)
        {
            ListedItem item = GetItemFromElement(sender);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();
                // Items from the same parent folder as this folder are dragged into this folder, so we move the items instead of copy
                if (draggedItems.Any(draggedItem => Directory.GetParent(draggedItem.Path).FullName == Directory.GetParent(item.ItemPath).FullName))
                {
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            ListedItem rowItem = GetItemFromElement(sender);
            await App.CurrentInstance.InteractionOperations.PasteItems(e.DataView, rowItem.ItemPath, e.AcceptedOperation);
        }

        protected void InitializeDrag(UIElement element)
        {
            ListedItem item = GetItemFromElement(element);
            if (item != null)
            {
                element.AllowDrop = false;
                element.DragStarting -= Item_DragStarting;
                element.DragStarting += Item_DragStarting;
                element.DragOver -= Item_DragOver;
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    element.AllowDrop = true;
                    element.DragOver += Item_DragOver;
                    element.Drop += Item_Drop;
                }
            }
        }
    }
}