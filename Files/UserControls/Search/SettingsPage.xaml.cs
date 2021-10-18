using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class SettingsPage : Page
    {
        //public static readonly DependencyProperty ViewModelProperty =
        //    DependencyProperty.Register(nameof(ViewModel), typeof(ISettingsViewModel), typeof(SettingsPage), new PropertyMetadata(null));

        //public ISettingsViewModel ViewModel
        //{
        //    get => (ISettingsViewModel)GetValue(ViewModelProperty);
        //    set => SetValue(ViewModelProperty, value);
        //}

        public SettingsPage() => InitializeComponent();

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);
        //    ViewModel = e.Parameter as ISettingsViewModel;
        //}
    }
}
