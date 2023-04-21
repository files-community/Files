using Microsoft.UI.Xaml.Controls;
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

	public class SearchBoxTextChangedEventArgs
	{
		public SearchBoxTextChangeReason Reason { get; }

		public SearchBoxTextChangedEventArgs(SearchBoxTextChangeReason reason)
			=> Reason = reason;

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
		public SuggestionModel ChosenSuggestion { get; }

		public SearchBoxQuerySubmittedEventArgs(SuggestionModel chosenSuggestion)
			=> ChosenSuggestion = chosenSuggestion;
	}

	public enum SearchBoxTextChangeReason : ushort
	{
		UserInput,
		ProgrammaticChange,
		SuggestionChosen,
	}
}
