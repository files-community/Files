using Files.View_Models;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Controls
{
    public class InteractionViewModel : ObservableObject
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public InteractionViewModel()
        {
            IsWindowCompactSize = InteractionViewModel.IsWindowResizedToCompactWidth();

            if (AppSettings.IsMultitaskingExperienceAdaptive)
                if (IsWindowCompactSize)
                {
                    IsVerticalTabFlyoutVisible = true;
                    IsHorizontalTabStripVisible = false;
                }
                else if (!IsWindowCompactSize)
                {
                    IsVerticalTabFlyoutVisible = false;
                    IsHorizontalTabStripVisible = true;
                }
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            App.InteractionViewModel.IsWindowCompactSize = InteractionViewModel.IsWindowResizedToCompactWidth();
        }

        private bool _IsContentLoadingIndicatorVisible = false;

        public bool IsContentLoadingIndicatorVisible
        {
            get => _IsContentLoadingIndicatorVisible;
            set => SetProperty(ref _IsContentLoadingIndicatorVisible, value);
        }

        private int _TabStripSelectedIndex = 0;

        public int TabStripSelectedIndex
        {
            get => _TabStripSelectedIndex;
            set
            {
                if (value >= 0)
                {
                    SetProperty(ref _TabStripSelectedIndex, value);
                    Frame rootFrame = Window.Current.Content as Frame;
                    var mainView = rootFrame.Content as MainPage;
                    mainView.SelectedTabItem = App.MultitaskingControl.Items[value];
                }
            }
        }

        private Thickness _TabsLeftMargin = new Thickness(0, 0, 0, 0);

        public Thickness TabsLeftMargin
        {
            get => _TabsLeftMargin;
            set => SetProperty(ref _TabsLeftMargin, value);
        }

        private bool _LeftMarginLoaded = true;

        public bool LeftMarginLoaded
        {
            get => _LeftMarginLoaded;
            set => SetProperty(ref _LeftMarginLoaded, value);
        }

        private bool _isPasteEnabled = false;

        public bool IsPasteEnabled
        {
            get => _isPasteEnabled;
            set => SetProperty(ref _isPasteEnabled, value);
        }

        private bool _isHorizontalTabStripVisible = App.AppSettings.IsMultitaskingExperienceAdaptive ? !IsWindowResizedToCompactWidth() : App.AppSettings.IsHorizontalTabStripEnabled;

        public bool IsHorizontalTabStripVisible
        {
            get => _isHorizontalTabStripVisible;
            set => SetProperty(ref _isHorizontalTabStripVisible, value);
        }

        private bool _isVerticalTabFlyoutVisible = App.AppSettings.IsMultitaskingExperienceAdaptive ? IsWindowResizedToCompactWidth() : App.AppSettings.IsVerticalTabFlyoutEnabled;

        public bool IsVerticalTabFlyoutVisible
        {
            get => _isVerticalTabFlyoutVisible;
            set => SetProperty(ref _isVerticalTabFlyoutVisible, value);
        }

        private bool _isWindowCompactSize = IsWindowResizedToCompactWidth();

        public static bool IsWindowResizedToCompactWidth()
        {
            return Window.Current.Bounds.Width <= 750 ? true : false;
        }

        public bool IsWindowCompactSize
        {
            get => _isWindowCompactSize;
            set
            {
                SetProperty(ref _isWindowCompactSize, value);
                if (value)
                {
                    IsHorizontalTabStripVisible = false;
                    IsVerticalTabFlyoutVisible = true;
                }
                else
                {
                    IsHorizontalTabStripVisible = true;
                    IsVerticalTabFlyoutVisible = false;
                }
            }
        }
    }
}