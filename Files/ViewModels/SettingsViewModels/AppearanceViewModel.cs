using Files.Enums;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Files.ViewModels.SettingsViewModels
{
    public class AppearanceViewModel : ObservableObject
    {
        private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
        private int selectedDateFormatIndex = (int)Enum.Parse(typeof(TimeStyle), App.AppSettings.DisplayedTimeStyle.ToString());
        private bool isAcrylicDisabled = App.AppSettings.IsAcrylicDisabled;
        private bool showAllContextMenuItems = App.AppSettings.ShowAllContextMenuItems;
        private bool showCopyLocationMenuItem = App.AppSettings.ShowCopyLocationMenuItem;
        private bool showOpenInNewTabMenuItem = App.AppSettings.ShowOpenInNewTabMenuItem;

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

        public List<string> Themes { get; set; }

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

        public List<string> DateFormats { get; set; }

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

        public bool ShowAllContextMenuItems
        {
            get
            {
                return showAllContextMenuItems;
            }
            set
            {
                if (SetProperty(ref showAllContextMenuItems, value))
                {
                    App.AppSettings.ShowAllContextMenuItems = value;
                }
            }
        }

        public bool ShowCopyLocationMenuItem
        {
            get
            {
                return showCopyLocationMenuItem;
            }
            set
            {
                if (SetProperty(ref showCopyLocationMenuItem, value))
                {
                    App.AppSettings.ShowCopyLocationMenuItem = value;
                }
            }
        }

        public bool ShowOpenInNewTabMenuItem
        {
            get
            {
                return showOpenInNewTabMenuItem;
            }
            set
            {
                if (SetProperty(ref showOpenInNewTabMenuItem, value))
                {
                    App.AppSettings.ShowOpenInNewTabMenuItem = value;
                }
            }
        }
    }
}