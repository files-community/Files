using Files.Dialogs;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Appearance : Page
    {
        public Appearance()
        {
            InitializeComponent();
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
    }
}