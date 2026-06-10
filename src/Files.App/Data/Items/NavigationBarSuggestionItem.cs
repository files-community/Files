// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;

namespace Files.App.Data.Items
{
	[Obsolete("Remove once Omnibar goes out of experimental.")]
	public sealed partial class NavigationBarSuggestionItem : IOmnibarTextMemberPathProvider
	{
		public IRichCommand? Command { get; }

		public Style? ThemedIconStyle { get; }

		public string? Glyph { get; }

		public string Text { get; }

		public string? PrimaryDisplayPreMatched { get; }

		public string? PrimaryDisplayMatched { get; }

		public string? PrimaryDisplayPostMatched { get; }

		public HotKeyCollection HotKeys { get; }

		public NavigationBarSuggestionItem(string? searchText, IRichCommand? command)
		{
			Command = command;

			if (command is null)
			{
				Text = string.Format(Strings.NoCommandsFound.GetLocalizedResource(), searchText);
			}
			else
			{
				ThemedIconStyle = command.ThemedIconStyle;
				Glyph = command.Glyph.BaseGlyph;
				Text = command.Description;
				HotKeys = command.HotKeys;
			}

			if (searchText is null)
			{
				PrimaryDisplayMatched = Text;
			}
			else
			{
				var index = Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
				if (index < 0)
				{
					PrimaryDisplayPreMatched = Text;
				}
				else
				{
					PrimaryDisplayPreMatched = Text.Substring(0, index);
					PrimaryDisplayMatched = Text.Substring(index, searchText.Length);
					PrimaryDisplayPostMatched = Text.Substring(index + searchText.Length);
				}
			}
		}

		public string GetTextMemberPath(string textMemberPath)
		{
			return textMemberPath switch
			{
				nameof(Text) => Text,
				_ => string.Empty
			};
		}
	}
}
