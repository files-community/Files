using Files.Filesystem.Search;
using Files.UserControls.Search;
using Files.ViewModels;
using Files.ViewModels.Search;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SearchBox : UserControl
    {
        private readonly SearchNavigator navigator = new SearchNavigator();

        public static readonly DependencyProperty SearchBoxViewModelProperty =
            DependencyProperty.Register(nameof(SearchBoxViewModel), typeof(SearchBoxViewModel), typeof(SearchBox), new PropertyMetadata(null));

        public SearchBoxViewModel SearchBoxViewModel
        {
            get => (SearchBoxViewModel)GetValue(SearchBoxViewModelProperty);
            set => SetValue(SearchBoxViewModelProperty, value);
        }

        public SearchBox() => InitializeComponent();

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
            navigator.SearchBox = SearchBoxViewModel;
            navigator.Frame = sender as Frame;
            GoRootPage();
        }
        private void MenuButton_Loaded(object sender, RoutedEventArgs e)
        {
            var allowFocusOnInteractionAvailable = ApiInformation.IsPropertyPresent("Windows.UI.Xaml.FrameworkElement", "AllowFocusOnInteraction");
            if (allowFocusOnInteractionAvailable && sender is FrameworkElement element)
            {
                element.AllowFocusOnInteraction = true;
            }
        }
        private void MenuFlyout_Opened(object sender, object e) => GoRootPage();
        private void MenuFlyout_Closed(object sender, object e) => navigator.GoPage(null);

        private void GoRootPage()
        {
            ISearchSettings settings = Ioc.Default.GetService<ISearchSettings>();
            ISearchPageContext context = new SearchPageContext(navigator, settings.Filter);
            ISearchSettingsViewModel viewModel = new SearchSettingsViewModel(context, settings);
            navigator.GoPage(viewModel);
        }
    }
}