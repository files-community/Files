// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.EventArguments
{
	public class NavigationArguments
	{
		public bool FocusOnNavigation { get; set; } = false;

		public string? NavPathParam
		{
			get;
			set => field = value is null ? null : ShellHelpers.ResolveShellPath(value);
		}

		public IShellPage? AssociatedTabInstance { get; set; }

		public bool IsSearchResultPage { get; set; } = false;

		public string? SearchPathParam
		{
			get;
			set => field = value is null ? null : ShellHelpers.ResolveShellPath(value);
		}

		public string? SearchQuery { get; set; } = null;

		public bool IsLayoutSwitch { get; set; } = false;

		public IEnumerable<string>? SelectItems { get; set; }
	}
}
