// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Settings
{
	public sealed class SettingsSearchResult
	{
		public SettingsSearchResult(SettingsPageKind pageKind, string pageDisplayName, string headerText, string? parentHeaderText = null)
		{
			PageKind = pageKind;
			PageDisplayName = pageDisplayName;
			HeaderText = headerText;
			ParentHeaderText = parentHeaderText;

			DisplayPath = parentHeaderText is null
				? pageDisplayName
				: $"{pageDisplayName} › {parentHeaderText}";

			Haystack = parentHeaderText is null
				? $"{pageDisplayName} {headerText}"
				: $"{pageDisplayName} {parentHeaderText} {headerText}";
		}

		public SettingsPageKind PageKind { get; }

		public string PageDisplayName { get; }

		public string HeaderText { get; }

		public string? ParentHeaderText { get; }

		public string DisplayPath { get; }

		public string Haystack { get; }
	}
}
