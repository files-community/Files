using Files.ViewModels.SettingsViewModels;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class About : Page
    {
        public AboutViewModel ViewModel
        {
            get => (AboutViewModel)DataContext;
            set => DataContext = value;
        }

        public About()
        {
            InitializeComponent();

            this.ViewModel = new AboutViewModel();
        }
    }
}