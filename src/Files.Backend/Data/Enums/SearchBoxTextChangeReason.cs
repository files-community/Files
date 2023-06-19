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
		/// The SearchBox has been changed text manually.
		/// </summary>
		UserInput,

		/// <summary>
		/// The SearchBox has been changed text programmatically.
		/// </summary>
		ProgrammaticChange,

		/// <summary>
		/// The user has chosen a suggestion.
		/// </summary>
		SuggestionChosen
	}
}
