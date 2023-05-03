// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Foundation;

namespace Files.App
{
	public interface ISearchBox
	{
		event TypedEventHandler<ISearchBox, SearchBoxTextChangedEventArgs> TextChanged;

		event TypedEventHandler<ISearchBox, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;

		event EventHandler<ISearchBox> Escaped;

		bool WasQuerySubmitted { get; set; }

		string Query { get; set; }

		void ClearSuggestions();

		void SetSuggestions(IEnumerable<SuggestionModel> suggestions);

		void AddRecentQueries();
	}
}
