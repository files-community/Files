using Files.Dialogs;
using Files.Helpers;
using Files.UserControls.Settings;
using Files.ViewModels;
using Files.ViewModels.SettingsViewModels;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Files.ViewModels.SettingsViewModels.AppearanceViewModel;

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
                if(ViewModel.CustomThemes[i].Path == ViewModel.SelectedTheme.Path)
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
            this.FindAscendant<SettingsDialog>()?.Hide();
            SettingsViewModel.OpenThemesFolder();
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ViewModel.SelectedElementTheme))
            {
                var containers = AppThemeSelectionGridView.ItemsPanelRoot.Children;
                foreach(var container in containers)
                {
                    var listViewItemPresenter = VisualTreeHelper.GetChild(container, 0);
                    var item = VisualTreeHelper.GetChild(listViewItemPresenter, 0) as ThemeSampleDisplayControl;
                    if(item !=  null)
                    {
                        await item.ReevaluateThemeResourceBinding();
                    }
                }
            }
        }
    }
}