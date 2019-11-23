using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using Files.Filesystem;
using Windows.UI.Xaml.Navigation;
using Files.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace Files
{

    public sealed partial class AddItem : Page
    {
        public ListView addItemsChoices;
        public AddItem()
        {
            this.InitializeComponent();
            
            addItemsChoices = AddItemsListView;
            AddItemsToList();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter;

        }

        public static List<AddListItem> AddItemsList = new List<AddListItem>();
        
        public static void AddItemsToList()
        {
            AddItemsList.Clear();
            AddItemsList.Add(new AddListItem { Header = "Folder", SubHeader = "Creates an empty folder", Icon = "\xE838", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Text Document", SubHeader = "Creates a simple file for text input", Icon = "\xE8A5", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Bitmap Image", SubHeader = "Creates an empty bitmap image file", Icon = "\xEB9F", isEnabled = true });
            
        }

        public T GetCurrentSelectedTabInstance<T>()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            var selectedTabContent = ((InstanceTabsView.tabView.SelectedItem as TabViewItem).Content as Grid);
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T)((uiElement as Frame).Content);
                }
            }
            return default;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var TabInstance = App.OccupiedInstance;
            TabInstance.addItemDialog.Hide();
            CreateFile(TabInstance, (e.ClickedItem as AddListItem).Header);
        }

        public static async void CreateFile(ProHome TabInstance, String fileType)
        {
            string currentPath = null;
            if (TabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                currentPath = TabInstance.instanceViewModel.Universal.path;
            }
            else if (TabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                currentPath = TabInstance.instanceViewModel.Universal.path;
            }
            StorageFolder folderToCreateItem = await StorageFolder.GetFolderFromPathAsync(currentPath);
            RenameDialog renameDialog = new RenameDialog();

            await renameDialog.ShowAsync();
            var userInput = renameDialog.storedRenameInput;

            if (fileType == "Folder")
            {
                StorageFolder folder;
                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    folder = await folderToCreateItem.CreateFolderAsync(userInput, CreationCollisionOption.FailIfExists);
                }
                else
                {
                    folder = await folderToCreateItem.CreateFolderAsync("New Folder", CreationCollisionOption.GenerateUniqueName);
                }
                TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = folder.DisplayName, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileType = "Folder", FileImg = null, FilePath = folder.Path });
            }
            else if (fileType == "Text Document")
            {
                StorageFile item;
                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    item = await folderToCreateItem.CreateFileAsync(userInput + ".txt", CreationCollisionOption.FailIfExists);
                }
                else
                {
                    item = await folderToCreateItem.CreateFileAsync("New Text Document" + ".txt", CreationCollisionOption.GenerateUniqueName);
                }
                TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(item.FolderRelativeId) { FileName = item.DisplayName, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = item.DisplayType, FileImg = null, FilePath = item.Path, DotFileExtension = item.FileType });
            }
            else if (fileType == "Bitmap Image")
            {
                StorageFile item;
                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    item = await folderToCreateItem.CreateFileAsync(userInput + ".bmp", CreationCollisionOption.FailIfExists);
                }
                else
                {
                    item = await folderToCreateItem.CreateFileAsync("New Bitmap Image" + ".bmp", CreationCollisionOption.GenerateUniqueName);
                }
                TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(item.FolderRelativeId) { FileName = item.DisplayName, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = item.DisplayType, FileImg = null, FilePath = item.Path, DotFileExtension = item.FileType });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }

    public class AddListItem
    {
        public string Header { get; set; }
        public string SubHeader { get; set; }
        public string Icon { get; set; }
        public bool isEnabled { get; set; }
    }
}
