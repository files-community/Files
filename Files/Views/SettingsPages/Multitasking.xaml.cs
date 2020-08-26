using Files.DataModels;
using Files.View_Models;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.SettingsPages
{
    public sealed partial class Multitasking : Page
    {
        private SettingsViewModel AppSettings => App.AppSettings;

        public Multitasking()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void AdaptiveMultToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (AdaptiveMultToggle.IsChecked == true)
            {
                AppSettings.IsHorizontalTabStripEnabled = false;
                AppSettings.IsVerticalTabFlyoutEnabled = false;
                AppSettings.IsMultitaskingExperienceAdaptive = true;
            }
            else
            {
                AppSettings.IsMultitaskingExperienceAdaptive = false;
            }
        }

        private void VerticalMultToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (VerticalMultToggle.IsChecked == true)
            {
                AppSettings.IsHorizontalTabStripEnabled = false;
                AppSettings.IsVerticalTabFlyoutEnabled = true;
                AppSettings.IsMultitaskingExperienceAdaptive = false;
            }
            else
            {
                AppSettings.IsVerticalTabFlyoutEnabled = false;
            }
        }

        private void HorizontalMultToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (HorizontalMultToggle.IsChecked == true)
            {
                AppSettings.IsHorizontalTabStripEnabled = true;
                AppSettings.IsVerticalTabFlyoutEnabled = false;
                AppSettings.IsMultitaskingExperienceAdaptive = false;
            }
            else
            {
                AppSettings.IsHorizontalTabStripEnabled = false;
            }
        }
    }
}