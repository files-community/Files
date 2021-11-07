using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Services;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Previews
{
    public abstract class BasePreviewModel : ObservableObject
    {
        protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

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
            var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Item.ItemFile, 400, ThumbnailMode.SingleItem);
            iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 400);
            if (iconData != null)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () => FileImage = await iconData.ToBitmapAsync());
            }
            else
            {
                FileImage ??= await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => new BitmapImage());
            }

            return new List<FileProperty>();
        }

        private BitmapImage fileImage;

        public BitmapImage FileImage
        {
            get => fileImage;
            set => SetProperty(ref fileImage, value);
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

            // adds the value for the file tag
            if (UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
            {
                list.FirstOrDefault(x => x.ID == "filetag").Value = Item.FileTagUI?.TagName;
            }
            else
            {
                _ = list.Remove(list.FirstOrDefault(x => x.ID == "filetag"));
            }

            return list.Where(i => i.ValueText != null).ToList();
        }

        /// <summary>
        /// Call this function when you are ready to load the preview and details.
        /// Override if you need custom loading code.
        /// </summary>
        /// <returns>The task to run</returns>
        public virtual async Task LoadAsync()
        {
            List<FileProperty> detailsFull = new();
            Item.ItemFile ??= await StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath);
            await Task.Run(async () =>
            {
                DetailsFromPreview = await LoadPreviewAndDetails();
                if (!UserSettingsService.PreviewPaneSettingsService.ShowPreviewOnly)
                {
                    // Add the details from the preview function, then the system file properties
                    DetailsFromPreview?.ForEach(i => detailsFull.Add(i));
                    List<FileProperty> props = await GetSystemFileProperties();
                    if(props is not null)
                    {
                        detailsFull.AddRange(props);
                    }
                }
            });

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

        public static async Task<string> ReadFileAsText(BaseStorageFile file, int maxLength = 10 * 1024 * 1024)
        {
            using (var stream = await file.OpenStreamForReadAsync())
            {
                var result = new StringBuilder();
                var bytesRead = 0;
                do
                {
                    var buffer = new byte[maxLength];
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    result.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                } while (bytesRead > 0 && result.Length <= maxLength);
                return result.ToString();
            }
        }
    }
}