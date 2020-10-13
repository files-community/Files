using Files.View_Models;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.SettingsPages
{
    public sealed partial class Multitasking : Page
    {
        private SettingsViewModel AppSettings => App.AppSettings;

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