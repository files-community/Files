// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Models;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Files.App.Data.EventArguments
{
	public class SearchBoxQuerySubmittedEventArgs
	{
		public SuggestionModel ChosenSuggestion { get; }

		public SearchBoxQuerySubmittedEventArgs(SuggestionModel chosenSuggestion)
			=> ChosenSuggestion = chosenSuggestion;
	}
}
