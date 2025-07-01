// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Controls
{
	public partial class Omnibar
	{
		private void Omnibar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// Popup width has to be set manually because it doesn't stretch with the parent
			_textBoxSuggestionsContainerBorder.Width = ActualWidth;
		}
		
		private void Omnibar_LostFocus(object sender, RoutedEventArgs e)
		{
			// Reset to the default mode when Omnibar loses focus
			CurrentSelectedMode = Modes?.FirstOrDefault();
		}

		private void AutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			if (args.OldFocusedElement is null)
				return;

			_previouslyFocusedElement = new(args.OldFocusedElement as UIElement);
		}

		private void AutoSuggestBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			if (IsModeButtonPressed)
			{
				IsModeButtonPressed = false;
				args.TryCancel();
				return;
			}
		}

		private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
		{
			IsFocused = true;
			_textBox.SelectAll();
		}

		private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// TextBox still has focus if the context menu for selected text is open
			if (_textBox.ContextFlyout.IsOpen)
				return;

			IsFocused = false;
		}

		private async void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Enter)
			{
				e.Handled = true;

				SubmitQuery(_textBoxSuggestionsPopup.IsOpen && _textBoxSuggestionsListView.SelectedIndex is not -1 ? _textBoxSuggestionsListView.SelectedItem : null);
			}
			else if ((e.Key == VirtualKey.Up || e.Key == VirtualKey.Down) && _textBoxSuggestionsPopup.IsOpen)
			{
				e.Handled = true;

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
			else if (e.Key == VirtualKey.Tab && !InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
			{
				// Focus on inactive content when pressing Tab instead of moving to the next control in the tab order
				e.Handled = true;
				IsFocused = false;
				await Task.Delay(15);
				CurrentSelectedMode?.ContentOnInactive?.Focus(FocusState.Keyboard);
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

			TextChanged?.Invoke(this, new(CurrentSelectedMode, _textChangeReason));

			// Reset
			_textChangeReason = OmnibarTextChangeReason.None;
		}

		private void AutoSuggestBoxSuggestionsPopup_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
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
