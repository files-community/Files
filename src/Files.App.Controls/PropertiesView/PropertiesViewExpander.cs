// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace Files.App.Controls;

public partial class PropertiesViewExpander : Expander
{
	[GeneratedDependencyProperty]
	public partial IconElement? HeaderIcon { get; set; }

	public PropertiesViewExpander()
	{
		DefaultStyleKey = typeof(PropertiesViewExpander);
	}
}
