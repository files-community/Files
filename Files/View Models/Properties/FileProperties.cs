using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.View_Models.Properties
{
    class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

        public ListedItem Item { get; }

        public FileProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, ProgressBar progressBar/*, ListedItem item*/)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            ProgressBar = progressBar;
            /*Item = item;

            if (Item != null)
            {
                ItemName = Item.ItemName;
                ItemType = Item.ItemType;
                ItemPath = Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath;
                ItemModifiedTimestamp = Item.ItemDateModified;
                FileIconSource = Item.FileImage;
                LoadFolderGlyph = Item.LoadFolderGlyph;
                LoadUnknownTypeGlyph = Item.LoadUnknownTypeGlyph;
                LoadFileIcon = Item.LoadFileIcon;
            }*/
        }

        public override async void GetProperties()
        {
            var file = await StorageFile.GetFileFromPathAsync(ViewModel.Item.ItemPath);
            ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(file.DateCreated);

            GetOtherProperties(file.Properties);
            ViewModel.ItemSizeVisibility = Visibility.Visible;
            ViewModel.ItemSize = ByteSize.FromBytes(ViewModel.Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()
                + " (" + ByteSize.FromBytes(ViewModel.Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";

            using (var Thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 80, ThumbnailOptions.UseCurrentScale))
            {
                BitmapImage icon = new BitmapImage();
                if (Thumbnail != null)
                {
                    ViewModel.FileIconSource = icon;
                    await icon.SetSourceAsync(Thumbnail);
                    ViewModel.LoadUnknownTypeGlyph = false;
                    ViewModel.LoadFileIcon = true;
                }
            }

            // Get file MD5 hash
            var hashAlgTypeName = HashAlgorithmNames.Md5;
            ViewModel.ItemMD5HashProgressVisibility = Visibility.Visible;
            ViewModel.ItemMD5HashVisibility = Visibility.Visible;
            try
            {
                ViewModel.ItemMD5Hash = await App.CurrentInstance.InteractionOperations
                    .GetHashForFile(ViewModel.Item, hashAlgTypeName, TokenSource.Token, ProgressBar);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                ViewModel.ItemMD5HashCalcError = true;
            }
        }
    }
}
