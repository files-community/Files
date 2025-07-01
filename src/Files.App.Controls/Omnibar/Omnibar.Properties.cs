﻿// Copyright (c) Files Community
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

		partial void OnCurrentSelectedModeNameChanged(string? newValue)
		{
			if (string.IsNullOrEmpty(newValue) ||
				string.IsNullOrEmpty(CurrentSelectedMode?.Name) ||
				CurrentSelectedMode.Name.Equals(newValue) ||
				Modes is null)
				return;

			var newMode = Modes.Where(x => x.Name?.Equals(newValue) ?? false).FirstOrDefault();
			if (newMode is null)
				return;

			CurrentSelectedMode = newMode;
		}

		partial void OnIsFocusedChanged(bool newValue)
		{
			if (CurrentSelectedMode is null || _textBox is null)
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
