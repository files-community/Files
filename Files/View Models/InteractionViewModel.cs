using Files.View_Models;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Controls
{
    public class InteractionViewModel : ObservableObject
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public InteractionViewModel()
        {
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            IsWindowCompactSize = IsWindowResizedToCompactWidth();

            if (AppSettings.IsMultitaskingExperienceAdaptive)
            {
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
            else
            {
                IsVerticalTabFlyoutVisible = false;
                IsHorizontalTabStripVisible = false;
            }
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

        private bool _IsPasteEnabled = false;

        public bool IsPasteEnabled
        {
            get => _IsPasteEnabled;
            set => SetProperty(ref _IsPasteEnabled, value);
        }

        private bool _IsHorizontalTabStripVisible = App.AppSettings.IsMultitaskingExperienceAdaptive ? !IsWindowResizedToCompactWidth() : App.AppSettings.IsHorizontalTabStripEnabled;

        public bool IsHorizontalTabStripVisible
        {
            get => _IsHorizontalTabStripVisible;
            set => SetProperty(ref _IsHorizontalTabStripVisible, value);
        }

        private bool _IsVerticalTabFlyoutVisible = App.AppSettings.IsMultitaskingExperienceAdaptive ? IsWindowResizedToCompactWidth() : App.AppSettings.IsVerticalTabFlyoutEnabled;

        public bool IsVerticalTabFlyoutVisible
        {
            get => _IsVerticalTabFlyoutVisible;
            set => SetProperty(ref _IsVerticalTabFlyoutVisible, value);
        }

        private bool _IsWindowCompactSize = IsWindowResizedToCompactWidth();

        public static bool IsWindowResizedToCompactWidth()
        {
            return Window.Current.Bounds.Width <= 750 ? true : false;
        }

        public bool IsWindowCompactSize
        {
            get => _IsWindowCompactSize;
            set
            {
                SetProperty(ref _IsWindowCompactSize, value);
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