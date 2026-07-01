// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Controls;

namespace Files.App.Data.Models
{
	internal record OmnibarPathModeSuggestionModel(string Path, string DisplayName) : IOmnibarTextMemberPathProvider
	{
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
