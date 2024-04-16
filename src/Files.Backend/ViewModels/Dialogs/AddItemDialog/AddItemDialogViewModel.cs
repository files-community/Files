using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Enums;
using Files.Backend.Extensions;
using Files.Backend.Models.Dialogs;
using Files.Backend.Models.Imaging;
using Files.Backend.Services;
using Files.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

#nullable enable

namespace Files.Backend.ViewModels.Dialogs.AddItemDialog
{
    public sealed class AddItemDialogViewModel : ObservableObject
    {
        public IImagingService ImagingService { get; } = Ioc.Default.GetRequiredService<IImagingService>();

        public ObservableCollection<AddItemDialogListItemViewModel> AddItemsList { get; }

        public AddItemDialogResultModel ResultType { get; set; } = new AddItemDialogResultModel() { ItemType = AddItemDialogItemType.Cancel };

        public AddItemDialogViewModel()
        {
            AddItemsList = new();
        }

        public async Task AddItemsToList(IEnumerable<ShellNewEntry> itemTypes)
        {
            AddItemsList.Clear();

            AddItemsList.Add(new AddItemDialogListItemViewModel
            {
                Header = "Folder".ToLocalized(),
                SubHeader = "AddDialogListFolderSubHeader".ToLocalized(),
                Glyph = "\xE838",
                IsItemEnabled = true,
                ItemResult = new AddItemDialogResultModel() { ItemType = AddItemDialogItemType.Folder }
            });

            foreach (var itemType in itemTypes)
            {
                ImageModel? imageModel = null;
                if (!string.IsNullOrEmpty(itemType.IconBase64))
                {
                    byte[] bitmapData = Convert.FromBase64String(itemType.IconBase64);
                    imageModel = await ImagingService.GetImageModelFromDataAsync(bitmapData);
                }

                AddItemsList.Add(new AddItemDialogListItemViewModel
                {
                    Header = itemType.Name,
                    SubHeader = itemType.Extension,
                    Glyph = imageModel != null ? null : "\xE8A5",
                    Icon = imageModel,
                    IsItemEnabled = true,
                    ItemResult = new AddItemDialogResultModel()
                    {
                        ItemType = AddItemDialogItemType.File,
                        ItemInfo = itemType
                    }
                });
            }

            AddItemsList.Add(new AddItemDialogListItemViewModel
            {
                Header = "File".ToLocalized(),
                SubHeader = "AddDialogListFileSubHeader".ToLocalized(),
                Glyph = "\xE8A5",
                IsItemEnabled = true,
                ItemResult = new AddItemDialogResultModel()
                {
                    ItemType = AddItemDialogItemType.File,
                    ItemInfo = new ShellNewEntry() // TODO(i): Make ItemInfo nullable and pass null there?
                }
            });
        }
    }
}
