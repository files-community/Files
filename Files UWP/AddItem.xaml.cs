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
        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var TabInstance = App.selectedTabInstance;
            TabInstance.addItemDialog.Hide();
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
            if ((e.ClickedItem as AddListItem).Header == "Folder")
            {
                await renameDialog.ShowAsync();
                var userInput = renameDialog.storedRenameInput;
                if (userInput != "")
                {
                    var folder = await folderToCreateItem.CreateFolderAsync(userInput, CreationCollisionOption.FailIfExists);
                    TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId){ FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileType = "Folder", FileImg = null, FilePath = (TabInstance.instanceViewModel.Universal.path + "\\" + userInput) });
                }
                else
                {
                    var folder = await folderToCreateItem.CreateFolderAsync("New Folder", CreationCollisionOption.GenerateUniqueName);
                    TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileType = "Folder", FileImg = null, FilePath = (TabInstance.instanceViewModel.Universal.path + "\\" + userInput) });
                }
            }
            else if ((e.ClickedItem as AddListItem).Header == "Text Document")
            {
                await renameDialog.ShowAsync();
                var userInput = renameDialog.storedRenameInput;
                if (userInput != "")
                {
                    var folder = await folderToCreateItem.CreateFileAsync(userInput + ".txt", CreationCollisionOption.FailIfExists);
                    TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = "Text Document", FileImg = null, FilePath = (TabInstance.instanceViewModel.Universal.path + "\\" + userInput + ".txt"), DotFileExtension = ".txt" });
                }
                else
                {
                    var folder = await folderToCreateItem.CreateFileAsync("New Text Document" + ".txt", CreationCollisionOption.GenerateUniqueName);
                    TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = "Text Document", FileImg = null, FilePath = (TabInstance.instanceViewModel.Universal.path + "\\" + userInput + ".txt"), DotFileExtension = ".txt" });
                }
            }
            else if ((e.ClickedItem as AddListItem).Header == "Bitmap Image")
            {
                await renameDialog.ShowAsync();
                var userInput = renameDialog.storedRenameInput;
                if (userInput != "")
                {
                    var folder = await folderToCreateItem.CreateFileAsync(userInput + ".bmp", CreationCollisionOption.FailIfExists);
                    TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = "BMP File", FileImg = null, FilePath = (TabInstance.instanceViewModel.Universal.path + "\\" + userInput + ".bmp"), DotFileExtension = ".bmp" });
                }
                else
                {
                    var folder = await folderToCreateItem.CreateFileAsync("New Bitmap Image" + ".bmp", CreationCollisionOption.GenerateUniqueName);
                    TabInstance.instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = "BMP File", FileImg = null, FilePath = (TabInstance.instanceViewModel.Universal.path + "\\" + userInput + ".bmp"), DotFileExtension = ".bmp" });
                }
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
