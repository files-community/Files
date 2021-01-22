using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Previews
{
    public class ImagePreviewViewModel : BasePreviewModel
    {
        public static List<string> Extensions => new List<string>() {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".ico", ".svg"
        };

        public ImagePreviewViewModel(ListedItem item) : base(item)
        {
        }

        private ImageSource imageSource;
        public ImageSource ImageSource
        {
            get => imageSource;
            set => SetProperty(ref imageSource, value);
        }

        public override async void LoadPreviewAndDetails()
        {
            try
            {
                FileRandomAccessStream stream = (FileRandomAccessStream)await ItemFile.OpenAsync(FileAccessMode.Read);

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
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            base.LoadSystemFileProperties();
        }
    }
}