using Files.ViewModels;
using Files.ViewModels.SettingsViewModels;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Multitasking : Page
    {
        public MultitaskingViewModel ViewModel { get; } = new MultitaskingViewModel();
        public MainViewModel MainViewModel => App.MainViewModel;

        public Multitasking()
        {
            InitializeComponent();
        }

        private void HorizontalMultToggle_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Setup the correct multitasking control
            MainViewModel.SetMultitaskingControl();
        }
    }
}