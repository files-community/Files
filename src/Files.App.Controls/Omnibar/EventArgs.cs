// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Controls
{
	public record class OmnibarQuerySubmittedEventArgs(OmnibarMode Mode, object? Item, string Text);

	public record class OmnibarSuggestionChosenEventArgs(OmnibarMode Mode, object SelectedItem);

	public record class OmnibarTextChangedEventArgs(OmnibarMode Mode, OmnibarTextChangeReason Reason);

	public record class OmnibarModeChangedEventArgs(OmnibarMode? OldMode, OmnibarMode NewMode);

	public record class OmnibarIsFocusedChangedEventArgs(bool IsFocused);
}
