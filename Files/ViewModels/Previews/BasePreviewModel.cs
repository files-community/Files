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

        public virtual Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            LoadThumbnail();
            return Task.FromResult(new List<FileProperty>());
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


        private async void LoadThumbnail()
        {
            try
            {
                var (IconData, OverlayData, IsCustom) = await LoadIconOverlayAsync(Item.ItemPath, 400);

                if (IconData != null && !Item.IsLinkItem)
                {
                    Item.FileImage = await IconData.ToBitmapAsync();
                }
            }
            catch (Exception)
            {

            }
        }
        public async Task<(byte[] IconData, byte[] OverlayData, bool IsCustom)> LoadIconOverlayAsync(string filePath, uint thumbnailSize)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "GetIconOverlay" },
                    { "filePath", filePath },
                    { "thumbnailSize", (int)thumbnailSize }
                };
                var response = await connection.SendMessageAsync(value);
                var hasCustomIcon = (response.Status == AppServiceResponseStatus.Success)
                    && response.Message.Get("HasCustomIcon", false);
                var icon = response.Message.Get("Icon", (string)null);
                var overlay = response.Message.Get("Overlay", (string)null);

                // BitmapImage can only be created on UI thread, so return raw data and create
                // BitmapImage later to prevent exceptions once SynchorizationContext lost
                return (icon == null ? null : Convert.FromBase64String(icon),
                    overlay == null ? null : Convert.FromBase64String(overlay),
                    hasCustomIcon);
            }
            return (null, null, false);
        }

    }
}