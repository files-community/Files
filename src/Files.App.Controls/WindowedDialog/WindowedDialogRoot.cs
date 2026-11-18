// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace Files.App.Controls;

internal partial class WindowedDialogRoot : ContentControl
{
	[GeneratedDependencyProperty]
	internal partial string? Title { get; set; }

	[GeneratedDependencyProperty]
	internal partial FrameworkElement? Header { get; set; }

	[GeneratedDependencyProperty]
	internal partial FrameworkElement? Footer { get; set; }

	internal WindowedDialogRoot()
	{
		DefaultStyleKey = typeof(WindowedDialogRoot);
	}
}
