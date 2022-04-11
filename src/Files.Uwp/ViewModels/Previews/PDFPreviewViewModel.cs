﻿using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels.Previews
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
            var fileStream = await Item.ItemFile.OpenReadAsync();
            var pdf = await PdfDocument.LoadFromStreamAsync(fileStream);
            TryLoadPagesAsync(pdf, fileStream);
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

        // the pips pager will crash when binding directly to Pages.Count, so count the pages here
        private int pageCount;
        public int PageCount
        {
            get => pageCount;
            set => SetProperty(ref pageCount, value);
        }

        public async void TryLoadPagesAsync(PdfDocument pdf, IRandomAccessStream fileStream)
        {
            try
            {
                await LoadPagesAsync(pdf);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                fileStream.Dispose();
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

                PdfPage page = pdf.GetPage(i);
                await page.PreparePageAsync();
                using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream);

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                using SoftwareBitmap sw = await decoder.GetSoftwareBitmapAsync();

                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                {
                    BitmapImage src = new();
                    PageViewModel pageData = new()
                    {
                        PageImage = src,
                        PageNumber = (int)i,
                        PageImageSB = sw,
                    };

                    await src.SetSourceAsync(stream);
                    Pages.Add(pageData);
                    PageCount++;
                });
            }
            LoadingBarVisibility = Visibility.Collapsed;
        }
    }

    public struct PageViewModel
    {
        public int PageNumber { get; set; }
        public BitmapImage PageImage { get; set; }
        public SoftwareBitmap PageImageSB { get; set; }
    }
}