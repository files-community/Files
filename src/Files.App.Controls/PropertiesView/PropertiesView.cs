// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class PropertiesView : ItemsControl
	{
		[GeneratedDependencyProperty]
		public partial FrameworkElement? Header { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? Footer { get; set; }

		public PropertiesView()
		{
			DefaultStyleKey = typeof(PropertiesView);
		}
	}
}
