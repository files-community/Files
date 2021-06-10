using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SearchBox : UserControl, ISearchBox
    {
        public event TypedEventHandler<ISearchBox, SearchBoxTextChangedEventArgs> TextChanged;
        public event TypedEventHandler<ISearchBox, SearchBoxSuggestionChosenEventArgs> SuggestionChosen;
        public event TypedEventHandler<ISearchBox, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;
        public event EventHandler<ISearchBox> Escaped;

        public static readonly DependencyProperty QueryProperty =
            DependencyProperty.Register("Query", typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

        public string Query
        {
            get => (string)GetValue(QueryProperty);
            set => SetValue(QueryProperty, value ?? string.Empty);
        }

        private readonly SuggestionComparer suggestionComparer = new SuggestionComparer();

        private ObservableCollection<ListedItem> Suggestions { get; } = new ObservableCollection<ListedItem>();

        public SearchBox()
        {
            InitializeComponent();
        }

        public void ClearSuggestions()
        {
            Suggestions.Clear();
        }
        public void SetSuggestions(IEnumerable<ListedItem> suggestions)
        {
            var items = suggestions.OrderBy(suggestion => suggestion, suggestionComparer).ToList();

            var oldSuggestions = Suggestions.Except(items, suggestionComparer).ToList();
            foreach (var oldSuggestion in oldSuggestions)
            {
                Suggestions.Remove(oldSuggestion);
            }

            var newSuggestions = items.Except(Suggestions, suggestionComparer).ToList();
            foreach (var newSuggestion in newSuggestions)
            {
                var indexSuggestion = Suggestions.FirstOrDefault(suggestion => suggestionComparer.Compare(suggestion, newSuggestion) < 1);
                if (!(indexSuggestion is null))
                {
                    int index = Suggestions.IndexOf(indexSuggestion);
                    Suggestions.Insert(index, newSuggestion);
                }
                else
                {
                    Suggestions.Add(newSuggestion);
                }
            }
        }

        private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            TextChanged?.Invoke(this, new SearchBoxTextChangedEventArgs(e.Reason));
        }
        private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            if (e.ChosenSuggestion is ListedItem listedItem)
            {
                QuerySubmitted?.Invoke(this, new SearchBoxQuerySubmittedEventArgs(listedItem));
            }
        }
        private void SearchRegion_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e)
        {
            if (e.SelectedItem is ListedItem listedItem)
            {
                SuggestionChosen?.Invoke(this, new SearchBoxSuggestionChosenEventArgs(listedItem));
            }
        }
        private void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            Escaped?.Invoke(this, this);
        }

        private class SuggestionComparer : IEqualityComparer<ListedItem>, IComparer<ListedItem>
        {
            public int Compare(ListedItem x, ListedItem y) => y.ItemPath.CompareTo(x.ItemPath);
            public bool Equals(ListedItem x, ListedItem y) => y.ItemPath.Equals(x.ItemPath);
            public int GetHashCode(ListedItem o) => o.ItemPath.GetHashCode();
        }
    }
}
