using GalaSoft.MvvmLight;
using System;
using Windows.UI.Xaml;

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

        private bool _isPasteEnabled = false;

        public bool IsPasteEnabled
        {
            get => _isPasteEnabled;
            set => Set(ref _isPasteEnabled, value);
        }      
    }
}