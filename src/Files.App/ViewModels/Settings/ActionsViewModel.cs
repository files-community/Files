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
							IsDefaultKey = defaultKeys.Contains(hotkey),
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

			// Get raw string keys stored in the user setting
			var storedKeys = actions.Find(x => x.CommandCode == SelectedNewShortcutItem.CommandCode);

			// Initialize
			var newHotKey = HotKey.Parse(SelectedNewShortcutItem.HotKeyText);
			var modifiedCollection = HotKeyCollection.Empty;

			// The first time to customize
			if (storedKeys is null)
			{
				// Replace with new one
				var modifiableDefaultCollection = SelectedNewShortcutItem.DefaultHotKeyCollection.ToList();
				modifiableDefaultCollection.RemoveAll(x => x.RawLabel == SelectedNewShortcutItem.PreviousHotKey.RawLabel);
				modifiableDefaultCollection.Add(newHotKey);
				modifiedCollection = new HotKeyCollection(modifiableDefaultCollection);
			}
			// Stored in the user setting
			else
			{
				// Replace with new one
				var modifiableCollection = HotKeyCollection.Parse(storedKeys?.KeyBinding ?? string.Empty).ToList();
				modifiableCollection.RemoveAll(x => x.RawLabel == SelectedNewShortcutItem.PreviousHotKey.RawLabel);
				modifiableCollection.Add(newHotKey);
				modifiedCollection = new HotKeyCollection(modifiableCollection);
			}

			// Remove previous one and add new one
			actions.RemoveAll(x => x.CommandCode == SelectedNewShortcutItem.CommandCode);
			actions.Add(new(SelectedNewShortcutItem.CommandCode, modifiedCollection.RawLabel));

			// Store
			ActionsSettingsService.Actions = actions;

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

			// Hide the section
			ShowAddNewHotKeySection = false;

			// Remove from excluded list and set null
			SelectedNewShortcutItem.HotKeyText = "";
			SelectedNewShortcutItem = null;

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

			// Reset the selected item's info
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
			var storedKeys = actions.Find(x => x.CommandCode == item.CommandCode);

			// Initialize
			var newHotKey = HotKey.Parse(item.HotKeyText);
			var modifiedCollection = HotKeyCollection.Empty;

			if (item.IsDefaultKey)
			{
				// The first time to customize in the user setting
				if (storedKeys is null)
				{
					// Replace with new one
					var modifiableDefaultCollection = item.DefaultHotKeyCollection.ToList();
					modifiableDefaultCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiableDefaultCollection.Add(newHotKey);
					modifiedCollection = new HotKeyCollection(modifiableDefaultCollection);
				}
				// Stored in the user setting
				else
				{
					// Replace with new one
					var modifiableCollection = HotKeyCollection.Parse(storedKeys.KeyBinding).ToList();
					modifiableCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiableCollection.Add(newHotKey);
					modifiedCollection = new HotKeyCollection(modifiableCollection);
				}

				// Store
				actions.Add(new(item.CommandCode, modifiedCollection.RawLabel));
				ActionsSettingsService.Actions = actions;

				// Update visual
				item.PreviousHotKey = newHotKey;
				item.HotKey = newHotKey;

				// Set as customized
				foreach (var action in ValidKeyboardShortcuts)
				{
					if (action.CommandCode == item.CommandCode)
						action.IsDefaultKey = item.DefaultHotKeyCollection.Contains(action.HotKey);
				}

				// Exit edit mode
				item.IsEditMode = false;

				return;
			}
			else
			{
				// Remove existing setting
				var modifiableCollection = HotKeyCollection.Parse(storedKeys?.KeyBinding ?? string.Empty).ToList();
				if (modifiableCollection.Contains(newHotKey))
					return;
				modifiableCollection.Add(HotKey.Parse(item.HotKeyText));
				modifiedCollection = new(modifiableCollection);

				// Remove previous one
				actions.RemoveAll(x => x.CommandCode == item.CommandCode);

				// Add new one
				if (modifiedCollection.Select(x => x.RawLabel).SequenceEqual(item.DefaultHotKeyCollection.Select(x => x.RawLabel)))
					actions.Add(new(item.CommandCode, modifiedCollection.RawLabel));

				// Save
				ActionsSettingsService.Actions = actions;

				// Update visual
				item.PreviousHotKey = newHotKey;
				item.HotKey = newHotKey;

				// Set as customized
				foreach (var action in ValidKeyboardShortcuts)
				{
					if (action.CommandCode == item.CommandCode)
						action.IsDefaultKey = item.DefaultHotKeyCollection.Contains(action.HotKey);
				}

				// Exit edit mode
				item.IsEditMode = false;
			}
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
			var storedKeys = actions.Find(x => x.CommandCode == item.CommandCode);

			// Initialize
			var modifiedCollection = HotKeyCollection.Empty;

			if (item.IsDefaultKey)
			{
				// The first time to customize in the user setting
				if (storedKeys is null)
				{
					// Replace with new one
					var modifiableDefaultCollection = item.DefaultHotKeyCollection.ToList();
					modifiableDefaultCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiedCollection = new HotKeyCollection(modifiableDefaultCollection);
				}
				// Stored in the user setting
				else
				{
					// Replace with new one
					var modifiableCollection = HotKeyCollection.Parse(storedKeys.KeyBinding).ToList();
					modifiableCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiedCollection = new HotKeyCollection(modifiableCollection);
				}

				// Remove previous one and add new one
				actions.RemoveAll(x => x.CommandCode == item.CommandCode);
				actions.Add(new(item.CommandCode, modifiedCollection.RawLabel));

				// Store
				ActionsSettingsService.Actions = actions;

				// Exit
				item.IsEditMode = false;
				ValidKeyboardShortcuts.Remove(item);

				return;
			}
			else
			{
				// Remove existing setting
				var modifiableCollection = HotKeyCollection.Parse(storedKeys?.KeyBinding ?? string.Empty).ToList();
				modifiableCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel || x.RawLabel == $"!{item.PreviousHotKey.RawLabel}");
				modifiedCollection = new(modifiableCollection);

				// Remove previous
				actions.RemoveAll(x => x.CommandCode == item.CommandCode);

				if (modifiedCollection.LocalizedLabel != string.Empty)
					actions.Add(new(item.CommandCode, modifiedCollection.RawLabel));

				// Save
				ActionsSettingsService.Actions = actions;

				// Exit
				item.IsEditMode = false;
				ValidKeyboardShortcuts.Remove(item);

				return;
			}
		}
	}
}
