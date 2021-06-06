using Files.Filesystem;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Files.ViewModels
{
    public class SearchBoxViewModel : ObservableObject, ISearchBox
    {

        private string query;
        public string Query
        {
            get => query;
            set => SetProperty(ref query, value);
        }

        public event TypedEventHandler<ISearchBox, SearchBoxTextChangedEventArgs> TextChanged;
        public event TypedEventHandler<ISearchBox, SearchBoxSuggestionChosenEventArgs> SuggestionChosen;
        public event TypedEventHandler<ISearchBox, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;
        public event EventHandler<ISearchBox> Escaped;

        private readonly SuggestionComparer suggestionComparer = new SuggestionComparer();

        private ObservableCollection<ListedItem> Suggestions { get; } = new ObservableCollection<ListedItem>();


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

        public class SuggestionComparer : IEqualityComparer<ListedItem>, IComparer<ListedItem>
        {
            public int Compare(ListedItem x, ListedItem y) => y.ItemPath.CompareTo(x.ItemPath);
            public bool Equals(ListedItem x, ListedItem y) => y.ItemPath.Equals(x.ItemPath);
            public int GetHashCode(ListedItem o) => o.ItemPath.GetHashCode();
        }
    }
}
