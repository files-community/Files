// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Controls;

namespace Files.App.Data.Models
{
	internal record OmnibarPathModeSuggestionModel(string Path, string DisplayName, bool IsPlaceholder = false) : IOmnibarTextMemberPathProvider
	{
		public virtual string DisplayNameMatch => string.Empty;

		public virtual string DisplayNameAfterMatch => DisplayName;

		public virtual string? Description => null;

		public virtual double NameMinWidth => 0;

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

	internal sealed record FolderAliasSuggestionModel(string Name, string Folder, int MatchLength, double NameWidth)
		: OmnibarPathModeSuggestionModel($"{FolderAliasHelpers.Prefix}{Name}", Name)
	{
		public override string DisplayNameMatch => Name[..Math.Min(MatchLength, Name.Length)];

		public override string DisplayNameAfterMatch => Name[DisplayNameMatch.Length..];

		public override string? Description => Folder;

		public override double NameMinWidth => NameWidth;
	}
}
