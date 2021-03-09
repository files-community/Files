using Files.Filesystem;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Preferences : Page
    {
        public static LibraryManager LibraryManager { get; private set; }

        public Preferences()
        {
            LibraryManager ??= new LibraryManager();

            InitializeComponent();
        }

        private async void ShowLibrarySection_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (App.AppSettings.ShowLibrarySection)
            {
                await LibraryManager.EnumerateDrivesAsync();
            }
            else
            {
                await LibraryManager.RemoveEnumerateDrivesAsync();
            }
        }
    }
}