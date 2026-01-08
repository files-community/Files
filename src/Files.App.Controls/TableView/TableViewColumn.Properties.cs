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
			var visualStateName = SortDirection switch
			{
				ListSortDirection.Ascending => TemplateVisualStateName_SortOrderAscending,
				ListSortDirection.Descending => TemplateVisualStateName_SortOrderDescending,
				_ => TemplateVisualStateName_SortOrderNone,
			};

			VisualStateManager.GoToState(this, visualStateName, true);
		}
	}
}
