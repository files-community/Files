using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog
    {
        private static readonly FilesystemHelpers _filesystemHelpers = new FilesystemHelpers(App.CurrentInstance, App.CancellationToken);

        public List<AddListItem> AddItemsList = new List<AddListItem>();

        public AddItemDialog()
        {
            InitializeComponent();
            AddItemsToList();
        }

        public void AddItemsToList()
        {
            AddItemsList.Clear();

            AddItemsList.Add(new AddListItem
            {
                Header = "AddDialogListFolderHeader".GetLocalized(),
                SubHeader = "AddDialogListFolderSubHeader".GetLocalized(),
                Icon = "\xE838",
                IsItemEnabled = true,
                ItemType = AddItemType.Folder
            });

            AddItemsList.Add(new AddListItem
            {
                Header = "AddDialogListTextFileHeader".GetLocalized(),
                SubHeader = "AddDialogListTextFileSubHeader".GetLocalized(),
                Icon = "\xE8A5",
                IsItemEnabled = true,
                ItemType = AddItemType.TextDocument
            });
            AddItemsList.Add(new AddListItem
            {
                Header = "AddDialogListBitmapHeader".GetLocalized(),
                SubHeader = "AddDialogListBitmapSubHeader".GetLocalized(),
                Icon = "\xEB9F",
                IsItemEnabled = true,
                ItemType = AddItemType.BitmapImage
            });
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.Hide();
            CreateFile((e.ClickedItem as AddListItem).ItemType);
        }

        public static async void CreateFile(AddItemType itemType)
        {
            string currentPath = null;
            if (App.CurrentInstance.ContentPage != null)
            {
                currentPath = App.CurrentInstance.FilesystemViewModel.WorkingDirectory;
            }

            StorageFolderWithPath folderWithPath = await ItemViewModel.GetFolderWithPathFromPathAsync(currentPath);
            StorageFolder folderToCreateItem = folderWithPath.Folder;

            // Show rename dialog
            RenameDialog renameDialog = new RenameDialog();
            var renameResult = await renameDialog.ShowAsync();
            if (renameResult != ContentDialogResult.Primary)
            {
                return;
            }

            // Create file based on dialog result
            string userInput = renameDialog.storedRenameInput;
            try
            {
                switch (itemType)
                {
                    case AddItemType.Folder:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalized();

                        await _filesystemHelpers.CreateAsync(Path.Combine(folderToCreateItem.Path, userInput), FilesystemItemType.Directory, null, true);
                        break;

                    case AddItemType.TextDocument:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewTextDocument".GetLocalized();

                        await _filesystemHelpers.CreateAsync(Path.Combine(folderToCreateItem.Path, (userInput + ".txt")), FilesystemItemType.File, null, true);
                        break;

                    case AddItemType.BitmapImage:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewBitmapImage".GetLocalized();

                        await _filesystemHelpers.CreateAsync(Path.Combine(folderToCreateItem.Path, (userInput + ".bmp")), FilesystemItemType.File, null, true);
                        break;
                }
            }
            catch (UnauthorizedAccessException)
            {
                await DialogDisplayHelper.ShowDialog("AccessDeniedCreateDialog/Title".GetLocalized(), "AccessDeniedCreateDialog/Text".GetLocalized());
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
        public AddItemType ItemType { get; set; }
    }
}