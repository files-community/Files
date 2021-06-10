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

        private void SkinsLearnMoreButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SkinsTeachingTip.IsOpen = true;
        }

        private void OpenSkinsFolderButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.FindAscendant<SettingsDialog>()?.Hide();
            SettingsViewModel.OpenSkinsFolder();
        }
    }
}