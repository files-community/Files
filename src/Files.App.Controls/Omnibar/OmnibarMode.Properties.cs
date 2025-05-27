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
		/// <remark>
		/// Implement <see cref="IOmnibarTextMemberPathProvider"/> in <see cref="SuggestionItemsSource"/> to get the text member path from the suggestion item correctly.
		/// </remark>
		public partial string? TextMemberPath { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool UpdateTextOnSelect { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool UpdateTextOnArrowKeys { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsAutoFocusEnabled { get; set; }

		[GeneratedDependencyProperty]
		public partial OmnibarModeUnfocusedStateBehaviors UnfocusedStateBehaviors { get; set; }

		partial void OnTextChanged(string? newValue)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false)
				return;

			owner.ChangeTextBoxText(newValue ?? string.Empty);
		}
	}
}
