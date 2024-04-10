// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed class ActionsViewModel : ObservableObject
	{
		// Dependency injections

		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
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

		// Constructor

		public ActionsViewModel()
		{
			LoadCommandsCommand = new AsyncRelayCommand(ExecuteLoadCommandsCommand);
			ShowAddNewHotKeySectionCommand = new RelayCommand(ExecuteShowAddNewHotKeySectionCommand);
			HideAddNewHotKeySectionCommand = new RelayCommand(ExecuteHideAddNewHotKeySectionCommand);
			AddNewHotKeyCommand = new RelayCommand(ExecuteAddNewHotKeyCommand);
			ShowRestoreDefaultsConfirmationCommand = new RelayCommand(ExecuteShowRestoreDefaultsConfirmationCommand);
			RestoreDefaultsCommand = new RelayCommand(ExecuteRestoreDefaultsCommand);
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
				GeneralSettingsService.Actions is not null
					? new Dictionary<string, string>(GeneralSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeys = actions.GetValueOrDefault(SelectedNewShortcutItem.CommandCode.ToString());

			// Initialize
			var newHotKey = HotKey.Parse(SelectedNewShortcutItem.HotKeyText);
			var modifiedCollection = HotKeyCollection.Empty;

			// The first time to customize
			if (string.IsNullOrEmpty(storedKeys))
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
				var modifiableCollection = HotKeyCollection.Parse(storedKeys).ToList();
				modifiableCollection.RemoveAll(x => x.RawLabel == SelectedNewShortcutItem.PreviousHotKey.RawLabel);
				modifiableCollection.Add(newHotKey);
				modifiedCollection = new HotKeyCollection(modifiableCollection);
			}

			// Remove previous one and add new one
			actions.Remove(SelectedNewShortcutItem.CommandCode.ToString());
			actions.Add(SelectedNewShortcutItem.CommandCode.ToString(), modifiedCollection.RawLabel);

			// Store
			GeneralSettingsService.Actions = actions;

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
			GeneralSettingsService.Actions = null;
			IsResetAllConfirmationTeachingTipOpened = false;

			_ = ExecuteLoadCommandsCommand();
		}
	}
}
