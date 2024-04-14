// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents view model of <see cref="Views.Settings.ActionsPage"/>.
	/// </summary>
	public sealed class ActionsViewModel : ObservableObject
	{
		// Dependency injections

		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();
		private ICommandManager CommandManager { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Properties

		public ObservableCollection<ModifiableActionItem> ValidActionItems { get; } = [];
		public ObservableCollection<ModifiableActionItem> AllActionItems { get; } = [];

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

		private bool _ShowAddNewKeyBindingBlock;
		public bool ShowAddNewKeyBindingBlock
		{
			get => _ShowAddNewKeyBindingBlock;
			set => SetProperty(ref _ShowAddNewKeyBindingBlock, value);
		}

		private ModifiableActionItem? _SelectedActionItem;
		public ModifiableActionItem? SelectedActionItem
		{
			get => _SelectedActionItem;
			set => SetProperty(ref _SelectedActionItem, value);
		}

		// Commands

		public ICommand LoadAllActionsCommand { get; set; }
		public ICommand ShowAddNewKeyBindingBlockCommand { get; set; }
		public ICommand HideAddNewKeyBindingBlockCommand { get; set; }
		public ICommand AddNewKeyBindingCommand { get; set; }
		public ICommand ShowRestoreDefaultsConfirmationCommand { get; set; }
		public ICommand RestoreDefaultsCommand { get; set; }
		public ICommand EditCommand { get; set; }
		public ICommand SaveCommand { get; set; }
		public ICommand CancelCommand { get; set; }
		public ICommand DeleteCommand { get; set; }

		// Constructor

		public ActionsViewModel()
		{
			LoadAllActionsCommand = new AsyncRelayCommand(ExecuteLoadAllActionsCommand);
			ShowAddNewKeyBindingBlockCommand = new RelayCommand(ExecuteShowAddNewKeyBindingBlockCommand);
			HideAddNewKeyBindingBlockCommand = new RelayCommand(ExecuteHideAddNewKeyBindingBlockCommand);
			AddNewKeyBindingCommand = new RelayCommand(ExecuteAddNewKeyBindingCommand);
			ShowRestoreDefaultsConfirmationCommand = new RelayCommand(ExecuteShowRestoreDefaultsConfirmationCommand);
			RestoreDefaultsCommand = new RelayCommand(ExecuteRestoreDefaultsCommand);
			EditCommand = new RelayCommand<ModifiableActionItem>(ExecuteEditCommand);
			SaveCommand = new RelayCommand<ModifiableActionItem>(ExecuteSaveCommand);
			CancelCommand = new RelayCommand<ModifiableActionItem>(ExecuteCancelCommand);
			DeleteCommand = new RelayCommand<ModifiableActionItem>(ExecuteDeleteCommand);
		}

		// Command methods

		private async Task ExecuteLoadAllActionsCommand()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				ValidActionItems.Clear();
				AllActionItems.Clear();

				foreach (var command in CommandManager)
				{
					var defaultKeys = command.DefaultHotKeys;

					if (command is NoneCommand)
						continue;

					AllActionItems.Add(new()
					{
						CommandCode = command.Code,
						CommandLabel = command.Label,
						CommandDescription = command.Description,
						KeyBinding = new(),
						DefaultKeyBindings = defaultKeys,
					});

					foreach (var hotkey in command.HotKeys)
					{
						// Don't show mouse hotkeys for now because no editor provided for mouse input as of now
						if (!hotkey.IsVisible || hotkey.Key == Keys.Mouse4 || hotkey.Key == Keys.Mouse5)
							continue;

						ValidActionItems.Add(new()
						{
							CommandCode = command.Code,
							CommandLabel = command.Label,
							CommandDescription = command.Description,
							KeyBinding = hotkey,
							DefaultKeyBindings = defaultKeys,
							PreviousKeyBinding = hotkey,
							IsDefinedByDefault = defaultKeys.Contains(hotkey),
						});
					}
				}
			});
		}

		private void ExecuteShowAddNewKeyBindingBlockCommand()
		{
			ShowAddNewKeyBindingBlock = true;

			// Reset edit mode of every item
			foreach (var hotkey in ValidActionItems)
			{
				hotkey.IsInEditMode = false;
				hotkey.LocalizedKeyBindingLabel = hotkey.KeyBinding.LocalizedLabel;
			}
		}

		private void ExecuteHideAddNewKeyBindingBlockCommand()
		{
			ShowAddNewKeyBindingBlock = false;

			if (SelectedActionItem is null)
				return;

			SelectedActionItem.LocalizedKeyBindingLabel = "";
			SelectedActionItem = null;
		}

		private void ExecuteAddNewKeyBindingCommand()
		{
			if (SelectedActionItem is null)
				return;

			// Initialize the new key binding
			var newHotKey = HotKey.Parse(SelectedActionItem.LocalizedKeyBindingLabel);

			// Check if this hot key is already taken
			foreach (var hotkey in ValidActionItems)
			{
				if (newHotKey.RawLabel == hotkey.KeyBinding.RawLabel)
				{
					IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			var actions =
				ActionsSettingsService.Actions is not null
					? new List<ActionWithParameterItem>(ActionsSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindingWithArgs = actions.Find(x => x.CommandCode == SelectedActionItem.CommandCode && x.KeyBinding == SelectedActionItem.PreviousKeyBinding.RawLabel);

			if (storedKeyBindingWithArgs == null)
			{
				// Any keys associated to the command is not customized at all
				foreach (var defaultKey in SelectedActionItem.DefaultKeyBindings)
					actions.Add(new(SelectedActionItem.CommandCode, defaultKey.RawLabel));
			}

			// Add to the temporary modifiable collection
			actions.Add(new(SelectedActionItem.CommandCode, newHotKey.RawLabel));

			// Set to the user settings
			ActionsSettingsService.Actions = actions;

			// Set as customized
			foreach (var action in ValidActionItems)
			{
				if (action.CommandCode == SelectedActionItem.CommandCode)
					action.IsDefinedByDefault = SelectedActionItem.DefaultKeyBindings.Contains(action.KeyBinding);
			}

			// Create a clone
			var selectedNewItem = new ModifiableActionItem()
			{
				CommandCode = SelectedActionItem.CommandCode,
				CommandLabel = SelectedActionItem.CommandLabel,
				CommandDescription = SelectedActionItem.CommandDescription,
				KeyBinding = HotKey.Parse(SelectedActionItem.LocalizedKeyBindingLabel),
				DefaultKeyBindings = new(SelectedActionItem.DefaultKeyBindings),
				PreviousKeyBinding = HotKey.Parse(SelectedActionItem.PreviousKeyBinding.RawLabel),
			};

			// Exit edit mode
			ShowAddNewKeyBindingBlock = false;
			SelectedActionItem.LocalizedKeyBindingLabel = string.Empty;
			SelectedActionItem = null;

			// Add to existing list
			ValidActionItems.Insert(0, selectedNewItem);
		}

		private void ExecuteShowRestoreDefaultsConfirmationCommand()
		{
			IsResetAllConfirmationTeachingTipOpened = true;
		}

		private void ExecuteRestoreDefaultsCommand()
		{
			ActionsSettingsService.Actions = null;
			IsResetAllConfirmationTeachingTipOpened = false;

			_ = ExecuteLoadAllActionsCommand();
		}

		private void ExecuteEditCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			// Hide the add command grid
			ShowAddNewKeyBindingBlock = false;

			// Clear the selected item
			if (SelectedActionItem is not null)
			{
				SelectedActionItem.LocalizedKeyBindingLabel = "";
				SelectedActionItem = null;
			}

			// Reset edit mode of every item
			foreach (var hotkey in ValidActionItems)
			{
				hotkey.IsInEditMode = false;
				hotkey.LocalizedKeyBindingLabel = hotkey.KeyBinding.LocalizedLabel;
			}

			// Enter edit mode for the item
			item.IsInEditMode = true;
		}

		private void ExecuteSaveCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			if (item.LocalizedKeyBindingLabel == item.PreviousKeyBinding.LocalizedLabel)
			{
				item.IsInEditMode = false;
				return;
			}

			// Check if this hot key is already taken
			foreach (var hotkey in ValidActionItems)
			{
				if (item.LocalizedKeyBindingLabel == hotkey.PreviousKeyBinding)
				{
					IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			// Get clone of customized hotkeys to overwrite
			var actions =
				ActionsSettingsService.Actions is not null
					? new List<ActionWithParameterItem>(ActionsSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindingWithArgs = actions.Find(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousKeyBinding.RawLabel);

			// Initialize
			var newHotKey = HotKey.Parse(item.LocalizedKeyBindingLabel);

			if (item.IsDefinedByDefault && storedKeyBindingWithArgs is null)
			{
				// Any keys associated to the command is not customized at all
				foreach (var defaultKey in item.DefaultKeyBindings)
				{
					if (defaultKey.RawLabel != item.PreviousKeyBinding.RawLabel)
						actions.Add(new(item.CommandCode, defaultKey.RawLabel));
				}
			}
			else
			{
				var storedKey = actions.Find(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousKeyBinding.RawLabel);
				if (storedKey is not null)
					storedKey.KeyBinding = newHotKey.RawLabel;
			}

			// Set to the user settings
			ActionsSettingsService.Actions = actions;

			// Set as customized
			foreach (var action in ValidActionItems)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsDefinedByDefault = item.DefaultKeyBindings.Contains(action.KeyBinding);
			}

			// Exit edit mode
			item.PreviousKeyBinding = newHotKey;
			item.KeyBinding = newHotKey;
			item.IsInEditMode = false;
		}

		private void ExecuteCancelCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			item.IsInEditMode = false;
			item.LocalizedKeyBindingLabel = item.KeyBinding.LocalizedLabel;
		}

		private void ExecuteDeleteCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			// Get clone of customized hotkeys to overwrite
			var actions =
				ActionsSettingsService.Actions is not null
					? new List<ActionWithParameterItem>(ActionsSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindingWithArgs = actions.Find(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousKeyBinding.RawLabel);

			// Initialize
			var newHotKey = HotKey.Parse(item.LocalizedKeyBindingLabel);

			if (item.IsDefinedByDefault && storedKeyBindingWithArgs is null)
			{
				// Any keys associated to the command is not customized at all
				foreach (var defaultKey in item.DefaultKeyBindings)
				{
					if (defaultKey.RawLabel != item.PreviousKeyBinding.RawLabel)
						actions.Add(new(item.CommandCode, defaultKey.RawLabel));
				}
			}
			else
			{
				actions.RemoveAll(x => x.CommandCode == item.CommandCode && x.KeyBinding == item.PreviousKeyBinding.RawLabel);
			}

			// Set to the user settings
			ActionsSettingsService.Actions = actions;

			// Set as customized
			foreach (var action in ValidActionItems)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsDefinedByDefault = item.DefaultKeyBindings.Contains(action.KeyBinding);
			}

			// Exit edit mode
			item.PreviousKeyBinding = newHotKey;
			item.KeyBinding = newHotKey;
			item.IsInEditMode = false;
		}
	}
}
