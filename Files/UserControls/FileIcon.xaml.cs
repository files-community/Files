using Files.Helpers;
using Files.ViewModels;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.UserControls
{
    public sealed partial class FileIcon : UserControl
    {
        private SelectedItemsPropertiesViewModel viewModel;
        public SelectedItemsPropertiesViewModel ViewModel 
        {
            get => viewModel; 
            set {
                viewModel = value;

                if(value == null)
                {
                    return;
                }

                if (ViewModel?.CustomIconSource != null)
                {
                    CustomIconImageSource = new SvgImageSource(ViewModel.CustomIconSource);
                }
                InitStuff();
            }
        }

        private double itemSize;

        public double ItemSize
        {
            get => itemSize;
            set
            {
                itemSize = value;
                LargerItemSize = itemSize + 2.0;
            }
        }

        private double LargerItemSize { get; set; }

        private static DependencyProperty FileIconImageSourceProperty { get; } = DependencyProperty.Register(nameof(FileIconImageSource), typeof(BitmapImage), typeof(FileIcon), null);
        private BitmapImage FileIconImageSource 
        {
            get => GetValue(FileIconImageSourceProperty) as BitmapImage;
            set => SetValue(FileIconImageSourceProperty, value);
        }
        private SvgImageSource CustomIconImageSource { get; set; }

        public FileIcon()
        {
            this.InitializeComponent();
        }

        public async void InitStuff()
        {
            if(ViewModel.IconData != null)
            {
                FileIconImageSource = new BitmapImage();
                using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(ViewModel.IconData.AsBuffer());
                stream.Seek(0);
                await FileIconImageSource.SetSourceAsync(stream);
            }
        }
    }
}