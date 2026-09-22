// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text;
using Windows.System;

namespace Files.App.Dialogs
{
	/// <summary>
	/// A dialog for adding a new key binding or editing an existing one on the Actions settings page.
	/// </summary>
	public sealed partial class KeyBindingEditorDialog : ContentDialog
	{
		private readonly ActionsViewModel _viewModel;
		private readonly ModifiableActionItem? _editingItem;

		private bool _isValidKeyBinding;

		public bool IsAddMode { get; }
		public bool IsEditMode { get; }
		public ObservableCollection<ModifiableActionItem> AllActionItems { get; }
		public string CommandDescription { get; }

		public KeyBindingEditorDialog(ActionsViewModel viewModel, ModifiableActionItem? itemToEdit = null)
		{
			_viewModel = viewModel;
			_editingItem = itemToEdit;

			IsAddMode = itemToEdit is null;
			IsEditMode = itemToEdit is not null;
			AllActionItems = viewModel.AllActionItems;
			CommandDescription = itemToEdit?.CommandDescription ?? string.Empty;

			InitializeComponent();

			Title = IsAddMode ? Strings.AddCommand.GetLocalizedResource() : Strings.Edit.GetLocalizedResource();
			PrimaryButtonText = IsAddMode ? Strings.Add.GetLocalizedResource() : Strings.Save.GetLocalizedResource();

			// Editing starts from the current binding, so saving is allowed right away.
			if (IsEditMode && _editingItem is not null)
				KeyBindingEditorTextBox.Text = _editingItem.KeyBinding.LocalizedLabel;

			_isValidKeyBinding = IsEditMode;
			IsPrimaryButtonEnabled = IsEditMode;

			PrimaryButtonClick += KeyBindingEditorDialog_PrimaryButtonClick;
		}

		private void ActionPickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_viewModel.SelectedActionItem = ActionPickerComboBox.SelectedItem as ModifiableActionItem;
			KeyBindingEditorTextBox.Focus(FocusState.Programmatic);
			UpdatePrimaryEnabled();
		}

		private void KeyBindingEditorTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			KeyBindingEditorTextBox.Focus(FocusState.Programmatic);
		}

		private void KeyBindingEditorTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			var pressedKey = e.OriginalKey;
			var pressedKeyValue = HotKey.LocalizedKeys.GetValueOrDefault((Keys)pressedKey);
			var buffer = new StringBuilder();

			var invalidKeys = new HashSet<VirtualKey>
			{
				VirtualKey.CapitalLock,
				VirtualKey.NumberKeyLock,
				VirtualKey.Scroll,
			};

			var modifierKeys = new HashSet<VirtualKey>
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

			var isInvalidKey = invalidKeys.Contains(pressedKey) || string.IsNullOrEmpty(pressedKeyValue);
			var isModifierKey = modifierKeys.Contains(pressedKey);

			if (isInvalidKey && !isModifierKey)
				ShowValidation(Strings.KeybindingInvalidKeyNotification.GetLocalizedResource());

			// A modifier on its own or an unusable key doesn't form a binding yet
			if (isInvalidKey || isModifierKey)
			{
				KeyBindingEditorTextBox.Text = buffer.ToString();
				_isValidKeyBinding = false;
				UpdatePrimaryEnabled();
				e.Handled = true;
				return;
			}

			var pressedModifiers = HotKeyHelpers.GetCurrentKeyModifiers();

			if (pressedModifiers.HasFlag(KeyModifiers.Ctrl))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Ctrl)}+");
			if (pressedModifiers.HasFlag(KeyModifiers.Alt))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Alt)}+");
			if (pressedModifiers.HasFlag(KeyModifiers.Shift))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Shift)}+");

			buffer.Append(pressedKeyValue);

			KeyBindingEditorTextBox.Text = buffer.ToString();
			ValidationInfoBar.IsOpen = false;
			_isValidKeyBinding = true;
			UpdatePrimaryEnabled();
			e.Handled = true;
		}

		private void UpdatePrimaryEnabled()
		{
			// Adding also requires an action to have been chosen from the picker
			IsPrimaryButtonEnabled = _isValidKeyBinding && (IsEditMode || _viewModel.SelectedActionItem is not null);
		}

		private void ShowValidation(string message)
		{
			ValidationInfoBar.Message = message;
			ValidationInfoBar.IsOpen = true;
		}

		private void KeyBindingEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (!_isValidKeyBinding || string.IsNullOrEmpty(KeyBindingEditorTextBox.Text))
			{
				args.Cancel = true;
				ShowValidation(Strings.KeybindingInvalidKeyNotification.GetLocalizedResource());
				return;
			}

			// The view model raises this flag when the chosen binding collides; reuse its own detection.
			_viewModel.IsAlreadyUsedTeachingTipOpened = false;

			if (IsAddMode)
			{
				if (_viewModel.SelectedActionItem is null)
				{
					args.Cancel = true;
					return;
				}

				_viewModel.SelectedActionItem.LocalizedKeyBindingLabel = KeyBindingEditorTextBox.Text;
				_viewModel.AddNewKeyBindingCommand.Execute(null);

				if (_viewModel.IsAlreadyUsedTeachingTipOpened)
				{
					_viewModel.IsAlreadyUsedTeachingTipOpened = false;
					args.Cancel = true;
					ShowValidation(Strings.KeybindingAlreadyUsedNotification.GetLocalizedResource());
					return;
				}

				// Reset the picker's scratch selection after a successful add
				if (_viewModel.SelectedActionItem is not null)
				{
					_viewModel.SelectedActionItem.LocalizedKeyBindingLabel = string.Empty;
					_viewModel.SelectedActionItem = null;
				}
			}
			else
			{
				_editingItem!.LocalizedKeyBindingLabel = KeyBindingEditorTextBox.Text;
				_viewModel.SaveCommand.Execute(_editingItem);

				if (_viewModel.IsAlreadyUsedTeachingTipOpened)
				{
					_viewModel.IsAlreadyUsedTeachingTipOpened = false;
					args.Cancel = true;
					ShowValidation(Strings.KeybindingAlreadyUsedNotification.GetLocalizedResource());
				}
			}
		}
	}
}
