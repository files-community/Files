// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class NavigationArguments
	{
		public bool FocusOnNavigation { get; set; }

		public string NavPathParam { get; set; }

		public IShellPage? AssociatedTabInstance { get; set; }

		public bool IsSearchResultPage { get; set; }

		public string? SearchPathParam { get; set; }

		public string? SearchQuery { get; set; }

		public bool SearchUnindexedItems { get; set; }

		public bool IsLayoutSwitch { get; set; }

		public IEnumerable<string>? SelectItems { get; set; }
	}
}
