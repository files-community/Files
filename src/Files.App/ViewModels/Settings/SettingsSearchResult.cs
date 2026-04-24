// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Settings
{
	public sealed class SettingsSearchResult
	{
		public SettingsPageKind PageKind { get; init; }

		public string PageDisplayName { get; init; } = string.Empty;

		public string HeaderText { get; init; } = string.Empty;

		public string? ParentHeaderText { get; init; }

		public string DisplayPath => ParentHeaderText is null
			? PageDisplayName
			: $"{PageDisplayName} › {ParentHeaderText}";

		public string Haystack => ParentHeaderText is null
			? $"{PageDisplayName} {HeaderText}"
			: $"{PageDisplayName} {ParentHeaderText} {HeaderText}";
	}
}
