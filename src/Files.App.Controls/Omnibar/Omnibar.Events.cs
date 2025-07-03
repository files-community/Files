// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.Controls
{
	public partial class Omnibar
	{
		private void Omnibar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// Popup width has to be set manually because it doesn't stretch with the parent
			_textBoxSuggestionsContainerBorder.Width = ActualWidth;
		}

		private void AutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			if (args.OldFocusedElement is null)
				return;

			GlobalHelper.WriteDebugStringForOmnibar("The TextBox is getting the focus.");

			_previouslyFocusedElement = new(args.OldFocusedElement as UIElement);
		}

		private void AutoSuggestBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			// Prevent the TextBox from losing focus when the ModeButton is focused
			if (args.NewFocusedElement is not Button button ||
				args.InputDevice is FocusInputDeviceKind.Keyboard ||
				button.Tag?.ToString() != "ModeButton")
				return;

			args.TryCancel();
		}

		private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
		{
			GlobalHelper.WriteDebugStringForOmnibar("The TextBox got the focus.");

			IsFocused = true;
			_textBox.SelectAll();
		}

		private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// TextBox still has focus if the context menu for selected text is open
			var element = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(this.XamlRoot);
			if (element is FlyoutBase or Popup)
				return;

			GlobalHelper.WriteDebugStringForOmnibar("The TextBox lost the focus.");

			IsFocused = false;

			// Reset to the default mode when Omnibar loses focus
			CurrentSelectedMode = Modes?.FirstOrDefault();
		}

		private async void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Enter)
			{
				e.Handled = true;

				GlobalHelper.WriteDebugStringForOmnibar("The TextBox accepted the Enter key.");

				SubmitQuery(_textBoxSuggestionsPopup.IsOpen && _textBoxSuggestionsListView.SelectedIndex is not -1 ? _textBoxSuggestionsListView.SelectedItem : null);
			}
			else if ((e.Key == VirtualKey.Up || e.Key == VirtualKey.Down) && _textBoxSuggestionsPopup.IsOpen)
			{
				e.Handled = true;

				GlobalHelper.WriteDebugStringForOmnibar("The TextBox accepted the Up/Down key while the suggestions pop-up is open.");

				var currentIndex = _textBoxSuggestionsListView.SelectedIndex;
				var nextIndex = currentIndex;
				var suggestionsCount = _textBoxSuggestionsListView.Items.Count;

				if (e.Key is VirtualKey.Up)
				{
					nextIndex--;
				}
				else if (e.Key is VirtualKey.Down)
				{
					nextIndex++;
				}

				if (0 > nextIndex || nextIndex >= suggestionsCount)
				{
					RevertTextToUserInput();
				}
				else
				{
					_textBoxSuggestionsListView.SelectedIndex = nextIndex;

					ChooseSuggestionItem(_textBoxSuggestionsListView.SelectedItem, true);
				}
			}
			else if (e.Key == VirtualKey.Escape)
			{
				e.Handled = true;

				GlobalHelper.WriteDebugStringForOmnibar("The TextBox accepted the Esc key.");

				if (_textBoxSuggestionsPopup.IsOpen)
				{
					RevertTextToUserInput();
					_textBoxSuggestionsPopup.IsOpen = false;
				}
				else
				{
					_previouslyFocusedElement.TryGetTarget(out var previouslyFocusedElement);
					previouslyFocusedElement?.Focus(FocusState.Programmatic);
				}
			}
			else
			{
				_textChangeReason = OmnibarTextChangeReason.UserInput;
			}
		}

		private void AutoSuggestBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (string.Compare(_textBox.Text, CurrentSelectedMode!.Text, StringComparison.OrdinalIgnoreCase) is not 0)
				CurrentSelectedMode!.Text = _textBox.Text;

			// UpdateSuggestionListView();

			if (_textChangeReason is not OmnibarTextChangeReason.SuggestionChosen and
				not OmnibarTextChangeReason.ProgrammaticChange)
			{
				_textChangeReason = OmnibarTextChangeReason.UserInput;
				_userInput = _textBox.Text;
			}
			else if (_textChangeReason is OmnibarTextChangeReason.ProgrammaticChange)
				_textBox.SelectAll();

			TextChanged?.Invoke(this, new(CurrentSelectedMode, _textChangeReason));

			// Reset
			_textChangeReason = OmnibarTextChangeReason.None;
		}

		private void AutoSuggestBoxSuggestionsPopup_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			// The suggestions popup is never wanted to be focused when it come to open.
			args.TryCancel();
		}

		private void AutoSuggestBoxSuggestionsPopup_Opened(object? sender, object e)
		{
			if (_textBoxSuggestionsListView.Items.Count > 0)
				_textBoxSuggestionsListView.ScrollIntoView(_textBoxSuggestionsListView.Items[0]);
		}

		private void AutoSuggestBoxSuggestionsListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (CurrentSelectedMode is null)
				return;

			ChooseSuggestionItem(e.ClickedItem);
			SubmitQuery(e.ClickedItem);
		}

		private void AutoSuggestBoxSuggestionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_textBoxSuggestionsListView.ScrollIntoView(_textBoxSuggestionsListView.SelectedItem);
		}
	}
}
