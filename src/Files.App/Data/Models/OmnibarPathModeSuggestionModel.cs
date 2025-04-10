// Copyright (c) Files Community
// Licensed under the MIT License.

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
