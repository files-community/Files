using Files.View_Models;
using Files.Views;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Widgets : Page
    {
        public SettingsViewModel AppSettings => MainPage.AppSettings;

        public Widgets()
        {
            InitializeComponent();
        }
    }
}