using Files.Filesystem;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Files
{

    public sealed partial class Properties : Page
    {
        public Properties()
        {
            this.InitializeComponent();
        }

        private async void itemIcon_Loading(Windows.UI.Xaml.FrameworkElement sender, object args)
        {
            if (App.CurrentInstance.ContentPage.SelectedItem != null)
            {
                if (App.CurrentInstance.ContentPage.SelectedItem.FolderImg != Windows.UI.Xaml.Visibility.Visible)
                {
                    var thumbnail = await (await StorageFile.GetFileFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath)).GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 40, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumbnail);
                    itemIcon.Source = bitmap;
                }
                else
                {
                    EmptyImageIcon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                EmptyImageIcon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }


        }
    }
}
