using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using Files.Filesystem;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files
{

    public sealed partial class AddItem : Page
    {
        public AddItem()
        {
            this.InitializeComponent();
            AddItemsToList();
        }

        public static List<AddListItem> AddItemsList = new List<AddListItem>();
        
        public static void AddItemsToList()
        {
            AddItemsList.Clear();
            AddItemsList.Add(new AddListItem { Header = "Folder", SubHeader = "Creates an empty folder", Icon = "\xE838", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Text Document", SubHeader = "Creates a simple file for text input", Icon = "\xE8A5", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Bitmap Image", SubHeader = "Creates an empty bitmap image file", Icon = "\xEB9F", isEnabled = true });
            
        }


        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {

            GenericFileBrowser.AddItemBox.Hide();
            var currentPath = ItemViewModel.PUIP.Path;
            StorageFolder folderToCreateItem = await StorageFolder.GetFolderFromPathAsync(currentPath);
            if ((e.ClickedItem as AddListItem).Header == "Folder")
            {
                await GenericFileBrowser.NameBox.ShowAsync();
                var userInput = GenericFileBrowser.inputForRename;
                if (userInput != null)
                {
                    await folderToCreateItem.CreateFolderAsync(userInput, CreationCollisionOption.FailIfExists);
                    ItemViewModel.FilesAndFolders.Add(new ListedItem(){ FileName = userInput, FileDate = "Now", EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileExtension = "Folder", FileImg = null, FilePath = (ItemViewModel.PUIP.Path + "\\" + userInput) });
                }
            }
            else if ((e.ClickedItem as AddListItem).Header == "Text Document")
            {
                await GenericFileBrowser.NameBox.ShowAsync();
                var userInput = GenericFileBrowser.inputForRename;
                if (userInput != null)
                {
                    await folderToCreateItem.CreateFileAsync(userInput + ".txt", CreationCollisionOption.FailIfExists);
                    ItemViewModel.FilesAndFolders.Add(new ListedItem() { FileName = userInput, FileDate = "Now", EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileExtension = "Text Document", FileImg = null, FilePath = (ItemViewModel.PUIP.Path + "\\" + userInput + ".txt"), DotFileExtension = ".txt" });
                }
            }
            else if ((e.ClickedItem as AddListItem).Header == "Bitmap Image")
            {
                await GenericFileBrowser.NameBox.ShowAsync();
                var userInput = GenericFileBrowser.inputForRename;
                if (userInput != null)
                {
                    await folderToCreateItem.CreateFileAsync(userInput + ".bmp", CreationCollisionOption.FailIfExists);
                    ItemViewModel.FilesAndFolders.Add(new ListedItem() { FileName = userInput, FileDate = "Now", EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileExtension = "BMP File", FileImg = null, FilePath = (ItemViewModel.PUIP.Path + "\\" + userInput + ".bmp"), DotFileExtension = ".bmp" });

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
