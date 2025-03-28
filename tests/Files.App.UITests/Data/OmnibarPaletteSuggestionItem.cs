// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using System;

namespace Files.App.UITests.Data
{
	public partial record OmnibarPaletteSuggestionItem(string Title, string Description, string HotKeys)
		: IOmnibarTextMemberPathProvider
	{
		/// <inheritdoc/>
		public string GetTextMemberPath(string textMemberPath)
		{
			return textMemberPath switch
			{
				nameof(Title) => Title,
				nameof(Description) => Description,
				nameof(HotKeys) => HotKeys,
				_ => throw new ArgumentOutOfRangeException(nameof(textMemberPath), textMemberPath, null),
			};
		}
	}
}
