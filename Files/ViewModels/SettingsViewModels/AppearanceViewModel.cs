using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.ViewModels.SettingsViewModels
{
    public class AppearanceViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
        private AppTheme selectedTheme = App.AppSettings.SelectedTheme;

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
        public List<AppTheme> CustomThemes => App.ExternalResourcesHelper.Themes;

        public int SelectedThemeIndex
        {
            get => selectedThemeIndex;
            set
            {
                if (SetProperty(ref selectedThemeIndex, value))
                {
                    ThemeHelper.RootTheme = (ElementTheme)value;
                    OnPropertyChanged(nameof(SelectedElementTheme));
                }
            }
        }

        public ElementTheme SelectedElementTheme
        {
            get => (ElementTheme)selectedThemeIndex;
        }

        public bool MoveOverflowMenuItemsToSubMenu
        {
            get => UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
                {
                    UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = value;
                    OnPropertyChanged();
                }
            }
        }

        public AppTheme SelectedTheme
        {
            get
            {
                return selectedTheme;
            }
            set
            {
                if (SetProperty(ref selectedTheme, value))
                {
                    if (selectedTheme != null)
                    {
                        // Remove the old resource file and load the new file
                        App.ExternalResourcesHelper.UpdateTheme(App.AppSettings.SelectedTheme, selectedTheme);

                        App.AppSettings.SelectedTheme = selectedTheme;

                        // Force the application to use the correct resource file
                        UpdateTheme();
                    }
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