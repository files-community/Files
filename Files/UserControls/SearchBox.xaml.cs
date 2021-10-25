﻿using Files.Filesystem.Search;
using Files.UserControls.Search;
using Files.ViewModels;
using Files.ViewModels.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SearchBox : UserControl
    {
        private readonly Navigator navigator = new Navigator();

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
            navigator.Frame = sender as Frame;
            GoRootPage();
        }

        private void MenuButton_Loaded(object sender, RoutedEventArgs e)
        {
            var allowFocusOnInteractionAvailable =
                Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(
                "Windows.UI.Xaml.FrameworkElement",
                "AllowFocusOnInteraction");

            if (allowFocusOnInteractionAvailable && sender is FrameworkElement s)
            {
                s.AllowFocusOnInteraction = true;
            }
        }

        private void Flyout_Opened(object sender, object e) => GoRootPage();
        private void Flyout_Closed(object sender, object e) => navigator.Clear();

        private void GoRootPage()
        {
            ISettings settings = Filesystem.Search.Settings.Instance;
            var context = new SearchPageContext(navigator, settings.Filter);
            var viewModel = new ViewModels.Search.SettingsViewModel(context, settings);
            navigator.GoPage(viewModel);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
