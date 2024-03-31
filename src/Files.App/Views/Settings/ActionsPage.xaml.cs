// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.Services.Settings;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Views.Settings
{
	public sealed partial class ActionsPage : Page
	{
		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly string PART_EditButton = "EditButton";
		private readonly string NormalState = "Normal";
		private readonly string PointerOverState = "PointerOver";

		static readonly IReadOnlyList<VirtualKey> ModifierKeys = new List<VirtualKey>
		{
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
		};

		public ActionsPage()
		{
			InitializeComponent();
		}

		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, PointerOverState, true);

			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableCommandHotKeyItem item &&
				!item.IsEditMode)
				editButton.Visibility = Visibility.Visible;
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, NormalState, true);

			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableCommandHotKeyItem item &&
				!item.IsEditMode)
				editButton.Visibility = Visibility.Collapsed;
		}

		private void EditButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is ModifiableCommandHotKeyItem item)
			{
				// Hide the add-new section grid
				ViewModel.ShowAddNewShortcutGrid = false;

				// Reset the selected item's info
				if (ViewModel.SelectedNewShortcutItem is not null)
				{
					ViewModel.SelectedNewShortcutItem.HotKeyText = "";
					ViewModel.SelectedNewShortcutItem = null;
				}

				// Reset edit mode for each item
				foreach (var hotkey in ViewModel.KeyboardShortcuts)
				{
					hotkey.IsEditMode = false;
					hotkey.HotKeyText = hotkey.HotKey.Label;
				}

				// Enter edit mode
				item.IsEditMode = true;
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.DataContext is not ModifiableCommandHotKeyItem item)
				return;

			if (item.HotKeyText == item.PreviousHotKey.Label)
			{
				item.IsEditMode = false;
				return;
			}

			// Check if this hot key is already taken
			foreach (var hotkey in ViewModel.KeyboardShortcuts)
			{
				if (item.HotKeyText == hotkey.PreviousHotKey)
				{
					ViewModel.IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			// Get clone of customized hotkeys to overwrite
			var actions = new Dictionary<string, string>(GeneralSettingsService.Actions);

			// Initialize
			var existingCollection = HotKeyCollection.Empty;
			var newHotKey = HotKey.Parse(item.HotKeyText);
			var defaultCollection = item.DefaultHotKeyCollection;

			// The first time to customize
			if (!item.IsCustomized)
			{
				// Override previous default key with the new one
				var editableDefaultCollection = defaultCollection.ToList();
				editableDefaultCollection.RemoveAll(x => x.Code == item.PreviousHotKey.Code);
				editableDefaultCollection.Add(newHotKey);
				var customizedDefaultCollection = new HotKeyCollection(editableDefaultCollection);

				// Set to the user setting
				actions.Add(item.CommandCode.ToString(), customizedDefaultCollection.Code);
				GeneralSettingsService.Actions = actions;

				// Update visual
				item.PreviousHotKey = newHotKey;
				item.HotKey = newHotKey;

				// Set as customized
				foreach (var action in ViewModel.KeyboardShortcuts)
				{
					if (action.CommandCode == item.CommandCode)
						action.IsCustomized = !item.DefaultHotKeyCollection.Contains(action.HotKey);
				}

				// Exit edit mode
				item.IsEditMode = false;

				return;
			}

			// Remove existing setting of the command code
			foreach (var action in actions)
			{
				if (Enum.TryParse(action.Key, true, out CommandCodes code) && code == item.CommandCode)
				{
					existingCollection = HotKeyCollection.Parse(action.Value);

					// If already there, exit rather than remove it
					if (existingCollection.Contains(newHotKey))
						return;

					actions.Remove(action.Key);
					break;
				}
			}

			// Create a new one
			var newCollectionString = string.Join(' ', existingCollection.ToList().Where(x => x.Label != item.HotKey.Label)) + (existingCollection.IsEmpty ? string.Empty : " ") + item.HotKeyText;
			var newCollection = HotKeyCollection.Parse(newCollectionString);

			// Set to the user setting
			if (newCollection.Select(x => x.Label).SequenceEqual(defaultCollection.Select(x => x.Label)))
				actions.Add(item.CommandCode.ToString(), newCollectionString);

			// Update visual and set
			GeneralSettingsService.Actions = actions;
			item.PreviousHotKey = newHotKey;
			item.HotKey = newHotKey;

			// Set as customized
			foreach (var action in ViewModel.KeyboardShortcuts)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsCustomized = !item.DefaultHotKeyCollection.Contains(action.HotKey);
			}

			// Exit edit mode
			item.IsEditMode = false;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.DataContext is not ModifiableCommandHotKeyItem item)
				return;

			item.IsEditMode = false;
			item.HotKeyText = item.HotKey.Label;
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.DataContext is not ModifiableCommandHotKeyItem item)
				return;

			var actions = new Dictionary<string, string>(GeneralSettingsService.Actions);

			HotKeyCollection customizedCollection = HotKeyCollection.Empty;

			// Remove existing setting
			foreach (var action in actions)
			{
				if (Enum.TryParse(action.Key, true, out CommandCodes code) && code == item.CommandCode)
				{
					// Override previous default key with the new one
					var editableCollection = HotKeyCollection.Parse(action.Value).ToList();
					editableCollection.RemoveAll(x => x.Code == item.PreviousHotKey.Code);
					customizedCollection = new HotKeyCollection(editableCollection);

					actions.Remove(action.Key);
					break;
				}
			}

			// Set
			if (customizedCollection.Label != string.Empty)
				actions.Add(item.CommandCode.ToString(), customizedCollection.Label);

			GeneralSettingsService.Actions = actions;

			item.IsEditMode = false;

			ViewModel.KeyboardShortcuts.Remove(item);
		}

		private void EditorTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
				return;

			var pressedKey = e.OriginalKey;

			// If pressed key is one of modifier don't show it in the TextBox yet
			foreach (var modifier in ModifierKeys)
			{
				if (pressedKey == modifier)
					return;
			}

			var modifiers = VirtualKeyModifiers.None;

			// TODO: Move to HotKeyHelpers.GetCurrentKeyModifiers()
			foreach (var modifier in ModifierKeys)
			{
				var keyState = InputKeyboardSource.GetKeyStateForCurrentThread(modifier);

				if (keyState.HasFlag(CoreVirtualKeyStates.Down))
				{
					switch (modifier)
					{
						case VirtualKey.Control:
						case VirtualKey.LeftControl:
						case VirtualKey.RightControl:
							modifiers |= VirtualKeyModifiers.Control;
							break;
						case VirtualKey.Menu:
						case VirtualKey.LeftMenu:
						case VirtualKey.RightMenu:
							modifiers |= VirtualKeyModifiers.Menu;
							break;
						case VirtualKey.Shift:
						case VirtualKey.LeftShift:
							modifiers |= VirtualKeyModifiers.Shift;
							break;
					}
				}
			}

			string text = string.Empty;

			// Add the modifiers with translated
			if (modifiers.HasFlag(VirtualKeyModifiers.Control))
				text += $"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Ctrl)}+";
			if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
				text += $"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Menu)}+";
			if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
				text += $"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Shift)}+";

			// Add the key with translated
			text += HotKey.LocalizedKeys.GetValueOrDefault((Keys)pressedKey);

			// Ban from using only alphabetic chars
			// Ctrl+A is allowed, but only A is not allowed in any context
			if (modifiers == VirtualKeyModifiers.None &&
				(VirtualKey.A < pressedKey && pressedKey < VirtualKey.Z))
				return;

			// Set text
			textBox.Text = text;

			// Prevent from invoking key down event in other UIElements
			e.Handled = true;
		}
	}
}
