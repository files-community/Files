using Files.Filesystem;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Previews
{
    public class ImagePreviewViewModel : BasePreviewModel
    {
        private ImageSource imageSource;

        public ImagePreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions => new List<string>() {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".ico", ".webp"
        };

        public ImageSource ImageSource
        {
            get => imageSource;
            set => SetProperty(ref imageSource, value);
        }

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            using IRandomAccessStream stream = await Item.ItemFile.OpenAsync(FileAccessMode.Read);
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () => {
                BitmapImage bitmap = new();
                await bitmap.SetSourceAsync(stream);
                ImageSource = bitmap;
            });

            return new List<FileProperty>();
        }
    }
}