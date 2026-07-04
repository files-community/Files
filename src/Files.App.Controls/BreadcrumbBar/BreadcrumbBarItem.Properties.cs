// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class BreadcrumbBarItem
	{
		[GeneratedDependencyProperty]
		public partial bool IsEllipsis { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsLastItem { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsChevronVisible { get; set; }

		[GeneratedDependencyProperty]
		public partial string? ItemToolTip { get; set; }

		[GeneratedDependencyProperty]
		public partial string? ChevronToolTip { get; set; }

		partial void OnIsEllipsisChanged(bool newValue)
		{
			UpdateChevronVisibilityState();
		}

		partial void OnIsChevronVisibleChanged(bool newValue)
		{
			UpdateChevronVisibilityState();
		}

		private void UpdateChevronVisibilityState()
		{
			var visible = !IsEllipsis && IsChevronVisible;
			VisualStateManager.GoToState(this, visible ? "ChevronVisible" : "ChevronCollapsed", true);
		}
	}
}
