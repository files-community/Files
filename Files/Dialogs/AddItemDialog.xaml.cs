using Files.DataModels;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Dialogs
{
    public enum AddItemType
    {
        Folder,
        File,
        Cancel
    }

    public sealed partial class AddItemDialog : ContentDialog
    {
        public ObservableCollection<AddListItem> AddItemsList = new ObservableCollection<AddListItem>();

        public AddItemDialog()
        {
            InitializeComponent();
            AddItemsToList();
        }

        public AddItemResult ResultType { get; private set; } = new AddItemResult() { ItemType = AddItemType.Cancel };

        public async void AddItemsToList()
        {
            AddItemsList.Clear();

            AddItemsList.Add(new AddListItem
            {
                Header = "AddDialogListFolderHeader".GetLocalized(),
                SubHeader = "AddDialogListFolderSubHeader".GetLocalized(),
                Glyph = "\xE838",
                IsItemEnabled = true,
                ItemType = new AddItemResult() { ItemType = AddItemType.Folder }
            });

            var itemTypes = await RegistryHelper.GetNewContextMenuEntries();

            foreach (var itemType in itemTypes)
            {
                BitmapImage image = null;
                if (itemType.Icon != null)
                {
                    image = new BitmapImage();
                    await image.SetSourceAsync(itemType.Icon);
                }

                AddItemsList.Add(new AddListItem
                {
                    Header = itemType.Name,
                    SubHeader = itemType.Extension,
                    Glyph = itemType.Icon != null ? null : "\xE8A5",
                    Icon = image,
                    IsItemEnabled = true,
                    ItemType = new AddItemResult()
                    {
                        ItemType = AddItemType.File,
                        ItemInfo = itemType
                    }
                });
            }

            AddItemsList.Add(new AddListItem
            {
                Header = "AddDialogListFileHeader".GetLocalized(),
                SubHeader = "AddDialogListFileSubHeader".GetLocalized(),
                Glyph = "\xE8A5",
                IsItemEnabled = true,
                ItemType = new AddItemResult()
                {
                    ItemType = AddItemType.File,
                    ItemInfo = new ShellNewEntry()
                }
            });
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ResultType = (e.ClickedItem as AddListItem).ItemType;
            this.Hide();
        }
    }

    public class AddItemResult
    {
        public ShellNewEntry ItemInfo { get; set; }
        public AddItemType ItemType { get; set; }
    }

    public class AddListItem
    {
        public string Glyph { get; set; }
        public string Header { get; set; }
        public BitmapImage Icon { get; set; }
        public bool IsItemEnabled { get; set; }
        public AddItemResult ItemType { get; set; }
        public string SubHeader { get; set; }
    }
}