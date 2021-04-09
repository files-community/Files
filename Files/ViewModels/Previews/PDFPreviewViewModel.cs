using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Previews
{
    public class PDFPreviewViewModel : BasePreviewModel
    {
        private Visibility loadingBarVisibility;

        public PDFPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions = new List<string>()
        {
            ".pdf",
        };

        public Visibility LoadingBarVisibility
        {
            get => loadingBarVisibility;
            set => SetProperty(ref loadingBarVisibility, value);
        }

        public ObservableCollection<PageViewModel> Pages { get; set; } = new ObservableCollection<PageViewModel>();

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var pdf = await PdfDocument.LoadFromFileAsync(Item.ItemFile);
            TryLoadPagesAsync(pdf);
            var details = new List<FileProperty>
            {

                // Add the number of pages to the details
                new FileProperty()
                {
                    NameResource = "PropertyPageCount",
                    Value = pdf.PageCount,
                }
            };

            return details;
        }

        public async void TryLoadPagesAsync(PdfDocument pdf)
        {
            try
            {
                await LoadPagesAsync(pdf);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private async Task LoadPagesAsync(PdfDocument pdf)
        {
            // This fixes an issue where loading an absurdly large PDF would take to much RAM
            // and eventually cause a crash
            var limit = Math.Clamp(pdf.PageCount, 0, Constants.PreviewPane.PDFPageLimit);

            for (uint i = 0; i < limit; i++)
            {
                // Stop loading if the user has cancelled
                if (LoadCancelledTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                var page = pdf.GetPage(i);
                await page.PreparePageAsync();
                using var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream);

                var src = new BitmapImage();
                await src.SetSourceAsync(stream);
                var pageData = new PageViewModel()
                {
                    PageImage = src,
                    PageNumber = (int)i,
                };
                Pages.Add(pageData);
            }
            LoadingBarVisibility = Visibility.Collapsed;
        }

        public struct PageViewModel
        {
            public int PageNumber { get; set; }
            public BitmapImage PageImage { get; set; }
        }
    }
}