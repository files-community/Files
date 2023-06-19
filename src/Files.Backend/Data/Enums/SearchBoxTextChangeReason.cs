// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Backend.Data.Enums
{
	/// <summary>
	/// Defines constants that specify search box text changed reason.
	/// </summary>
	public enum SearchBoxTextChangeReason : ushort
	{
		/// <summary>
		/// The SearchBox text was manually changed.
		/// </summary>
		UserInput,

		/// <summary>
		/// The SearchBox text has been changed programmatically.
		/// </summary>
		ProgrammaticChange,

		/// <summary>
		/// The user has chosen a suggestion.
		/// </summary>
		SuggestionChosen
	}
}
