// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class BreadcrumbBarItem
	{
		[GeneratedDependencyProperty]
		public partial bool IsEllipsis { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsLastItem { get; set; }

		partial void OnIsEllipsisChanged(bool newValue)
		{
			VisualStateManager.GoToState(this, newValue ? "ChevronCollapsed" : "ChevronVisible", true);
		}

		partial void OnIsLastItemChanged(bool newValue)
		{
			VisualStateManager.GoToState(this, newValue ? "ChevronCollapsed" : "ChevronVisible", true);
		}
	}
}
