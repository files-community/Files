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

        public InteractionViewModel()
        {

        }

        public bool PermanentlyDelete
        {
            get => _PermanentlyDelete;
            set => Set(ref _PermanentlyDelete, value);
        }

       
    }
}
