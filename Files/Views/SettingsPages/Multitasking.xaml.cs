using Files.View_Models;
using Files.Views;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.SettingsPages
{
    public sealed partial class Multitasking : Page
    {
        private SettingsViewModel AppSettings => MainPage.AppSettings;

        public Multitasking()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}