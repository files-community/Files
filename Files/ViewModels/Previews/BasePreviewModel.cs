using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;

namespace Files.ViewModels.Previews
{
    public abstract class BasePreviewModel : ObservableObject
    {
        public BasePreviewModel(ListedItem item) : base()
        {
            Item = item;
        }

        public ListedItem Item { get; internal set; }

        public List<FileProperty> DetailsFromPreview { get; set; }

        /// <summary>
        /// This is cancelled when the user has selected another file or closed the pane.
        /// </summary>
        public CancellationTokenSource LoadCancelledTokenSource { get; } = new CancellationTokenSource();

        /// <summary>
        /// Override this if the preview control needs to handle the unloaded event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
        {
            LoadCancelledTokenSource.Cancel();
        }

        /// <summary>
        /// Override this and place the code to load the file preview here.
        /// You can return details that may have been obtained while loading the preview (eg. word count).
        /// This details will be displayed *before* the system file properties.
        /// If there are none, return an empty list.
        /// </summary>
        /// <returns>A list of details</returns>
        public async virtual Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var (IconData, OverlayData, IsCustom) = await FileThumbnailHelper.LoadIconOverlayAsync(Item.ItemPath, 400);

            if (IconData != null)
            {
                Item.FileImage = await IconData.ToBitmapAsync();
            }
            else
            {
                using var icon = await Item.ItemFile.GetThumbnailAsync(ThumbnailMode.SingleItem, 400);
                Item.FileImage ??= new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                await Item.FileImage.SetSourceAsync(icon);
            }

            return new List<FileProperty>();
        }

        private async Task<List<FileProperty>> GetSystemFileProperties()
        {
            if (Item.IsShortcutItem)
            {
                return null;
            }

            var list = await FileProperty.RetrieveAndInitializePropertiesAsync(Item.ItemFile, Constants.ResourceFilePaths.PreviewPaneDetailsPropertiesJsonPath);

            list.Find(x => x.ID == "address").Value = await FileProperties.GetAddressFromCoordinatesAsync((double?)list.Find(x => x.Property == "System.GPS.LatitudeDecimal").Value,
                                                                                            (double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);
            return list.Where(i => i.Value != null).ToList();
        }

        /// <summary>
        /// Call this function when you are ready to load the preview and details.
        /// Override if you need custom loading code.
        /// </summary>
        /// <returns>The task to run</returns>
        public virtual async Task LoadAsync()
        {
            var detailsFull = new List<FileProperty>();
            Item.ItemFile ??= await StorageFile.GetFileFromPathAsync(Item.ItemPath);
            DetailsFromPreview = await LoadPreviewAndDetails();
            var props = await GetSystemFileProperties();

            // Add the details from the preview function, then the system file properties
            DetailsFromPreview?.ForEach(i => detailsFull.Add(i));
            props?.ForEach(i => detailsFull.Add(i));

            Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(detailsFull);
        }

        public delegate void LoadedEventHandler(object sender, EventArgs e);

        public static async Task LoadDetailsOnly(ListedItem item, List<FileProperty> details = null)
        {
            var temp = new DetailsOnlyPreviewModel(item) { DetailsFromPreview = details };
            await temp.LoadAsync();
        }

        internal class DetailsOnlyPreviewModel : BasePreviewModel
        {
            public DetailsOnlyPreviewModel(ListedItem item) : base(item)
            {
            }

            public override Task<List<FileProperty>> LoadPreviewAndDetails()
            {
                return Task.FromResult(DetailsFromPreview);
            }
        }
    }
}