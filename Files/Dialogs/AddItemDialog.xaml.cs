using System;
using System.Collections.Generic;
using Windows.Storage;
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
            AddItemsList.Add(new AddListItem { Header = "Folder", SubHeader = "Creates an empty folder", Icon = "\xE838", IsItemEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Text Document", SubHeader = "Creates a simple text file", Icon = "\xE8A5", IsItemEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Bitmap Image", SubHeader = "Creates an empty bitmap image file", Icon = "\xEB9F", IsItemEnabled = true });
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.AddItemDialogDisplay.Hide();
            switch ((e.ClickedItem as AddListItem).Header)
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
            var TabInstance = App.CurrentInstance;
            string currentPath = null;
            if (TabInstance.ContentPage != null)
            {
                currentPath = TabInstance.FilesystemViewModel.WorkingDirectory;
            }
            StorageFolder folderToCreateItem = await Filesystem.ItemViewModel.GetFolderFromPathAsync(currentPath);
            RenameDialog renameDialog = new RenameDialog();

            var renameResult = await renameDialog.ShowAsync();
            if (renameResult != ContentDialogResult.Primary)
            {
                return;
            }

            var userInput = renameDialog.storedRenameInput;

            if (fileType == AddItemType.Folder)
            {
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : ResourceController.GetTranslation("NewFolder");

                await folderToCreateItem.CreateFolderAsync(userInput, CreationCollisionOption.GenerateUniqueName);
            }
            else if (fileType == AddItemType.TextDocument)
            {
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : ResourceController.GetTranslation("NewTextDocument");

                await folderToCreateItem.CreateFileAsync(userInput + ".txt", CreationCollisionOption.GenerateUniqueName);
            }
            else if (fileType == AddItemType.BitmapImage)
            {
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : ResourceController.GetTranslation("NewBitmapImage");

                await folderToCreateItem.CreateFileAsync(userInput + ".bmp", CreationCollisionOption.GenerateUniqueName);
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
        public bool IsItemEnabled { get; set; }
    }
}