using Files.UserControls;
using Files.Views;
using GalaSoft.MvvmLight;
using System;
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
                    mainView.SelectedTabItem = App.CurrentInstance.MultitaskingControl.Items[value];
                }
            }
        }

        private Thickness _TabsLeftMargin = new Thickness(0, 0, 0, 0);

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

        private bool _IsAppWindowSmall = Window.Current.Bounds.Width < 800;

        public bool IsAppWindowSmall
        {
            get => _IsAppWindowSmall;
            set 
            {
                Set(ref _IsAppWindowSmall, value);
                IsHorizontalTabStripVisible = !value;
            }
        }

        private bool _isHorizontalTabStripVisible = Window.Current.Bounds.Width > 800;

        public bool IsHorizontalTabStripVisible
        {
            get => _isHorizontalTabStripVisible;
            set => Set(ref _isHorizontalTabStripVisible, value);
        }
    }
}