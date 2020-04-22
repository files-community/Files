using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class StartPageWidgets : Page
    {
        private ApplicationDataContainer localSettings;

        public StartPageWidgets()
        {
            this.InitializeComponent();
            localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["FavoritesDisplayed_Start"] == null)
            {
                localSettings.Values["FavoritesDisplayed_Start"] = true;
                FavoritesCheck.IsChecked = true;
            }
            else if (localSettings.Values["FavoritesDisplayed_Start"] != null)
            {
                switch ((bool)localSettings.Values["FavoritesDisplayed_Start"])
                {
                    case true:
                        FavoritesCheck.IsChecked = true;
                        break;

                    case false:
                        FavoritesCheck.IsChecked = false;
                        break;
                }
            }

            if (localSettings.Values["RecentsDisplayed_Start"] == null)
            {
                localSettings.Values["RecentsDisplayed_Start"] = true;
                RecentsCheck.IsChecked = true;
            }
            else if (localSettings.Values["RecentsDisplayed_Start"] != null)
            {
                switch ((bool)localSettings.Values["RecentsDisplayed_Start"])
                {
                    case true:
                        RecentsCheck.IsChecked = true;
                        break;

                    case false:
                        RecentsCheck.IsChecked = false;
                        break;
                }
            }

            if (localSettings.Values["DrivesDisplayed_Start"] == null)
            {
                localSettings.Values["DrivesDisplayed_Start"] = false;
                DrivesCheck.IsChecked = false;
            }
            else if (localSettings.Values["DrivesDisplayed_Start"] != null)
            {
                switch ((bool)localSettings.Values["DrivesDisplayed_Start"])
                {
                    case true:
                        DrivesCheck.IsChecked = true;
                        break;

                    case false:
                        DrivesCheck.IsChecked = false;
                        break;
                }
            }

            if (localSettings.Values["FavoritesDisplayed_NewTab"] == null)
            {
                localSettings.Values["FavoritesDisplayed_NewTab"] = true;
                FavoritesCheckNewTab.IsChecked = true;
            }
            else if (localSettings.Values["FavoritesDisplayed_NewTab"] != null)
            {
                switch ((bool)localSettings.Values["FavoritesDisplayed_NewTab"])
                {
                    case true:
                        FavoritesCheckNewTab.IsChecked = true;
                        break;

                    case false:
                        FavoritesCheckNewTab.IsChecked = false;
                        break;
                }
            }

            if (localSettings.Values["RecentsDisplayed_NewTab"] == null)
            {
                localSettings.Values["RecentsDisplayed_NewTab"] = true;
                RecentsCheckNewTab.IsChecked = true;
            }
            else if (localSettings.Values["RecentsDisplayed_NewTab"] != null)
            {
                switch ((bool)localSettings.Values["RecentsDisplayed_NewTab"])
                {
                    case true:
                        RecentsCheckNewTab.IsChecked = true;
                        break;

                    case false:
                        RecentsCheckNewTab.IsChecked = false;
                        break;
                }
            }

            if (localSettings.Values["DrivesDisplayed_NewTab"] == null)
            {
                localSettings.Values["DrivesDisplayed_NewTab"] = false;
                DrivesCheckNewTab.IsChecked = false;
            }
            else if (localSettings.Values["DrivesDisplayed_NewTab"] != null)
            {
                switch ((bool)localSettings.Values["DrivesDisplayed_NewTab"])
                {
                    case true:
                        DrivesCheckNewTab.IsChecked = true;
                        break;

                    case false:
                        DrivesCheckNewTab.IsChecked = false;
                        break;
                }
            }
        }

        private void FavoritesCheck_Checked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["FavoritesDisplayed_Start"] = true;
        }

        private void FavoritesCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["FavoritesDisplayed_Start"] = false;
        }

        private void RecentsCheck_Checked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["RecentsDisplayed_Start"] = true;
        }

        private void RecentsCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["RecentsDisplayed_Start"] = false;
        }

        private void DrivesCheck_Checked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["DrivesDisplayed_Start"] = true;
        }

        private void DrivesCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["DrivesDisplayed_Start"] = false;
        }

        private void FavoritesCheckNewTab_Checked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["FavoritesDisplayed_NewTab"] = true;
        }

        private void FavoritesCheckNewTab_Unchecked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["FavoritesDisplayed_NewTab"] = false;
        }

        private void RecentsCheckNewTab_Checked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["RecentsDisplayed_NewTab"] = true;
        }

        private void RecentsCheckNewTab_Unchecked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["DrivesDisplayed_NewTab"] = false;
        }

        private void DrivesCheckNewTab_Checked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["DrivesDisplayed_NewTab"] = true;
        }

        private void DrivesCheckNewTab_Unchecked(object sender, RoutedEventArgs e)
        {
            localSettings.Values["DrivesDisplayed_NewTab"] = false;
        }
    }
}