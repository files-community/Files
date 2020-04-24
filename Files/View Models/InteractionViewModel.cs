using GalaSoft.MvvmLight;
using System;
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

        private bool _PermanentlyDelete = false;

        public bool PermanentlyDelete
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

        public void CheckForImage()
        {
            //check if the selected item is an image file
            try
            {
                string ItemExtension = (App.CurrentInstance.ContentPage as BaseLayout).SelectedItem.FileExtension;

                if (ItemExtension == ".png" || ItemExtension == ".jpg" || ItemExtension == ".bmp" || ItemExtension == ".jpeg")
                {
                    // Since item is an image, set the IsSelectedItemImage property to true
                    App.InteractionViewModel.IsSelectedItemImage = true;
                }
                else
                {
                    // Since item is not an image, set the IsSelectedItemImage property to false
                    App.InteractionViewModel.IsSelectedItemImage = false;
                }
            }
            catch (Exception) { }
        }
    }
}