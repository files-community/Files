// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class OmnibarMode
	{
		[GeneratedDependencyProperty]
		public partial string? Text { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsDefault { get; set; }

		[GeneratedDependencyProperty]
		public partial string? PlaceholderText { get; set; }

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
		public partial string? DisplayMemberPath { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool UpdateTextOnSelect { get; set; }
	}
}
