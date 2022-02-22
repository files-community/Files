using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models.Icons;
using Files.Shared;
using Files.Backend.Extensions;
using System;
using System.Collections.ObjectModel;

namespace Files.Backend.ViewModels.Dialogs
{
    public sealed class AddItemDialogViewModel : ObservableObject
    {
        public ObservableCollection<AddListItem> AddItemsList = new ObservableCollection<AddListItem>();

        public AddItemResult ResultType { get; set; } = new AddItemResult() { ItemType = AddItemType.Cancel };

        public AddItemDialogViewModel()
        {
            AddItemsToList();
        }

        public async void AddItemsToList()
        {
            AddItemsList.Clear();

            AddItemsList.Add(new AddListItem
            {
                Header = "Folder".GetLocalized(),
                SubHeader = "AddDialogListFolderSubHeader".GetLocalized(),
                Glyph = "\xE838",
                IsItemEnabled = true,
                ItemType = new AddItemResult() { ItemType = AddItemType.Folder }
            });

            var itemTypes = await ShellNewEntryExtensions.GetNewContextMenuEntries();
            foreach (var itemType in itemTypes)
            {
                BitmapImage image = null;
                if (!string.IsNullOrEmpty(itemType.IconBase64))
                {
                    byte[] bitmapData = Convert.FromBase64String(itemType.IconBase64);
                    image = await bitmapData.ToBitmapAsync();
                }

                AddItemsList.Add(new AddListItem
                {
                    Header = itemType.Name,
                    SubHeader = itemType.Extension,
                    Glyph = image != null ? null : "\xE8A5",
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
                Header = "File".GetLocalized(),
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
    }

    public class AddListItem // TODO(i): Move to its own folder?
    {
        public string Header { get; set; }

        public string SubHeader { get; set; }

        public string Glyph { get; set; }

        public IconModel Icon { get; set; }

        public bool IsItemEnabled { get; set; }

        public AddItemResult ItemType { get; set; }
    }

    public class AddItemResult
    {
        public AddItemType ItemType { get; set; }

        public ShellNewEntry ItemInfo { get; set; }
    }

    public enum AddItemType
    {
        Folder,
        File,
        Cancel
    }
}
