using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels
{
    public class InteractionViewModel : ObservableObject
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public InteractionViewModel()
        {
            Window.Current.SizeChanged += Current_SizeChanged;

            SetMultitaskingControl();
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            IsWindowCompactSize = IsWindowResizedToCompactWidth();

            // Setup the correct multitasking control
            SetMultitaskingControl();
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

        private int tabStripSelectedIndex = 0;

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
                    if (value < MainPage.MultitaskingControl.Items.Count)
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        var mainView = rootFrame.Content as MainPage;
                        mainView.SelectedTabItem = MainPage.MultitaskingControl.Items[value];
                    }
                }
            }
        }

        private bool isPasteEnabled = false;

        public bool IsPasteEnabled
        {
            get => isPasteEnabled;
            set => SetProperty(ref isPasteEnabled, value);
        }

        private bool isHorizontalTabStripVisible = false;

        public bool IsHorizontalTabStripVisible
        {
            get => isHorizontalTabStripVisible;
            set => SetProperty(ref isHorizontalTabStripVisible, value);
        }

        private bool isVerticalTabFlyoutVisible = false;

        public bool IsVerticalTabFlyoutVisible
        {
            get => isVerticalTabFlyoutVisible;
            set => SetProperty(ref isVerticalTabFlyoutVisible, value);
        }

        private bool isWindowCompactSize = IsWindowResizedToCompactWidth();

        public static bool IsWindowResizedToCompactWidth()
        {
            return Window.Current.Bounds.Width <= 750;
        }

        public bool IsWindowCompactSize
        {
            get => isWindowCompactSize;
            set
            {
                SetProperty(ref isWindowCompactSize, value);
            }
        }

        private bool multiselectEnabled;
        public bool MultiselectEnabled
        {
            get => multiselectEnabled;
            set => SetProperty(ref multiselectEnabled, value);
        }
    }
}