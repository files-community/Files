using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.DataModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.System;

namespace Files.App.ViewModels.UserControls
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

		public event TypedEventHandler<ISearchBox, SearchBoxTextChangedEventArgs>? TextChanged;

		public event TypedEventHandler<ISearchBox, SearchBoxQuerySubmittedEventArgs>? QuerySubmitted;

		public event EventHandler<ISearchBox>? Escaped;

		private readonly SuggestionComparer suggestionComparer = new SuggestionComparer();

		public ObservableCollection<SuggestionModel> Suggestions { get; } = new ObservableCollection<SuggestionModel>();

		private readonly List<SuggestionModel> oldQueries = new List<SuggestionModel>();

		public void ClearSuggestions()
		{
			Suggestions.Clear();
		}

		public void SetSuggestions(IEnumerable<SuggestionModel> suggestions)
		{
			ClearSuggestions();

			var items = suggestions.OrderBy(suggestion => suggestion, suggestionComparer).ToList();

			var oldSuggestions = Suggestions.Except(items, suggestionComparer).ToList();
			foreach (var oldSuggestion in oldSuggestions)
				Suggestions.Remove(oldSuggestion);

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
			if (e.ChosenSuggestion is null && string.IsNullOrWhiteSpace(e.QueryText))
				return;

			WasQuerySubmitted = true;

			if (e.ChosenSuggestion is SuggestionModel chosen && chosen.ItemPath is null)
			{
				Query = chosen.Name;
				QuerySubmitted?.Invoke(this, new SearchBoxQuerySubmittedEventArgs(null));
			}
			else
			{
				QuerySubmitted?.Invoke(this, new SearchBoxQuerySubmittedEventArgs((SuggestionModel)e.ChosenSuggestion));
			}

			if (!string.IsNullOrWhiteSpace(e.QueryText))
			{
				// If the element is already contained, update its position
				if (oldQueries.FirstOrDefault(suggestion => suggestion.Name == e.QueryText) is SuggestionModel old)
					oldQueries.Remove(old);

				oldQueries.Insert(0, new SuggestionModel(e.QueryText, true));

				// Limit to last 5 queries to improve performance
				if (oldQueries.Count > 5)
					oldQueries.RemoveAt(5);
			}
		}

		public void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
		{
			Escaped?.Invoke(this, this);
		}

		public void SearchRegion_GotFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(query))
				AddRecentQueries();
		}

		public void SearchRegion_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			e.Handled =
				e.Key is VirtualKey.Left ||
				e.Key is VirtualKey.Right ||
				(e.Key is VirtualKey.Up || e.Key is VirtualKey.Down) && Suggestions.Count == 0;
		}

		public void AddRecentQueries()
		{
			ClearSuggestions();
			oldQueries.ForEach(query => Suggestions.Add(query));
		}

		public class SuggestionComparer : IEqualityComparer<SuggestionModel>, IComparer<SuggestionModel>
		{
			public int Compare(SuggestionModel x, SuggestionModel y)
				=> y.ItemPath.CompareTo(x.ItemPath);

			public bool Equals(SuggestionModel x, SuggestionModel y)
				=> y.ItemPath.Equals(x.ItemPath);

			public int GetHashCode(SuggestionModel o)
				=> o.ItemPath.GetHashCode();
		}
	}
}
