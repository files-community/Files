// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class SearchBoxQuerySubmittedEventArgs
	{
		public SuggestionModel ChosenSuggestion { get; }

		public SearchBoxQuerySubmittedEventArgs(SuggestionModel chosenSuggestion)
			=> ChosenSuggestion = chosenSuggestion;
	}
}
