// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public record class OmnibarQuerySubmittedEventArgs(OmnibarMode Mode, object? Item, string Text);

	public record class OmnibarSuggestionChosenEventArgs(OmnibarMode Mode, object SelectedItem);

	public record class OmnibarTextChangedEventArgs(OmnibarMode Mode, OmnibarTextChangeReason Reason);
}
