using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

namespace Files.UserControls
{
    /// <summary>
    /// This control allows preview extensions to send images in their xaml in the form of a base64 string
    /// </summary>
    public sealed partial class StringEncodedImage : UserControl
    {
        public static readonly DependencyProperty EncodedImageProperty = DependencyProperty.Register("EncodedImage", typeof(String), typeof(StringEncodedImage), null);
        public string EncodedImage
        {
            get => (string)GetValue(EncodedImageProperty);
            set 
            { 
                SetValue(EncodedImageProperty, (string)value);
                SetImageFromString(value);
            }
        }
        public StringEncodedImage()
        {
            this.InitializeComponent();
        }

        private async void SetImageFromString(string encodedImage)
        {
            var array = Convert.FromBase64String(encodedImage);
            var buffer = array.AsBuffer();
            var source = new BitmapImage();
            var stream = buffer.AsStream();
            var rastream = stream.AsRandomAccessStream();
            await source.SetSourceAsync(rastream);
            MainImage.Source = source;
        }
    }
}
