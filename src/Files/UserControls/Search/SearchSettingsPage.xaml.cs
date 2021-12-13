using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class SearchSettingsPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(ISearchSettingsViewModel), typeof(SearchSettingsPage), new PropertyMetadata(null));

        public ISearchSettingsViewModel ViewModel
        {
            get => (ISearchSettingsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public SearchSettingsPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = e.Parameter as ISearchSettingsViewModel;
        }
    }
}
