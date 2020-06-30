using ByteSizeLib;
using Files.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.View_Models.Properties
{
    class CombinedProperties : BaseProperties
    {
        public CombinedProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
        }

        public override async void GetProperties()
        {
            ViewModel.LastSeparatorVisibility = Visibility.Collapsed;
            ViewModel.ItemSizeVisibility = Visibility.Visible;

            ViewModel.FilesCount += ViewModel.List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).ToList().Count;
            ViewModel.FoldersCount += ViewModel.List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.Folder).ToList().Count;

            long totalSize = 0;
            long filesSize = ViewModel.List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Sum(x => x.FileSizeBytes);
            long foldersSize = 0;

            ViewModel.ItemSizeProgressVisibility = Visibility.Visible;
            foreach (var item in ViewModel.List)
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
                + " (" + ByteSize.FromBytes(totalSize).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
            SetItemsCountString();
        }
    }
}
