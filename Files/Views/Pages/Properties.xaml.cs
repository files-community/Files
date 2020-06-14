﻿using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static AppWindowTitleBar _TitleBar;

        public AppWindow propWindow;
        public SelectedItemsPropertiesViewModel ViewModel { get; } = new SelectedItemsPropertiesViewModel();

        public Properties()
        {
            this.InitializeComponent();
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                Loaded += Properties_Loaded;
            }
            else
            {
                this.OKButton.Visibility = Visibility.Collapsed;
            }
            App.AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                // Collect AppWindow-specific info
                propWindow = Interaction.AppWindows[UIContext];
                // Set properties window titleBar style
                _TitleBar = propWindow.TitleBar;
                _TitleBar.ButtonBackgroundColor = Colors.Transparent;
                _TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                App.AppSettings.UpdateThemeElements.Execute(null);
            }
            await ViewModel.GetPropertiesAsync();
        }
    
        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                switch (ThemeHelper.RootTheme)
                {
                    case ElementTheme.Default:
                        _TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                        _TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                        break;

                    case ElementTheme.Light:
                        _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                        _TitleBar.ButtonForegroundColor = Colors.Black;
                        break;

                    case ElementTheme.Dark:
                        _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                        _TitleBar.ButtonForegroundColor = Colors.White;
                        break;
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            App.AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
            else
            {
                App.PropertiesDialogDisplay.Hide();
            }
        }
    }
}