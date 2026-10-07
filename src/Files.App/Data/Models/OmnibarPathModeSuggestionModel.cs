// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Controls;

namespace Files.App.Data.Models
{
	internal record OmnibarPathModeSuggestionModel(string Path, string DisplayName, string? Description = null, bool IsPlaceholder = false, double NameMinWidth = 0, int MatchStart = 0, int MatchLength = 0) : IOmnibarTextMemberPathProvider
	{
		public string DisplayNameBeforeMatch => DisplayName[..MatchStart];

		public string DisplayNameMatch => DisplayName.Substring(MatchStart, MatchLength);

		public string DisplayNameAfterMatch => DisplayName[(MatchStart + MatchLength)..];

		public string GetTextMemberPath(string textMemberPath)
		{
			return textMemberPath switch
			{
				nameof(Path) => Path,
				nameof(DisplayName) => DisplayName,
				_ => string.Empty
			};
		}
	}
}
