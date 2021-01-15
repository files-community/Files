using Files.Filesystem;
using Files.ViewModels;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Pdf;
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
    public sealed partial class PDFPreview : UserControl
    {
        public static List<string> Extensions = new List<string>()
        {
            ".pdf",
        };
        public PDFPreview(ListedItem item)
        {
            this.InitializeComponent();
            _ = initialize(item, tokenSource.Token);
        }

        ObservableCollection<Page> pages = new ObservableCollection<Page>();

        CancellationTokenSource tokenSource = new CancellationTokenSource();

        private async Task initialize(ListedItem item, CancellationToken token)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            var pdf = await PdfDocument.LoadFromFileAsync(file);

            // Add the number of pages to the details
            item.FileDetails.Add(new FileProperty() {
                NameResource = "PropertyPageCount",
                Value = pdf.PageCount,
            });

            for (uint i = 0; i < pdf.PageCount; i++)
            {
                // Stop loading if the user has cancelled
                if(token.IsCancellationRequested)
                {
                    return;
                }

                var page = pdf.GetPage(i);
                await page.PreparePageAsync();
                using var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream);

                var src = new BitmapImage();
                await src.SetSourceAsync(stream);
                var pageData = new Page()
                {
                    PageImage = src,
                    PageNumber = (int)i,
                };

                pages.Add(pageData);
            }
            LoadingRing.Visibility = Visibility.Collapsed;
        }

        internal struct Page
        {
            public int PageNumber {get; set;}
            public BitmapImage PageImage { get; set; }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}
