using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Files.ViewModels
{
    public class InteractionViewModel : ObservableObject
    {
        // TODO Add xml comments for the properties

        private FontFamily fontName;

        private bool isFullTrustElevated = false;
        private bool isHorizontalTabStripVisible = false;
        private bool isPasteEnabled = false;
        private bool isVerticalTabFlyoutVisible = false;
        private bool isWindowCompactSize = IsWindowResizedToCompactWidth();
        private bool multiselectEnabled;
        private int tabStripSelectedIndex = 0;

        public InteractionViewModel()
        {
            Window.Current.SizeChanged += Current_SizeChanged;

            DetectFontName();
            SetMultitaskingControl();
        }

        public SettingsViewModel AppSettings => App.AppSettings;

        public FontFamily FontName
        {
            get => fontName;
            set => SetProperty(ref fontName, value);
        }

        public bool IsFullTrustElevated
        {
            get => isFullTrustElevated;
            set => SetProperty(ref isFullTrustElevated, value);
        }

        public bool IsHorizontalTabStripVisible
        {
            get => isHorizontalTabStripVisible;
            set => SetProperty(ref isHorizontalTabStripVisible, value);
        }

        public bool IsPasteEnabled
        {
            get => isPasteEnabled;
            set => SetProperty(ref isPasteEnabled, value);
        }

        public bool IsQuickLookEnabled { get; set; }

        public bool IsVerticalTabFlyoutVisible
        {
            get => isVerticalTabFlyoutVisible;
            set => SetProperty(ref isVerticalTabFlyoutVisible, value);
        }

        public bool IsWindowCompactSize
        {
            get => isWindowCompactSize;
            set
            {
                SetProperty(ref isWindowCompactSize, value);
            }
        }

        public bool MultiselectEnabled
        {
            get => multiselectEnabled;
            set => SetProperty(ref multiselectEnabled, value);
        }

        public int TabStripSelectedIndex
        {
            get => tabStripSelectedIndex;
            set
            {
                if (value >= 0)
                {
                    if (tabStripSelectedIndex != value)
                    {
                        SetProperty(ref tabStripSelectedIndex, value);
                    }
                    if (value < MainPageViewModel.MultitaskingControl.Items.Count)
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        var mainView = rootFrame.Content as MainPage;
                        mainView.ViewModel.SelectedTabItem = MainPageViewModel.MultitaskingControl.Items[value];
                    }
                }
            }
        }

        public static bool IsWindowResizedToCompactWidth()
        {
            return Window.Current.Bounds.Width <= 750;
        }

        public void SetMultitaskingControl()
        {
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

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            IsWindowCompactSize = IsWindowResizedToCompactWidth();

            // Setup the correct multitasking control
            SetMultitaskingControl();
        }

        private void DetectFontName()
        {
            var rawVersion = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            var currentVersion = new Version((int)((rawVersion & 0xFFFF000000000000) >> 48), (int)((rawVersion & 0x0000FFFF00000000) >> 32), (int)((rawVersion & 0x00000000FFFF0000) >> 16), (int)(rawVersion & 0x000000000000FFFF));
            var newIconsMinVersion = new Version(10, 0, 21327, 1000);
            bool isRunningNewIconsVersion = currentVersion >= newIconsMinVersion;

            if (isRunningNewIconsVersion)
            {
                FontName = new FontFamily("Segoe Fluent Icons");
            }
            else
            {
                FontName = new FontFamily("Segoe MDL2 Assets");
            }
        }
    }
}