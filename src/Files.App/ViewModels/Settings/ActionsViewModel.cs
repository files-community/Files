// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed class ActionsViewModel : ObservableObject
	{
		// Dependency injections

		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Properties

		public ObservableCollection<ModifiableCommandHotKeyItem> ValidKeyboardShortcuts { get; } = [];
		public ObservableCollection<ModifiableCommandHotKeyItem> AllKeyboardShortcuts { get; } = [];

		private bool _IsResetAllConfirmationTeachingTipOpened;
		public bool IsResetAllConfirmationTeachingTipOpened
		{
			get => _IsResetAllConfirmationTeachingTipOpened;
			set => SetProperty(ref _IsResetAllConfirmationTeachingTipOpened, value);
		}

		private bool _IsAlreadyUsedTeachingTipOpened;
		public bool IsAlreadyUsedTeachingTipOpened
		{
			get => _IsAlreadyUsedTeachingTipOpened;
			set => SetProperty(ref _IsAlreadyUsedTeachingTipOpened, value);
		}

		private bool _ShowAddNewHotKeySection;
		public bool ShowAddNewHotKeySection
		{
			get => _ShowAddNewHotKeySection;
			set => SetProperty(ref _ShowAddNewHotKeySection, value);
		}

		private ModifiableCommandHotKeyItem? _SelectedNewShortcutItem;
		public ModifiableCommandHotKeyItem? SelectedNewShortcutItem
		{
			get => _SelectedNewShortcutItem;
			set => SetProperty(ref _SelectedNewShortcutItem, value);
		}

		// Commands

		public ICommand LoadCommandsCommand { get; set; }
		public ICommand ShowAddNewHotKeySectionCommand { get; set; }
		public ICommand HideAddNewHotKeySectionCommand { get; set; }
		public ICommand AddNewHotKeyCommand { get; set; }
		public ICommand ShowRestoreDefaultsConfirmationCommand { get; set; }
		public ICommand RestoreDefaultsCommand { get; set; }
		public ICommand EditCommand { get; set; }
		public ICommand SaveCommand { get; set; }
		public ICommand CancelCommand { get; set; }
		public ICommand DeleteCommand { get; set; }

		// Constructor

		public ActionsViewModel()
		{
			LoadCommandsCommand = new AsyncRelayCommand(ExecuteLoadCommandsCommand);
			ShowAddNewHotKeySectionCommand = new RelayCommand(ExecuteShowAddNewHotKeySectionCommand);
			HideAddNewHotKeySectionCommand = new RelayCommand(ExecuteHideAddNewHotKeySectionCommand);
			AddNewHotKeyCommand = new RelayCommand(ExecuteAddNewHotKeyCommand);
			ShowRestoreDefaultsConfirmationCommand = new RelayCommand(ExecuteShowRestoreDefaultsConfirmationCommand);
			RestoreDefaultsCommand = new RelayCommand(ExecuteRestoreDefaultsCommand);
			EditCommand = new RelayCommand<ModifiableCommandHotKeyItem>(ExecuteEditCommand);
			SaveCommand = new RelayCommand<ModifiableCommandHotKeyItem>(ExecuteSaveCommand);
			CancelCommand = new RelayCommand<ModifiableCommandHotKeyItem>(ExecuteCancelCommand);
			DeleteCommand = new RelayCommand<ModifiableCommandHotKeyItem>(ExecuteDeleteCommand);
		}

		// Command methods

		private async Task ExecuteLoadCommandsCommand()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				ValidKeyboardShortcuts.Clear();
				AllKeyboardShortcuts.Clear();

				foreach (var command in Commands)
				{
					var defaultKeys = command.DefaultHotKeys;

					if (command is NoneCommand)
						continue;

					AllKeyboardShortcuts.Add(new()
					{
						CommandCode = command.Code,
						Label = command.Label,
						Description = command.Description,
						HotKey = new(),
					});

					foreach (var hotkey in command.HotKeys)
					{
						// Don't show mouse hotkeys for now because no editor provided for mouse input as of now
						if (!hotkey.IsVisible || hotkey.Key == Keys.Mouse4 || hotkey.Key == Keys.Mouse5)
							continue;

						ValidKeyboardShortcuts.Add(new()
						{
							CommandCode = command.Code,
							Label = command.Label,
							Description = command.Description,
							HotKey = hotkey,
							DefaultHotKeyCollection = defaultKeys,
							PreviousHotKey = hotkey,
							IsDefinedByDefault = defaultKeys.Contains(hotkey),
						});
					}
				}
			});
		}

		private void ExecuteShowAddNewHotKeySectionCommand()
		{
			ShowAddNewHotKeySection = true;

			// Reset edit mode of every item
			foreach (var hotkey in ValidKeyboardShortcuts)
			{
				hotkey.IsEditMode = false;
				hotkey.HotKeyText = hotkey.HotKey.LocalizedLabel;
			}
		}

		private void ExecuteHideAddNewHotKeySectionCommand()
		{
			ShowAddNewHotKeySection = false;

			if (SelectedNewShortcutItem is null)
				return;

			SelectedNewShortcutItem.HotKeyText = "";
			SelectedNewShortcutItem = null;
		}

		private void ExecuteAddNewHotKeyCommand()
		{
			if (SelectedNewShortcutItem is null)
				return;

			// Check if this hot key is already taken
			foreach (var hotkey in ValidKeyboardShortcuts)
			{
				if (SelectedNewShortcutItem.HotKeyText == hotkey.PreviousHotKey)
				{
					IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			var actions =
				ActionsSettingsService.Actions is not null
					? new List<ActionWithCustomArgItem>(ActionsSettingsService.Actions)
					: [];

			// Initialize the new key binding
			var newHotKey = HotKey.Parse(SelectedNewShortcutItem.HotKeyText);

			// Add to the temporary modifiable collection
			actions.Add(new(SelectedNewShortcutItem.CommandCode, newHotKey.RawLabel));

			// Set to the user settings
			ActionsSettingsService.Actions = actions;

			// Set as customized
			foreach (var action in ValidKeyboardShortcuts)
			{
				if (action.CommandCode == SelectedNewShortcutItem.CommandCode)
					action.IsDefinedByDefault = SelectedNewShortcutItem.DefaultHotKeyCollection.Contains(action.HotKey);
			}

			// Create a clone
			var selectedNewItem = new ModifiableCommandHotKeyItem()
			{
				CommandCode = SelectedNewShortcutItem.CommandCode,
				Label = SelectedNewShortcutItem.Label,
				Description = SelectedNewShortcutItem.Description,
				HotKey = HotKey.Parse(SelectedNewShortcutItem.HotKeyText),
				DefaultHotKeyCollection = new(SelectedNewShortcutItem.DefaultHotKeyCollection),
				PreviousHotKey = HotKey.Parse(SelectedNewShortcutItem.PreviousHotKey.RawLabel),
			};

			// Exit edit mode
			SelectedNewShortcutItem.PreviousHotKey = newHotKey;
			SelectedNewShortcutItem.HotKey = newHotKey;
			SelectedNewShortcutItem.IsEditMode = false;
			SelectedNewShortcutItem.IsDefinedByDefault = SelectedNewShortcutItem.DefaultHotKeyCollection.Select(x => x.RawLabel).Contains(newHotKey.RawLabel);

			// Add to existing list
			ValidKeyboardShortcuts.Insert(0, selectedNewItem);
		}

		private void ExecuteShowRestoreDefaultsConfirmationCommand()
		{
			IsResetAllConfirmationTeachingTipOpened = true;
		}

		private void ExecuteRestoreDefaultsCommand()
		{
			ActionsSettingsService.Actions = null;
			IsResetAllConfirmationTeachingTipOpened = false;

			_ = ExecuteLoadCommandsCommand();
		}

		private void ExecuteEditCommand(ModifiableCommandHotKeyItem? item)
		{
			if (item is null)
				return;

			// Hide the add command grid
			ShowAddNewHotKeySection = false;

			// Clear the selected item
			if (SelectedNewShortcutItem is not null)
			{
				SelectedNewShortcutItem.HotKeyText = "";
				SelectedNewShortcutItem = null;
			}

			// Reset edit mode of every item
			foreach (var hotkey in ValidKeyboardShortcuts)
			{
				hotkey.IsEditMode = false;
				hotkey.HotKeyText = hotkey.HotKey.LocalizedLabel;
			}

			// Enter edit mode for the item
			item.IsEditMode = true;
		}

		private void ExecuteSaveCommand(ModifiableCommandHotKeyItem? item)
		{
			if (item is null)
				return;

			if (item.HotKeyText == item.PreviousHotKey.LocalizedLabel)
			{
				item.IsEditMode = false;
				return;
			}

			// Check if this hot key is already taken
			foreach (var hotkey in ValidKeyboardShortcuts)
			{
				if (item.HotKeyText == hotkey.PreviousHotKey)
				{
					IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			// Get clone of customized hotkeys to overwrite
			var actions =
				ActionsSettingsService.Actions is not null
					? new List<ActionWithCustomArgItem>(ActionsSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindingWithArgs = actions.Find(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousHotKey.RawLabel);

			// Initialize
			var newHotKey = HotKey.Parse(item.HotKeyText);
			var modifiedCollection = HotKeyCollection.Empty;
			List<HotKey> modifiableCollection = [];

			if (item.IsDefinedByDefault && storedKeyBindingWithArgs is null)
			{
				// Any keys associated to the command is not customized at all
				foreach (var defaultKey in item.DefaultHotKeyCollection)
				{
					if (defaultKey.RawLabel != item.PreviousHotKey.RawLabel)
						actions.Add(new(item.CommandCode, defaultKey.RawLabel));
				}
			}
			else
			{
				var storedKey = actions.Find(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousHotKey.RawLabel);

				if (storedKey is not null)
					storedKey.KeyBinding = newHotKey.RawLabel;
			}

			// Set to the user settings
			ActionsSettingsService.Actions = actions;

			// Set as customized
			foreach (var action in ValidKeyboardShortcuts)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsDefinedByDefault = item.DefaultHotKeyCollection.Contains(action.HotKey);
			}

			// Exit edit mode
			item.PreviousHotKey = newHotKey;
			item.HotKey = newHotKey;
			item.IsEditMode = false;
		}

		private void ExecuteCancelCommand(ModifiableCommandHotKeyItem? item)
		{
			if (item is null)
				return;

			item.IsEditMode = false;
			item.HotKeyText = item.HotKey.LocalizedLabel;
		}

		private void ExecuteDeleteCommand(ModifiableCommandHotKeyItem? item)
		{
			if (item is null)
				return;

			// Get clone of customized hotkeys to overwrite
			var actions =
				ActionsSettingsService.Actions is not null
					? new List<ActionWithCustomArgItem>(ActionsSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindingWithArgs = actions.Find(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousHotKey.RawLabel);

			// Initialize
			var newHotKey = HotKey.Parse(item.HotKeyText);
			var modifiedCollection = HotKeyCollection.Empty;
			List<HotKey> modifiableCollection = [];

			if (item.IsDefinedByDefault && storedKeyBindingWithArgs is null)
			{
				// Any keys associated to the command is not customized at all
				foreach (var defaultKey in item.DefaultHotKeyCollection)
				{
					if (defaultKey.RawLabel != item.PreviousHotKey.RawLabel)
						actions.Add(new(item.CommandCode, defaultKey.RawLabel));
				}
			}
			else
			{
				actions.RemoveAll(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousHotKey.RawLabel);
			}

			// Set to the user settings
			ActionsSettingsService.Actions = actions;

			// Set as customized
			foreach (var action in ValidKeyboardShortcuts)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsDefinedByDefault = item.DefaultHotKeyCollection.Contains(action.HotKey);
			}

			// Exit edit mode
			item.PreviousHotKey = newHotKey;
			item.HotKey = newHotKey;
			item.IsEditMode = false;
		}
	}
}
