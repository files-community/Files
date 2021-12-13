using Files.Filesystem;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface ISearchBox
    {
        event TypedEventHandler<ISearchBox, SearchBoxTextChangedEventArgs> TextChanged;
        event TypedEventHandler<ISearchBox, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;
        event EventHandler<ISearchBox> Escaped;

        string Query { get; set; }

        void ClearSuggestions();

        void SetSuggestions(IEnumerable<ListedItem> suggestions);
    }

    public class SearchBoxTextChangedEventArgs
    {
        public SearchBoxTextChangeReason Reason { get; }

        public SearchBoxTextChangedEventArgs(SearchBoxTextChangeReason reason) => Reason = reason;

        public SearchBoxTextChangedEventArgs(AutoSuggestionBoxTextChangeReason reason)
        {
            Reason = reason switch
            {
                AutoSuggestionBoxTextChangeReason.UserInput => SearchBoxTextChangeReason.UserInput,
                AutoSuggestionBoxTextChangeReason.SuggestionChosen => SearchBoxTextChangeReason.SuggestionChosen,
                _ => SearchBoxTextChangeReason.ProgrammaticChange
            };
        }
    }

    public class SearchBoxQuerySubmittedEventArgs
    {
        public ListedItem ChosenSuggestion { get; }

        public SearchBoxQuerySubmittedEventArgs(ListedItem chosenSuggestion) => ChosenSuggestion = chosenSuggestion;
    }

    public enum SearchBoxTextChangeReason : ushort
    {
        UserInput,
        ProgrammaticChange,
        SuggestionChosen,
    }
}