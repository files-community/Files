// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class LocationUnavailableIndicator : UserControl
	{
		[GeneratedDependencyProperty]
		public partial string? Glyph { get; set; }

		[GeneratedDependencyProperty]
		public partial string? Title { get; set; }

		[GeneratedDependencyProperty]
		public partial string? Message { get; set; }

		public LocationUnavailableIndicator()
		{
			InitializeComponent();
		}
	}
}
