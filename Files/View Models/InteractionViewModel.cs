using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.UI.Xaml.Controls;
using System;
using Files.Interacts;

namespace Files.Controls
{
    public class InteractionViewModel : ViewModelBase
    {
        private bool _PermanentlyDelete = false;
        private bool _IsSelectedItemImage = false;

        public InteractionViewModel()
        {

        }

        public bool PermanentlyDelete
        {
            get => _PermanentlyDelete;
            set => Set(ref _PermanentlyDelete, value);
        }
        
        public bool IsSelectedItemImage
        {
            get => _IsSelectedItemImage;
            set => Set(ref _IsSelectedItemImage, value);
        }

        private RelayCommand checkForImage;
        public RelayCommand CheckForImage => checkForImage = new RelayCommand(() =>
        {
            //check if the selected file is an image file
            try
            {
                string ItemExtension = (App.CurrentInstance.ContentPage as BaseLayout).SelectedItem.DotFileExtension;

                if (ItemExtension == "png" || ItemExtension == "jpg" || ItemExtension == "bmp" || ItemExtension == "jpeg")
                {
                    App.InteractionViewModel.IsSelectedItemImage = true;
                }
                else
                {
                    App.InteractionViewModel.IsSelectedItemImage = false;
                }
            }
            catch (Exception) { }
        });

    }
}
