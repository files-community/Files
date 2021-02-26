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
            LoadAsync();
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

        private async void LoadSystemFileProperties()
        {
            if (Item.IsShortcutItem)
            {
                return;
            }

            try
            {
                var list = await FileProperty.RetrieveAndInitializePropertiesAsync(Item.ItemFile, Constants.ResourceFilePaths.PreviewPaneDetailsPropertiesJsonPath);

                list.Find(x => x.ID == "address").Value = await FileProperties.GetAddressFromCoordinatesAsync((double?)list.Find(x => x.Property == "System.GPS.LatitudeDecimal").Value,
                                                                                               (double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);
                
                list.InsertRange(0, DetailsFromPreview ?? new List<FileProperty>());

                Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(list.Where(i => i.Value != null));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public virtual async void LoadAsync()
        {
            // Files can be corrupt, in use, and stuff
            try
            {
                Item.ItemFile ??= await StorageFile.GetFileFromPathAsync(Item.ItemPath);
                DetailsFromPreview ??= await LoadPreviewAndDetails();
                RaiseLoadedEvent();
                LoadSystemFileProperties();
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

        public static void LoadDetailsOnly(ListedItem item, List<FileProperty> details = null)
        {
            _ = new BasePreviewModel.DetailsOnlyPreviewModel(item) { DetailsFromPreview = details };
        }

        internal class DetailsOnlyPreviewModel : BasePreviewModel
        {
            public DetailsOnlyPreviewModel(ListedItem item) : base(item)
            {
            }
        }
    }
}