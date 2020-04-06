using Files.Interacts;
using System;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files
{

    public sealed partial class Properties : Page
    {
        public AppWindow propWindow;

        public Properties()
        {
            this.InitializeComponent();
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                Loaded += Properties_Loaded;
            }
            else
            {
                this.OKButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void Properties_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Collect AppWindow-specific info
            propWindow = Interaction.AppWindows[this.UIContext];
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

        private async void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
        }
    }
}
