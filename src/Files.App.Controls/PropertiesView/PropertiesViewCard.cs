// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace Files.App.Controls;

public partial class PropertiesViewCard : ButtonBase
{
	[GeneratedDependencyProperty]
	public partial object? Header { get; set; }

	[GeneratedDependencyProperty]
	public partial FrameworkElement? HeaderIcon { get; set; }

	[GeneratedDependencyProperty]
	public partial FrameworkElement? IsClickEnabled { get; set; }
}
