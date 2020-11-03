using Files.View_Models;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Widgets : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public Widgets()
        {
            InitializeComponent();
        }
    }
}