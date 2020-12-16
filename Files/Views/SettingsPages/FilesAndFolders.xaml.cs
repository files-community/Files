using Files.View_Models;
using Files.Views;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class FilesAndFolders : Page
    {
        public SettingsViewModel AppSettings => MainPage.AppSettings;

        public FilesAndFolders()
        {
            InitializeComponent();
        }
    }
}