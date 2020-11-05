using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.View_Models.Properties
{
    internal class CombinedProperties : BaseProperties
    {
        public List<ListedItem> List { get; }

        public CombinedProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource,
            CoreDispatcher coreDispatcher, List<ListedItem> listedItems)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            List = listedItems;

            GetBaseProperties();
        }

        public override void GetBaseProperties()
        {
            if (List != null)
            {
                ViewModel.LoadCombinedItemsGlyph = true;
                if (List.All(x => x.ItemType.Equals(List.First().ItemType)))
                {
                    ViewModel.ItemType = string.Format("PropertiesDriveItemTypesEquals".GetLocalized(), List.First().ItemType);
                }
                else
                {
                    ViewModel.ItemType = "PropertiesDriveItemTypeDifferent".GetLocalized();
                }
                ViewModel.ItemPath = string.Format(
                    "PropertiesCombinedItemPath".GetLocalized(), Path.GetDirectoryName(List.First().ItemPath));
            }
        }

        public override async void GetSpecialProperties()
        {
            ViewModel.LastSeparatorVisibility = Visibility.Collapsed;
            ViewModel.ItemSizeVisibility = Visibility.Visible;

            ViewModel.FilesCount += List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).ToList().Count;
            ViewModel.FoldersCount += List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.Folder).ToList().Count;

            long totalSize = 0;
            long filesSize = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Sum(x => x.FileSizeBytes);
            long foldersSize = 0;

            ViewModel.ItemSizeProgressVisibility = Visibility.Visible;
            foreach (var item in List)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var fileSizeTask = Task.Run(async () =>
                    {
                        var size = await CalculateFolderSizeAsync(item.ItemPath, TokenSource.Token);
                        return size;
                    });
                    try
                    {
                        foldersSize += await fileSizeTask;
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                    }
                }
            }
            ViewModel.ItemSizeProgressVisibility = Visibility.Collapsed;

            totalSize = filesSize + foldersSize;
            ViewModel.ItemSize = ByteSize.FromBytes(totalSize).ToBinaryString().ConvertSizeAbbreviation()
                + " (" + ByteSize.FromBytes(totalSize).Bytes.ToString("#,##0") + " " + "ItemSizeBytes".GetLocalized() + ")";
            SetItemsCountString();
        }
    }
}