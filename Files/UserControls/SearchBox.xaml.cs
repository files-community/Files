using Files.UserControls.Search;
using Files.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SearchBox : UserControl
    {
        private readonly INavigator navigator = Navigator.Instance;

        public SearchBoxViewModel SearchBoxViewModel
        {
            get => (SearchBoxViewModel)GetValue(SearchBoxViewModelProperty);
            set => SetValue(SearchBoxViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for SearchBoxViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchBoxViewModelProperty =
            DependencyProperty.Register(nameof(SearchBoxViewModel), typeof(SearchBoxViewModel), typeof(SearchBox), new PropertyMetadata(null));

        public SearchBox()
        {
            InitializeComponent();
        }

        private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
            => SearchBoxViewModel.SearchRegion_TextChanged(sender, e);
        private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
            => SearchBoxViewModel.SearchRegion_QuerySubmitted(sender, e);
        private void SearchRegion_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e)
            => SearchBoxViewModel.SearchRegion_SuggestionChosen(sender, e);
        private void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
            => SearchBoxViewModel.SearchRegion_Escaped(sender, e);

        private void MenuFrame_Loaded(object sender, RoutedEventArgs e)
        {
            Navigator.Instance.Frame = sender as Frame;
            navigator.GoRoot();
        }

        private void MenuButton_Loaded(object sender, RoutedEventArgs e)
        {
            var allowFocusOnInteractionAvailable =
                Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(
                "Windows.UI.Xaml.FrameworkElement",
                "AllowFocusOnInteraction");

            if (allowFocusOnInteractionAvailable)
            {
                if (sender is FrameworkElement s)
                {
                    s.AllowFocusOnInteraction = true;
                }
            }
        }

        private void Flyout_Opened(object sender, object e) => navigator.GoRoot();
        private void Flyout_Closed(object sender, object e) => navigator.Clear();
    }
}
