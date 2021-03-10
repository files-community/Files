using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Preferences : Page
    {
        public Preferences()
        {

            InitializeComponent();
        }

        private async void ShowLibrarySection_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            App.AppSettings.ShowLibrarySection = ShowLibrarySection.IsOn;
        }
    }
}