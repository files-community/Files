// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using System.Linq;
using System.Collections.Generic;

namespace Files.App.Controls
{
	public partial class OmnibarMode
	{
		[GeneratedDependencyProperty]
		public partial string? Text { get; set; }

		[GeneratedDependencyProperty]
		public partial string? TextPlaceholder { get; set; }

		[GeneratedDependencyProperty]
		public partial string? ModeName { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? ContentOnInactive { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? IconOnActive { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? IconOnInactive { get; set; }

		[GeneratedDependencyProperty]
		public partial object? SuggestionItemsSource { get; set; }

		[GeneratedDependencyProperty]
		public partial DataTemplate? SuggestionItemTemplate { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsDefault { get; set; }

		[GeneratedDependencyProperty]
		internal partial Omnibar? Host { get; set; }
	}
}
