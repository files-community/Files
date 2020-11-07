using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog
    {
        public AddItemType ResultType { get; private set; } = AddItemType.Cancel;

        public AddItemDialog()
        {
            InitializeComponent();
            AddItemsToList();
        }

        public List<AddListItem> AddItemsList = new List<AddListItem>();

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
            ResultType = (e.ClickedItem as AddListItem).ItemType;
            this.Hide();
        }
    }

    public enum AddItemType
    {
        Folder = 0,
        TextDocument = 1,
        BitmapImage = 2,
        CompressedArchive = 3,
        Cancel = 4
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