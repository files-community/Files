using Files.UserControls;
using Files.Views;
using GalaSoft.MvvmLight;
using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Controls
{
    public class InteractionViewModel : ViewModelBase
    {
        private bool _IsContentLoadingIndicatorVisible = false;

        public bool IsContentLoadingIndicatorVisible
        {
            get => _IsContentLoadingIndicatorVisible;
            set => Set(ref _IsContentLoadingIndicatorVisible, value);
        }

        private int _TabStripSelectedIndex = 0;

        public int TabStripSelectedIndex
        {
            get => _TabStripSelectedIndex;
            set 
            { 
                if (value >= 0) 
                { 
                    Set(ref _TabStripSelectedIndex, value);
                    Frame rootFrame = Window.Current.Content as Frame;
                    var mainView = rootFrame.Content as MainPage;
                    mainView.SelectedTabItem = VerticalTabView.Items[value];
                }
            }
        }

        private Thickness _TabsLeftMargin = new Thickness(200, 0, 0, 0);

        public Thickness TabsLeftMargin
        {
            get => _TabsLeftMargin;
            set => Set(ref _TabsLeftMargin, value);
        }

        private bool _LeftMarginLoaded = true;

        public bool LeftMarginLoaded
        {
            get => _LeftMarginLoaded;
            set => Set(ref _LeftMarginLoaded, value);
        }

        private StorageDeleteOption _PermanentlyDelete = StorageDeleteOption.Default;

        public StorageDeleteOption PermanentlyDelete
        {
            get => _PermanentlyDelete;
            set => Set(ref _PermanentlyDelete, value);
        }

        private bool _IsSelectedItemImage = false;

        public bool IsSelectedItemImage
        {
            get => _IsSelectedItemImage;
            set => Set(ref _IsSelectedItemImage, value);
        }

        public void CheckForImage()
        {
            //check if the selected item is an image file
            string ItemExtension = App.CurrentInstance.ContentPage.SelectedItem.FileExtension;

            if (!string.IsNullOrEmpty(ItemExtension))
            {
                if (ItemExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    // Since item is an image, set the IsSelectedItemImage property to true
                    App.InteractionViewModel.IsSelectedItemImage = true;
                    return;
                }
            }

            // Since item is not an image, folder or file without extension, set the IsSelectedItemImage property to false
            App.InteractionViewModel.IsSelectedItemImage = false;
        }
    }
}