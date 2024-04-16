using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels.Previews
{
    public class ImagePreviewViewModel : BasePreviewModel
    {
        private ImageSource imageSource;
        public ImageSource ImageSource
        {
            get => imageSource;
            private set => SetProperty(ref imageSource, value);
        }

        public ImagePreviewViewModel(ListedItem item) : base(item) {}

        public static bool ContainsExtension(string extension)
            => extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tiff" or ".ico" or ".webp";

        public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
        {
            using IRandomAccessStream stream = await Item.ItemFile.OpenAsync(FileAccessMode.Read);
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
            {
                BitmapImage bitmap = new();
                await bitmap.SetSourceAsync(stream);
                ImageSource = bitmap;
            });

            return new List<FileProperty>();
        }
    }
}