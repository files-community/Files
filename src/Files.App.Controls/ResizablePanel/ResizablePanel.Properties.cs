// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls;

public partial class ResizablePanel
{
	[GeneratedDependencyProperty(DefaultValue = Orientation.Horizontal)]
	public partial Orientation Orientation { get; set; }

	partial void OnOrientationChanged(Orientation newValue)
	{
		InvalidateAutoGeneration();
	}
}
