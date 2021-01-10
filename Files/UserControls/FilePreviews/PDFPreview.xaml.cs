using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public PDFPreview(string path)
        {
            this.InitializeComponent();
            initialize(path);
        }

        ObservableCollection<Page> pages = new ObservableCollection<Page>(); 

        private async void initialize(string path)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var pdf = await PdfDocument.LoadFromFileAsync(file);

            for (uint i = 0; i < pdf.PageCount; i++)
            {
                var page = pdf.GetPage(i);
                await page.PreparePageAsync();
                var stream = new InMemoryRandomAccessStream();
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
    }
}
