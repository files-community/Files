using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface ISearchBox
    {
        event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> QueryChanged;
        event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;
        event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;

        event EventHandler<AutoSuggestBox> Escaped;

        string Query { get; set; }
    }
}
