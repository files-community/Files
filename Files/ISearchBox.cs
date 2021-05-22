using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface ISearchBox
    {
        event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> SearchTextChanged;
        event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmitted;
        event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SearchSuggestionChosen;
    }
}
