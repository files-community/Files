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

		private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
		{
			_isFocused = true;

			VisualStateManager.GoToState(CurrentSelectedMode, "Focused", true);
			VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);

			TryToggleIsSuggestionsPopupOpen(true);
		}

		private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// TextBox still has focus if the context menu for selected text is open
			if (_textBox.ContextFlyout.IsOpen)
				return;

			_isFocused = false;

			if (CurrentSelectedMode?.ContentOnInactive is not null)
			{
				VisualStateManager.GoToState(CurrentSelectedMode, "CurrentUnfocused", true);
				VisualStateManager.GoToState(_textBox, "InputAreaCollapsed", true);
			}

			TryToggleIsSuggestionsPopupOpen(false);
		}

		private void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
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

					ChooseSuggestionItem(_textBoxSuggestionsListView.SelectedItem);
				}
			}
			else if (e.Key == VirtualKey.Escape && _textBoxSuggestionsPopup.IsOpen)
			{
				e.Handled = true;

				RevertTextToUserInput();
				_textBoxSuggestionsPopup.IsOpen = false;
			}
			else
			{
				_textChangeReason = OmnibarTextChangeReason.UserInput;
			}
		}

		private void AutoSuggestBox_TextChanged(object sender, TextChangedEventArgs e)
		{
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

		private void AutoSuggestBoxSuggestionsListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (CurrentSelectedMode is null)
				return;

			ChooseSuggestionItem(e.ClickedItem);
			SubmitQuery(e.ClickedItem);
		}
	}
}
