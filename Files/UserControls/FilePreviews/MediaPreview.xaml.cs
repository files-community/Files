using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
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
    public sealed partial class MediaPreview : UserControl
    {
        public static List<string> Extensions => new List<string>() {
            ".mp4", ".mp3", ".m4a",
        };

        public MediaPreview(string path)
        {
            this.InitializeComponent();
            SetFile(path);
        }

        public async void SetFile(string path)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            VideoPlayer.Source = MediaSource.CreateFromStorageFile(file);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Source = null;
        }
    }
}
