using Files.Enums;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.ViewModels.SettingsViewModels
{
    public class AppearanceViewModel : ObservableObject
    {
        private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
        private bool isAcrylicDisabled = App.AppSettings.IsAcrylicDisabled;
        private bool moveOverflowMenuItemsToSubMenu = App.AppSettings.MoveOverflowMenuItemsToSubMenu;
        private AppSkin selectedSkin = App.AppSettings.SelectedSkin;
        private bool showRestartControl = false;
        public AppearanceViewModel()
        {
            Themes = new List<string>()
            {
                "SystemTheme".GetLocalized(),
                "LightTheme".GetLocalized(),
                "DarkTheme".GetLocalized()
            };
        }

        public List<string> Themes { get; set; }
        public List<AppSkin> CustomSkins => App.ExternalResourcesHelper.Skins;

        public int SelectedThemeIndex
        {
            get => selectedThemeIndex;
            set
            {
                if (SetProperty(ref selectedThemeIndex, value))
                {
                    ThemeHelper.RootTheme = (ElementTheme)value;
                }
            }
        }

        public bool IsAcrylicDisabled
        {
            get
            {
                return isAcrylicDisabled;
            }
            set
            {
                if (SetProperty(ref isAcrylicDisabled, value))
                {
                    App.AppSettings.IsAcrylicDisabled = value;
                }
            }
        }

        public bool MoveOverflowMenuItemsToSubMenu
        {
            get
            {
                return moveOverflowMenuItemsToSubMenu;
            }
            set
            {
                if (SetProperty(ref moveOverflowMenuItemsToSubMenu, value))
                {
                    App.AppSettings.MoveOverflowMenuItemsToSubMenu = value;
                }
            }
        }

        public AppSkin SelectedSkin
        {
            get
            {
                return selectedSkin;
            }
            set
            {
                if (SetProperty(ref selectedSkin, value))
                {
                    // Remove the old resource file and load the new file
                    App.ExternalResourcesHelper.UpdateSkin(App.AppSettings.SelectedSkin, selectedSkin);

                    App.AppSettings.SelectedSkin = selectedSkin;

                    // Force the application to use the correct resource file
                    UpdateTheme();
                }
            }
        }

        /// <summary>
        /// Forces the application to use the correct resource styles
        /// </summary>
        private async void UpdateTheme()
        {
            // Allow time to remove the old theme
            await Task.Delay(250);

            // Get the index of the current theme
            var selTheme = SelectedThemeIndex;

            // Toggle between the themes to force the controls to use the new resource styles
            SelectedThemeIndex = 0;
            SelectedThemeIndex = 1;
            SelectedThemeIndex = 2;

            // Restore the theme to the correct theme
            SelectedThemeIndex = selTheme;
        }
    }
}