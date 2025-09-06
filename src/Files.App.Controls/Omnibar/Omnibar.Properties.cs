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

		partial void OnCurrentSelectedModePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is not OmnibarMode newMode)
				return;

			if (e.OldValue is OmnibarMode oldMode)
				GlobalHelper.WriteDebugStringForOmnibar($"The mode change from {oldMode} to {newMode} has been requested.");
			else
				GlobalHelper.WriteDebugStringForOmnibar($"The mode change to {newMode} has been requested.");

			ChangeMode(e.OldValue as OmnibarMode, newMode);
			CurrentSelectedModeName = newMode.Name;
		}

		partial void OnCurrentSelectedModeNamePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is not string oldValue ||
				e.NewValue is not string newValue ||
				string.IsNullOrEmpty(newValue) ||
				string.IsNullOrEmpty(CurrentSelectedMode?.Name) ||
				CurrentSelectedMode.Name.Equals(newValue, StringComparison.OrdinalIgnoreCase) ||
				oldValue.Equals(newValue, StringComparison.OrdinalIgnoreCase) ||
				Modes is null)
				return;

			var newMode = Modes.Where(x => x.Name?.Equals(newValue, StringComparison.OrdinalIgnoreCase) ?? false).FirstOrDefault();
			if (newMode is null)
				return;

			CurrentSelectedMode = newMode;
		}

		partial void OnIsFocusedPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (CurrentSelectedMode is null || _textBox is null || e.OldValue is not bool oldValue || e.NewValue is not bool newValue || oldValue == newValue)
				return;

			GlobalHelper.WriteDebugStringForOmnibar($"{nameof(IsFocused)} has been changed to {IsFocused}");

			if (newValue)
			{
				VisualStateManager.GoToState(CurrentSelectedMode, "Focused", true);
				VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
			}
			else
			{
				if (CurrentSelectedMode?.ContentOnInactive is not null)
				{
					VisualStateManager.GoToState(CurrentSelectedMode, "CurrentUnfocused", true);
					VisualStateManager.GoToState(_textBox, "InputAreaCollapsed", true);
				}
			}

			TryToggleIsSuggestionsPopupOpen(newValue);
		}
	}
}
