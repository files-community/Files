// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Data.Models
{
	internal sealed class ToolbarHistoryItemModel
	{
		public PageStackEntry PageStackEntry { get; }

		public bool IsBackMode { get; }

		public ToolbarHistoryItemModel(PageStackEntry pageStackEntry, bool isBackMode)
		{
			PageStackEntry = pageStackEntry;
			IsBackMode = isBackMode;
		}
	}
}
