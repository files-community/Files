using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.UI.Xaml.Controls;
using System;
using Files.Interacts;

namespace Files.Controls
{
    public class InteractionViewModel : ViewModelBase
    {
        private string _ToggleRibbonIcon = "";

        public InteractionViewModel()
        {

        }

        public string ToggleRibbonIcon
        {
            get => _ToggleRibbonIcon;
            set => Set(ref _ToggleRibbonIcon, value);
        }

       
    }
}
