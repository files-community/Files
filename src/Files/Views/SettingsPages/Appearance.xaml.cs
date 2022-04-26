using Files.Dialogs;
using Files.Helpers.XamlHelpers;
using Files.UserControls.Settings;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Appearance : Page
    {
        public Appearance()
        {
            InitializeComponent();
            Loaded += Appearance_Loaded;
        }

        private void Appearance_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < ViewModel.CustomThemes.Count; i++)
            {
                if (ViewModel.CustomThemes[i].Path == ViewModel.SelectedTheme.Path)
                {
                    AppThemeSelectionGridView.SelectedIndex = i;
                }
            }
        }

        private void ThemesLearnMoreButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ThemesTeachingTip.IsOpen = true;
        }

        private void OpenThemesFolderButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ThemesTeachingTip.IsOpen = false;
            this.FindAscendant<SettingsDialog>()?.Hide();
            SettingsViewModel.OpenThemesFolder();
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedElementTheme))
            {
                foreach (var theme in ViewModel.CustomThemes)
                {
                    var container = AppThemeSelectionGridView.ContainerFromItem(theme);
                    if (container != null)
                    {
                        var item = DependencyObjectHelpers.FindChild<ThemeSampleDisplayControl>(container);
                        if (item != null)
                        {
                            await item.ReevaluateThemeResourceBinding();
                        }
                    }
                }
            }
        }
    }
}