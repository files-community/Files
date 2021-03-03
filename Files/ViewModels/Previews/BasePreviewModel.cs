using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Files.Common;

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

        public CancellationTokenSource LoadCancelledTokenSource { get; } = new CancellationTokenSource();

        public virtual void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
        {
            LoadCancelledTokenSource.Cancel();
        }

        public async virtual Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var (IconData, OverlayData, IsCustom) = await FileThumbnailHelper.LoadIconOverlayAsync(Item.ItemPath, 400);

            if (IconData != null)
            {
                Item.FileImage = await IconData.ToBitmapAsync();
            } else
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

        public virtual async Task LoadAsync()
        {
            // Files can be corrupt, in use, and stuff
            try
            {
                var detailsFull = new List<FileProperty>();
                Item.ItemFile ??= await StorageFile.GetFileFromPathAsync(Item.ItemPath);
                DetailsFromPreview = await LoadPreviewAndDetails();
                RaiseLoadedEvent();
                var props = await GetSystemFileProperties();

                DetailsFromPreview?.ForEach(i => detailsFull.Add(i));
                props?.ForEach(i => detailsFull.Add(i));

                Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(detailsFull);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public event LoadedEventHandler LoadedEvent;

        public delegate void LoadedEventHandler(object sender, EventArgs e);

        protected virtual void RaiseLoadedEvent()
        {
            // Raise the event in a thread-safe manner using the ?. operator.
            LoadedEvent?.Invoke(this, new EventArgs());
        }

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