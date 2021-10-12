using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.UserControls.Search
{
    public sealed partial class FilterPage : Page
    {
        private readonly INavigator navigator = Navigator.Instance;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(IFilterPageViewModel), typeof(FilterPage), new PropertyMetadata(null));

        public IFilterPageViewModel ViewModel
        {
            get => (IFilterPageViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public FilterPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = e.Parameter as IFilterPageViewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = ViewModel?.Parent?.Filter;
            var filter = ViewModel?.Filter?.Filter;

            if (parent is not null && filter is not null)
            {
                if (!filter.IsEmpty)
                {
                    parent.Set(filter);
                }
                else
                {
                    parent.Unset(filter);
                }
            }

            navigator.GoBack();
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e) => navigator.GoBack();
        private void CancelButton_Click(object sender, RoutedEventArgs e) => navigator.GoBack();
    }
}
