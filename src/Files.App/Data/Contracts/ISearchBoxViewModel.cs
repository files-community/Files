// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Foundation;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract of AddressToolbar view model.
	/// </summary>
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
