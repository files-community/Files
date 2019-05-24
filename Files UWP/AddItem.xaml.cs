using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using Files.Filesystem;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{

    public sealed partial class AddItem : Page
    {
        public ListView addItemsChoices;
        public ItemViewModel<AddItem> instanceViewModel;
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
            if (parameters.GetType() == typeof(GenericFileBrowser))
            {
                instanceViewModel = new ItemViewModel<AddItem>();
            }
            else if (parameters.GetType() == typeof(PhotoAlbum))
            {
                instanceViewModel = new ItemViewModel<AddItem>();
            }
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
            var TabInstance = GetCurrentSelectedTabInstance<ProHome>();
            TabInstance.AddItemBox.Hide();
            string currentPath = null;
            if (TabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                currentPath = (TabInstance.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel.Universal.path;
            }
            else if (TabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                currentPath = (TabInstance.accessibleContentFrame.Content as PhotoAlbum).instanceViewModel.Universal.path;
            }
            StorageFolder folderToCreateItem = await StorageFolder.GetFolderFromPathAsync(currentPath);
            if ((e.ClickedItem as AddListItem).Header == "Folder")
            {
                await TabInstance.NameBox.ShowAsync();
                var userInput = TabInstance.inputForRename;
                if (userInput != null)
                {
                    var folder = await folderToCreateItem.CreateFolderAsync(userInput, CreationCollisionOption.FailIfExists);
                    instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId){ FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileType = "Folder", FileImg = null, FilePath = (instanceViewModel.Universal.path + "\\" + userInput) });
                }
            }
            else if ((e.ClickedItem as AddListItem).Header == "Text Document")
            {
                await TabInstance.NameBox.ShowAsync();
                var userInput = TabInstance.inputForRename;
                if (userInput != null)
                {
                    var folder = await folderToCreateItem.CreateFileAsync(userInput + ".txt", CreationCollisionOption.FailIfExists);
                    instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = "Text Document", FileImg = null, FilePath = (instanceViewModel.Universal.path + "\\" + userInput + ".txt"), DotFileExtension = ".txt" });
                }
            }
            else if ((e.ClickedItem as AddListItem).Header == "Bitmap Image")
            {
                await GetCurrentSelectedTabInstance<ProHome>().NameBox.ShowAsync();
                var userInput = GetCurrentSelectedTabInstance<ProHome>().inputForRename;
                if (userInput != null)
                {
                    var folder = await folderToCreateItem.CreateFileAsync(userInput + ".bmp", CreationCollisionOption.FailIfExists);
                    instanceViewModel.AddFileOrFolder(new ListedItem(folder.FolderRelativeId) { FileName = userInput, FileDateReal = DateTimeOffset.Now, EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = "BMP File", FileImg = null, FilePath = (instanceViewModel.Universal.path + "\\" + userInput + ".bmp"), DotFileExtension = ".bmp" });
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
