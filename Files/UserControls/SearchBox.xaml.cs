using Files.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SearchBox : UserControl
    {
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

        private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e) => SearchBoxViewModel.SearchRegion_TextChanged(sender, e);

        private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e) => SearchBoxViewModel.SearchRegion_QuerySubmitted(sender, e);

        private void SearchRegion_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e) => SearchBoxViewModel.SearchRegion_SuggestionChosen(sender, e);

        private void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) => SearchBoxViewModel.SearchRegion_Escaped(sender, e);
    }
}