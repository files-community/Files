// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class TableViewColumn
	{
		[GeneratedDependencyProperty]
		public partial string? Header { get; set; }

		[GeneratedDependencyProperty]
		public partial ListSortDirection? SortDirection { get; set; }

		[GeneratedDependencyProperty]
		public partial string? Binding { get; set; }

		partial void OnSortDirectionChanged(ListSortDirection? newValue)
		{
			UpdateSortVisualState(true);
			NotifySortDirectionChanged();
		}

		partial void OnHeaderChanged(string? newValue)
		{
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, newValue ?? string.Empty);
		}
	}
}
