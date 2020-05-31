using GalaSoft.MvvmLight;
using System;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.Controls
{
    public class InteractionViewModel : ViewModelBase
    {
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

        private bool _IsPageTypeNotHome = false;

        public bool IsPageTypeNotHome
        {
            get => _IsPageTypeNotHome;
            set => Set(ref _IsPageTypeNotHome, value);
        }

        private bool _IsPageTypeNotRecycleBin = false;

        public bool IsPageTypeNotRecycleBin
        {
            get => _IsPageTypeNotRecycleBin;
            set => Set(ref _IsPageTypeNotRecycleBin, value);
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