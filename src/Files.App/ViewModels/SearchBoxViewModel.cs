using Files.App.Filesystem;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.ViewModels
{
    public class SearchBoxViewModel : ObservableObject, ISearchBox
    {
        private string query;
        public string Query
        {
            get => query;
            set => SetProperty(ref query, value);
        }

        public bool WasQuerySubmitted { get; set; } = false;

        public event TypedEventHandler<ISearchBox, SearchBoxTextChangedEventArgs> TextChanged;
        public event TypedEventHandler<ISearchBox, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;
        public event EventHandler<ISearchBox> Escaped;

        private readonly SuggestionComparer suggestionComparer = new SuggestionComparer();

        public ObservableCollection<ListedItem> Suggestions { get; } = new ObservableCollection<ListedItem>();
        private readonly List<ListedItem> oldQueries = new List<ListedItem>();

        public void ClearSuggestions()
        {
            Suggestions.Clear();
        }

        public void SetSuggestions(IEnumerable<ListedItem> suggestions)
        {
            ClearSuggestions();

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

        public void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            TextChanged?.Invoke(this, new SearchBoxTextChangedEventArgs(e.Reason));
        }

        public void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            WasQuerySubmitted = true;
            if (e.ChosenSuggestion is ListedItem chosen && chosen.ItemPath is null)
            {
                Query = chosen.ItemNameRaw;
                QuerySubmitted?.Invoke(this, new SearchBoxQuerySubmittedEventArgs(null));
            }
            else
            {
                QuerySubmitted?.Invoke(this, new SearchBoxQuerySubmittedEventArgs(e.ChosenSuggestion as ListedItem));
            }

            if (!string.IsNullOrWhiteSpace(e.QueryText))
            {
                // If the element is already contained, update its position
                oldQueries.Remove(oldQueries.FirstOrDefault(suggestion => suggestion.ItemNameRaw == e.QueryText));

                oldQueries.Insert(0, new ListedItem
                {
                    ItemNameRaw = e.QueryText
                });

                // Limit to last 5 queries to improve performance
                if (oldQueries.Count > 5)
                {
                    oldQueries.RemoveAt(5);
                }
            }
        }

        public void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            Escaped?.Invoke(this, this);
        }

        public void SearchRegion_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                AddRecentQueries();
            }
        }

        public void AddRecentQueries()
        {
            ClearSuggestions();
            oldQueries.ForEach(query => Suggestions.Add(query));
        }

        public class SuggestionComparer : IEqualityComparer<ListedItem>, IComparer<ListedItem>
        {
            public int Compare(ListedItem x, ListedItem y) => y.ItemPath.CompareTo(x.ItemPath);

            public bool Equals(ListedItem x, ListedItem y) => y.ItemPath.Equals(x.ItemPath);

            public int GetHashCode(ListedItem o) => o.ItemPath.GetHashCode();
        }
    }
}