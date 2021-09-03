using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            FileRandomAccessStream stream = (FileRandomAccessStream)await Item.ItemFile.OpenAsync(FileAccessMode.Read);

            // svg files require a different type of source
            if (!Item.ItemPath.EndsWith(".svg"))
            {
                var bitmap = new BitmapImage();
                ImageSource = bitmap;
                await bitmap.SetSourceAsync(stream);
            }
            else
            {
                var bitmap = new SvgImageSource();
                ImageSource = bitmap;
                await bitmap.SetSourceAsync(stream);
            }

            return new List<FileProperty>();
        }
    }
}