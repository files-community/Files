// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Foundation;

namespace Files.App.Data.Contracts
{
	public interface ISearchBoxViewModel
	{
		event TypedEventHandler<ISearchBoxViewModel, SearchBoxTextChangedEventArgs> TextChanged;

		event TypedEventHandler<ISearchBoxViewModel, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;

		event EventHandler<ISearchBoxViewModel> Escaped;

		bool WasQuerySubmitted { get; set; }

		string Query { get; set; }

		void ClearSuggestions();

		void SetSuggestions(IEnumerable<SuggestionModel> suggestions);

		void AddRecentQueries();
	}
}
