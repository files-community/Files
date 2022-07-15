using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using Files.Uwp.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels.Previews
{
    public abstract class BasePreviewModel : ObservableObject
    {
        private readonly IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

        public ListedItem Item { get; }

        private BitmapImage fileImage;
        public BitmapImage FileImage
        {
            get => fileImage;
            protected set => SetProperty(ref fileImage, value);
        }

        public List<FileProperty> DetailsFromPreview { get; set; }

        /// <summary>
        /// This is cancelled when the user has selected another file or closed the pane.
        /// </summary>
        public CancellationTokenSource LoadCancelledTokenSource { get; } = new CancellationTokenSource();

        public BasePreviewModel(ListedItem item) : base() => Item = item;

        public delegate void LoadedEventHandler(object sender, EventArgs e);

        public static async Task LoadDetailsOnlyAsync(ListedItem item, List<FileProperty> details = null)
        {
            var temp = new DetailsOnlyPreviewModel(item) { DetailsFromPreview = details };
            await temp.LoadAsync();
        }

        public static async Task<string> ReadFileAsTextAsync(BaseStorageFile file, int maxLength = 10 * 1024 * 1024)
            => await file.ReadTextAsync(maxLength);

        /// <summary>
        /// Call this function when you are ready to load the preview and details.
        /// Override if you need custom loading code.
        /// </summary>
        /// <returns>The task to run</returns>
        public virtual async Task LoadAsync()
        {
            List<FileProperty> detailsFull = new();
            if (Item.ItemFile is null)
            {
                var rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(Item.ItemPath));
                Item.ItemFile = await StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath, rootItem);
            }
            await Task.Run(async () =>
            {
                DetailsFromPreview = await LoadPreviewAndDetailsAsync();
                if (!userSettingsService.PaneSettingsService.ShowPreviewOnly)
                {
                    // Add the details from the preview function, then the system file properties
                    DetailsFromPreview?.ForEach(i => detailsFull.Add(i));
                    List<FileProperty> props = await GetSystemFilePropertiesAsync();
                    if (props is not null)
                    {
                        detailsFull.AddRange(props);
                    }
                }
            });

            Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(detailsFull);
        }

        /// <summary>
        /// Override this and place the code to load the file preview here.
        /// You can return details that may have been obtained while loading the preview (eg. word count).
        /// This details will be displayed *before* the system file properties.
        /// If there are none, return an empty list.
        /// </summary>
        /// <returns>A list of details</returns>
        public async virtual Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
        {
            var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Item.ItemFile, 400, ThumbnailMode.DocumentsView);
            iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 400);
            if (iconData is not null)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () => FileImage = await iconData.ToBitmapAsync());
            }
            else
            {
                FileImage ??= await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => new BitmapImage());
            }

            return new List<FileProperty>();
        }

        /// <summary>
        /// Override this if the preview control needs to handle the unloaded event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e) => LoadCancelledTokenSource.Cancel();

        protected static FileProperty GetFileProperty(string nameResource, object value)
            => new() { NameResource = nameResource, Value = value };

        private async Task<List<FileProperty>> GetSystemFilePropertiesAsync()
        {
            if (Item.IsShortcutItem)
            {
                return null;
            }

            var list = await FileProperty.RetrieveAndInitializePropertiesAsync(Item.ItemFile,
                Constants.ResourceFilePaths.PreviewPaneDetailsPropertiesJsonPath);

            list.Find(x => x.ID is "address").Value = await FileProperties.GetAddressFromCoordinatesAsync(
                (double?)list.Find(x => x.Property is "System.GPS.LatitudeDecimal").Value,
                (double?)list.Find(x => x.Property is "System.GPS.LongitudeDecimal").Value
            );

            // adds the value for the file tag
            if (userSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
            {
                list.FirstOrDefault(x => x.ID is "filetag").Value = 
                    Item.FileTagsUI is not null ? string.Join(',', Item.FileTagsUI.Select(x => x.TagName)) : null;
            }
            else
            {
                _ = list.Remove(list.FirstOrDefault(x => x.ID is "filetag"));
            }

            return list.Where(i => i.ValueText is not null).ToList();
        }

        private class DetailsOnlyPreviewModel : BasePreviewModel
        {
            public DetailsOnlyPreviewModel(ListedItem item) : base(item) {}

            public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync() => Task.FromResult(DetailsFromPreview);
        }
    }
}