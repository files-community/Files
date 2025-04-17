// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class Omnibar
	{
		[GeneratedDependencyProperty]
		public partial IList<OmnibarMode>? Modes { get; set; }

		[GeneratedDependencyProperty]
		public partial OmnibarMode? CurrentSelectedMode { get; set; }

		[GeneratedDependencyProperty]
		public partial string? CurrentSelectedModeName { get; set; }

		[GeneratedDependencyProperty]
		public partial Thickness AutoSuggestBoxPadding { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsFocused { get; set; }

		partial void OnCurrentSelectedModeChanged(OmnibarMode? newValue)
		{
			CurrentSelectedModeName = newValue?.ModeName;
		}

		partial void OnIsFocusedChanged(bool newValue)
		{
			//_textBox?.Focus(newValue ? FocusState.Programmatic : FocusState.Unfocused);
		}
	}
}
