using Files.DataModels;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog
    {
        public AddItemResult ResultType { get; private set; } = new AddItemResult() { ItemType = AddItemType.Cancel };

        public AddItemDialog()
        {
            InitializeComponent();
            AddItemsToList();
        }

        public ObservableCollection<AddListItem> AddItemsList = new ObservableCollection<AddListItem>();

        public async void AddItemsToList()
        {
            AddItemsList.Clear();

            AddItemsList.Add(new AddListItem
            {
                Header = "AddDialogListFolderHeader".GetLocalized(),
                SubHeader = "AddDialogListFolderSubHeader".GetLocalized(),
                Icon = "\xE838",
                IsItemEnabled = true,
                ItemType = new AddItemResult() { ItemType = AddItemType.Folder }
            });

            var itemTypes = await RegistryHelper.GetNewContextMenuEntries();

            foreach (var itemType in itemTypes)
            {
                AddItemsList.Add(new AddListItem
                {
                    Header = itemType.Name,
                    SubHeader = itemType.Extension,
                    Icon = "\xE8A5",
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
                Icon = "\xE8A5",
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

    public enum AddItemType
    {
        Folder,
        File,
        Cancel
    }

    public class AddItemResult
    {
        public AddItemType ItemType { get; set; }
        public ShellNewEntry ItemInfo { get; set; }
    }

    public class AddListItem
    {
        public string Header { get; set; }
        public string SubHeader { get; set; }
        public string Icon { get; set; }
        public bool IsItemEnabled { get; set; }
        public AddItemResult ItemType { get; set; }
    }
}