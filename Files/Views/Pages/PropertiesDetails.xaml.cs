﻿using Files.Filesystem;
using Files.UserControls;
using Files.View_Models;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class PropertiesDetails : Page
    {
        public FileProperties BaseProperties { get; set; }

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public PropertiesDetails()
        {
            this.InitializeComponent();
        }

        private void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (BaseProperties != null)
            {
                BaseProperties.GetSpecialProperties();
                Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                BaseProperties.GetSystemFileProperties();
                stopwatch.Stop();
                Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
            }
        }

        private void SetOverviewVisibilities()
        {
            var name = ViewModel.ItemName.Split(".");
            var extension = name[name.Length - 1].ToLower();

            //if (extension.Contains("png") || extension.Contains("jpg") || extension.Contains("png") || extension.Contains("gif") || extension.Contains("jpeg"))
            //    OverviewImage.Visibility = Visibility.Visible;
        }

        private string GetStringArray(object array)
        {
            if (array == null || !(array is string[]))
                return "";

            var str = "";
            foreach (var i in array as string[])
                str += string.Format("{0}; ", i);

            return str;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new SelectedItemsPropertiesViewModel();
            var np = e.Parameter as Properties.PropertyNavParam;

            var listedItem = np.navParameter as ListedItem;
            if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
            {
                BaseProperties = new FileProperties(ViewModel, np.tokenSource, Dispatcher, null, listedItem);
            }

            SetOverviewVisibilities();
            base.OnNavigatedTo(e);
        }


        /// <summary>
        /// Returns false if the operation was cancelled
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<bool> SaveChanges()
        {
            try
            {
                await (BaseProperties as FileProperties).SyncPropertyChanges();
            }
            catch (Exception error)
            {
                var dialog = new PropertySaveError()
                {
                    Text = ResourceController.GetTranslation("PropertySaveErrorDialogText"),
                    PrimaryButtonText = ResourceController.GetTranslation("PropertySaveErrorRetry"),
                    SecondaryButtonText = ResourceController.GetTranslation("PropertySaveErrorDialogCloseAnyway"),
                    CloseButtonText = ResourceController.GetTranslation("PropertySaveErrorDialogCancel"),
                };
                switch (await dialog.ShowAsync())
                {
                    case ContentDialogResult.Primary:
                        SaveChanges();
                        return false;

                    case ContentDialogResult.Secondary:
                        break;

                    default:
                        return false;
                }
            }
            return true;
        }

        private async void ClearPropertiesConfirmation_Click(object sender, RoutedEventArgs e)
        {
            ClearPropertiesFlyout.Hide();
            await (BaseProperties as FileProperties).ClearProperties();
        }
    }
}