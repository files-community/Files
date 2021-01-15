using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class ImagePreview : PreviewControlBase
    {
        public static List<string> Extensions => new List<string>() {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".ico", ".svg"
        };

        public ImagePreview(ListedItem item) : base(item)
        {
            this.InitializeComponent();
        }

        public override async void LoadPreviewAndDetails()
        {
            FileRandomAccessStream stream = (FileRandomAccessStream)await ItemFile.OpenAsync(FileAccessMode.Read);

            // svg files require a different type of source
            if (!Item.ItemPath.EndsWith(".svg"))
            {
                var bitmap = new BitmapImage();
                ImageContent.Source = bitmap;
                await bitmap.SetSourceAsync(stream);
            }
            else
            {
                var bitmap = new SvgImageSource();
                ImageContent.Source = bitmap;
                await bitmap.SetSourceAsync(stream);
            }

            base.LoadSystemFileProperties();
        }
    }
}
