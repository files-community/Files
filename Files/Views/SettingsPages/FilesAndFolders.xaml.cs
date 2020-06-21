using Files.View_Models;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class FilesAndFolders : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public FilesAndFolders()
        {
            InitializeComponent();
        }
    }
}