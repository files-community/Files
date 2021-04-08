using Files.Enums;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Files.ViewModels.SettingsViewModels
{
    public class AppearanceViewModel : ObservableObject
    {
        private bool isAcrylicDisabled = App.AppSettings.IsAcrylicDisabled;
        private bool moveOverflowMenuItemsToSubMenu = App.AppSettings.MoveOverflowMenuItemsToSubMenu;
        private int selectedDateFormatIndex = (int)Enum.Parse(typeof(TimeStyle), App.AppSettings.DisplayedTimeStyle.ToString());
        private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
        private string selectedThemeName = App.AppSettings.PathToThemeFile;
        private bool showRestartControl = false;

        public AppearanceViewModel()
        {
            Themes = new List<string>()
            {
                "SystemTheme".GetLocalized(),
                "LightTheme".GetLocalized(),
                "DarkTheme".GetLocalized()
            };

            DateFormats = new List<string>
            {
                "ApplicationTimeStye".GetLocalized(),
                "SystemTimeStye".GetLocalized()
            };
        }

        public List<string> ColorSchemes => App.ExternalResourcesHelper.Themes;
        public List<string> DateFormats { get; set; }

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

        public RelayCommand OpenThemesFolderCommand => new RelayCommand(() => SettingsViewModel.OpenThemesFolder());

        public int SelectedDateFormatIndex
        {
            get
            {
                return selectedDateFormatIndex;
            }
            set
            {
                if (SetProperty(ref selectedDateFormatIndex, value))
                {
                    App.AppSettings.DisplayedTimeStyle = (TimeStyle)value;
                }
            }
        }

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

        public string SelectedThemeName
        {
            get
            {
                return selectedThemeName;
            }
            set
            {
                if (SetProperty(ref selectedThemeName, value))
                {
                    App.AppSettings.PathToThemeFile = selectedThemeName;
                    ShowRestartControl = true;
                }
            }
        }

        public bool ShowRestartControl
        {
            get => showRestartControl;
            set => SetProperty(ref showRestartControl, value);
        }

        public List<string> Themes { get; set; }
    }
}