// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Input;
using Windows.Foundation.Collections;
using Windows.System;
using WinRT;

namespace Files.App.Controls
{
	public partial class Omnibar
	{
		private void Omnibar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// Popup width has to be set manually because it doesn't stretch with the parent
			_textBoxSuggestionsContainerBorder.Width = ActualWidth;
		}

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		private void AutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			if (args.OldFocusedElement is null && !_isFocusInFlyout)
			{
				// Window is regaining activation and restoring focus to the TextBox. TrySetNewFocusedElement can't
				// move focus to the previously-focused item (it's often a recycled list item and gets rejected), so
				// ask the host to move focus to its content instead, keeping the omnibar out of edit mode.
				_placeCaretAtEndOnFocus = false;
				FocusRedirectRequested?.Invoke(this, System.EventArgs.Empty);
				return;
			}

			_previouslyFocusedElement = new(args.OldFocusedElement as UIElement);
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void AutoSuggestBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			// Programmatic focus moves (InputDevice == None) while the user is typing in the
			// Omnibar - typically a post-folder-load FocusActivePane / FocusFileList - should not
			// pull focus away and clear the in-progress query. User gestures still pass through.
			if (args.InputDevice is FocusInputDeviceKind.None && args.FocusState is FocusState.Programmatic)
			{
				args.TryCancel();
				return;
			}

			// Prevent the TextBox from losing focus when the ModeButton is focused
			if (args.NewFocusedElement is not Button button ||
				args.InputDevice is FocusInputDeviceKind.Keyboard ||
				button.Name.ToString() != "PART_ModeButton")
				return;

			args.TryCancel();
		}

		private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
		{
			GlobalHelper.WriteDebugStringForOmnibar("The TextBox got the focus.");

			// Focus returning from the context menu keeps the text and caret as they are
			if (_isFocusInFlyout)
			{
				_isFocusInFlyout = false;
				return;
			}

			IsFocused = true;
			IsFocusedChanged?.Invoke(this, new(IsFocused));

			if (_placeCaretAtEndOnFocus)
			{
				_placeCaretAtEndOnFocus = false;
				_textBox.Select(_textBox.Text.Length, 0);
			}
			else
				_textBox.SelectAll();
		}

		[DynamicWindowsRuntimeCast(typeof(FlyoutBase))]
		[DynamicWindowsRuntimeCast(typeof(Popup))]
		private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// TextBox still has focus if the context menu for selected text is open
			if (IsInFlyout(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(this.XamlRoot)))
			{
				_isFocusInFlyout = true;
				foreach (var flyout in new[] { _textBox.ContextFlyout, _textBox.SelectionFlyout })
				{
					if (flyout is not null)
					{
						flyout.Closed -= TextBoxFlyout_Closed;
						flyout.Closed += TextBoxFlyout_Closed;
					}
				}

				return;
			}

			HandleLostFocus();
		}

		[DynamicWindowsRuntimeCast(typeof(FlyoutBase))]
		private void TextBoxFlyout_Closed(object? sender, object e)
		{
			if (sender is FlyoutBase flyout)
				flyout.Closed -= TextBoxFlyout_Closed;

			DispatcherQueue.TryEnqueue(() =>
			{
				if (!_isFocusInFlyout)
					return;

				var focusedElement = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(this.XamlRoot);
				if (ReferenceEquals(focusedElement, _textBox))
					return;

				// After a command focus falls back to an ancestor, while dismissing the menu moves it to the clicked control
				if ((focusedElement is null || IsAncestorOfTextBox(focusedElement)) && _textBox.Focus(FocusState.Programmatic))
					return;

				_isFocusInFlyout = false;
				HandleLostFocus();
			});
		}

		private bool IsAncestorOfTextBox(object element)
		{
			for (var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(_textBox); parent is not null; parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent))
			{
				if (ReferenceEquals(parent, element))
					return true;
			}

			return false;
		}

		private void HandleLostFocus()
		{
			GlobalHelper.WriteDebugStringForOmnibar("The TextBox lost the focus.");

			_placeCaretAtEndOnFocus = false;
			_isTextTypedByUser = false;
			_isSuggestionSelectedByUser = false;
			IsFocused = false;
			IsFocusedChanged?.Invoke(this, new(IsFocused));
		}

		[DynamicWindowsRuntimeCast(typeof(FlyoutBase))]
		[DynamicWindowsRuntimeCast(typeof(Popup))]
		[DynamicWindowsRuntimeCast(typeof(DependencyObject))]
		private static bool IsInFlyout(object? focusedElement)
		{
			if (focusedElement is FlyoutBase)
				return true;

			for (var element = focusedElement as DependencyObject; element is not null; element = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element))
			{
				if (element is Popup)
					return true;
			}

			return false;
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
				var isAutoSelected = !_isSuggestionSelectedByUser && currentIndex is not -1;
				_isSuggestionSelectedByUser = true;

				if (e.Key is VirtualKey.Up)
				{
					nextIndex--;
				}
				else if (e.Key is VirtualKey.Down && !isAutoSelected)
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

			if (_textChangeReason is OmnibarTextChangeReason.ProgrammaticChange)
				_textBox.SelectAll();
			else
			{
				_userInput = _textBox.Text;
			}

			_isTextTypedByUser = _textChangeReason is OmnibarTextChangeReason.UserInput or OmnibarTextChangeReason.None;
			if (_isTextTypedByUser)
				_isSuggestionSelectedByUser = false;

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

		private void AutoSuggestBoxSuggestionsListView_ItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
		{
			if (_isFirstSuggestionSelectionQueued || _isSuggestionSelectedByUser)
				return;

			_isFirstSuggestionSelectionQueued = true;
			DispatcherQueue.TryEnqueue(() =>
			{
				_isFirstSuggestionSelectionQueued = false;
				if (_isSuggestionSelectedByUser)
					return;

				var selectFirst = CurrentSelectedMode?.IsFirstSuggestionAutoSelected is true &&
					_isTextTypedByUser &&
					!string.IsNullOrEmpty(_textBox.Text) &&
					_textBoxSuggestionsListView.Items.Count > 0;

				_textBoxSuggestionsListView.SelectedIndex = selectFirst ? 0 : -1;
			});
		}
	}
}
