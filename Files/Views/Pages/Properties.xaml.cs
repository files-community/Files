using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Files.View_Models.Properties;
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static AppWindowTitleBar TitleBar;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public AppWindow propWindow;

        public SettingsViewModel AppSettings => App.AppSettings;

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public BaseProperties BaseProperties { get; set; }

        public Properties()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new SelectedItemsPropertiesViewModel();

            if (e.Parameter is ListedItem)
            {
                var listedItem = e.Parameter as ListedItem;
                if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    BaseProperties = new FileProperties(ViewModel, tokenSource, ItemMD5HashProgress, listedItem);
                }
                else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    BaseProperties = new FolderProperties(ViewModel, tokenSource, Dispatcher, listedItem);
                }
            }
            else if (e.Parameter is List<ListedItem>)
            {
                BaseProperties = new CombinedProperties(ViewModel, tokenSource, Dispatcher, e.Parameter as List<ListedItem>);
            }
            else if (e.Parameter is DriveItem)
            {
                BaseProperties = new DriveProperties(ViewModel, e.Parameter as DriveItem);
            }

            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            base.OnNavigatedTo(e);
        }

        private void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                propWindow = Interaction.AppWindows[UIContext]; // Collect AppWindow-specific info

                TitleBar = propWindow.TitleBar; // Set properties window titleBar style
                TitleBar.ButtonBackgroundColor = Colors.Transparent;
                TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                AppSettings.UpdateThemeElements.Execute(null);
            }

            if (BaseProperties != null)
            {
                BaseProperties.GetSpecialProperties();
            }
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                switch (ThemeHelper.RootTheme)
                {
                    case ElementTheme.Default:
                        TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                        TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                        break;

                    case ElementTheme.Light:
                        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                        TitleBar.ButtonForegroundColor = Colors.Black;
                        break;

                    case ElementTheme.Dark:
                        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                        TitleBar.ButtonForegroundColor = Colors.White;
                        break;
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
            else
            {
                var propertiesDialog = new PropertiesDialog();
                propertiesDialog.Hide();
            }
        }
    }
}