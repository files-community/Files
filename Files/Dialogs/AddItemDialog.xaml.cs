using Files.Filesystem;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Files.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog
    {
        public ListView addItemsChoices;
        public AddItemDialog()
        {
            this.InitializeComponent();
            addItemsChoices = AddItemsListView;
            AddItemsToList();
        }

        public List<AddListItem> AddItemsList = new List<AddListItem>();

        public void AddItemsToList()
        {
            AddItemsList.Clear();
            AddItemsList.Add(new AddListItem { Header = "Folder", SubHeader = "Creates an empty folder", Icon = "\xE838", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Text Document", SubHeader = "Creates a simple text file", Icon = "\xE8A5", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Bitmap Image", SubHeader = "Creates an empty bitmap image file", Icon = "\xEB9F", isEnabled = true });

        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.addItemDialog.Hide();
            switch((e.ClickedItem as AddListItem).Header)
            {
                case "Folder":
                    CreateFile(AddItemType.Folder);
                    break;
                case "Text Document":
                    CreateFile(AddItemType.TextDocument);
                    break;
                case "Bitmap Image":
                    CreateFile(AddItemType.BitmapImage);
                    break;
            }
        }

        public static async void CreateFile(AddItemType fileType)
        {
            var TabInstance = App.OccupiedInstance;
            string currentPath = null;
            if (TabInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                currentPath = TabInstance.instanceViewModel.Universal.path;
            }
            else if (TabInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                currentPath = TabInstance.instanceViewModel.Universal.path;
            }
            StorageFolder folderToCreateItem = await StorageFolder.GetFolderFromPathAsync(currentPath);
            RenameDialog renameDialog = new RenameDialog();

            await renameDialog.ShowAsync();
            var userInput = renameDialog.storedRenameInput;

            if (fileType == AddItemType.Folder)
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
            else if (fileType == AddItemType.TextDocument)
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
            else if (fileType == AddItemType.BitmapImage)
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
    }

    public enum AddItemType
    {
        Folder = 0,
        TextDocument = 1,
        BitmapImage = 2,
        CompressedArchive = 3
    }

    public class AddListItem
    {
        public string Header { get; set; }
        public string SubHeader { get; set; }
        public string Icon { get; set; }
        public bool isEnabled { get; set; }
    }
}
