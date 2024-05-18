// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text;
using Windows.System;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Represents settings page called Actions, which provides a way to customize key bindings.
	/// </summary>
	public sealed partial class ActionsPage : Page
	{
		private readonly string PART_EditButton = "EditButton";
		private readonly string NormalState = "Normal";
		private readonly string PointerOverState = "PointerOver";

		private ActionsViewModel ViewModel { get; set; } = new();

		public ActionsPage()
		{
			InitializeComponent();
		}

		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, PointerOverState, true);

			// Make edit button visible on pointer in
			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableActionItem item &&
				!item.IsInEditMode)
				editButton.Visibility = Visibility.Visible;
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, NormalState, true);

			// Make edit button invisible on pointer out
			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableActionItem item &&
				!item.IsInEditMode)
				editButton.Visibility = Visibility.Collapsed;
		}

		private void KeyBindingEditorTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
				return;

			var pressedKey = e.OriginalKey;
			var pressetKeyValue = HotKey.LocalizedKeys.GetValueOrDefault((Keys)pressedKey);
			var buffer = new StringBuilder();

			List<VirtualKey> invalidKeys =
			[
				VirtualKey.CapitalLock,
				VirtualKey.NumberKeyLock,
				VirtualKey.Scroll,
			];

			List<VirtualKey> modifierKeys =
			[
				VirtualKey.Shift,
				VirtualKey.Control,
				VirtualKey.Menu,
				VirtualKey.LeftWindows,
				VirtualKey.RightWindows,
				VirtualKey.LeftShift,
				VirtualKey.LeftControl,
				VirtualKey.RightControl,
				VirtualKey.LeftMenu,
				VirtualKey.RightMenu
			];

			var isInvalidKey = (invalidKeys.Contains(pressedKey) || string.IsNullOrEmpty(pressetKeyValue));
			var isModifierKey = modifierKeys.Contains(pressedKey);

			if (isInvalidKey && !isModifierKey)
			{
				InvalidKeyTeachingTip.Target = textBox;
				ViewModel.IsInvalidKeyTeachingTipOpened = true;
			}

			// Check if the pressed key is invalid, a modifier, or has no value; Don't show it in the TextBox yet
			if (isInvalidKey || isModifierKey)
			{
				textBox.Text = buffer.ToString();
				ViewModel.EnableAddNewKeyBindingButton = false;

				// Prevent key down event in other UIElements from getting invoked
				e.Handled = true;

				return;
			}

			var pressedModifiers = HotKeyHelpers.GetCurrentKeyModifiers();

			// Add the modifiers with translated
			if (pressedModifiers.HasFlag(KeyModifiers.Ctrl))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Ctrl)}+");
			if (pressedModifiers.HasFlag(KeyModifiers.Alt))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Alt)}+");
			if (pressedModifiers.HasFlag(KeyModifiers.Shift))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Shift)}+");

			// Add the key with translated
			buffer.Append(pressetKeyValue);

			// Set text
			textBox.Text = buffer.ToString();

			ViewModel.EnableAddNewKeyBindingButton = true;

			// Prevent key down event in other UIElements from getting invoked
			e.Handled = true;
		}

		private void KeyBindingEditorTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			// Focus the editor TextBox
			TextBox keyboardShortcutEditorTextBox = (TextBox)sender;
			keyboardShortcutEditorTextBox.Focus(FocusState.Programmatic);
		}

		private void NewKeyBindingItemPickerComboBox_DropDownClosed(object sender, object e)
		{
			// Check if a new action is selected
			if (ViewModel.SelectedActionItem is null)
				return;

			// Focus the editor TextBox
			KeyBindingEditorTextBox.Focus(FocusState.Programmatic);
		}
	}
}
