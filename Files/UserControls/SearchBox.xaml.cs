using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class SearchBox : UserControl, ISearchBox
    {
        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> SearchTextChanged;
        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmitted;
        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SearchSuggestionChosen;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value ?? string.Empty);
        }

        public SearchBox()
        {
            InitializeComponent();
        }

        private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            SearchTextChanged?.Invoke(sender, e);
        }
        private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            SearchQuerySubmitted?.Invoke(sender, e);
        }
        private void SearchRegion_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e)
        {
            SearchSuggestionChosen?.Invoke(sender, e);
            //Visibility = Visibility.Collapsed;
        }
    }
}
