// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Services.Settings;
using System.Linq;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public class ActionsViewModel : ObservableObject
	{
		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public ObservableCollection<ModifiableCommandHotKeyItem> KeyboardShortcuts { get; } = [];
		public ObservableCollection<ModifiableCommandHotKeyItem> ExcludedKeyboardShortcuts { get; } = [];

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

		private bool _ShowAddNewShortcutGrid;
		public bool ShowAddNewShortcutGrid
		{
			get => _ShowAddNewShortcutGrid;
			set => SetProperty(ref _ShowAddNewShortcutGrid, value);
		}

		private ModifiableCommandHotKeyItem? _SelectedNewShortcutItem;
		public ModifiableCommandHotKeyItem? SelectedNewShortcutItem
		{
			get => _SelectedNewShortcutItem;
			set => SetProperty(ref _SelectedNewShortcutItem, value);
		}

		public ICommand LoadCommandsCommand { get; set; }
		public ICommand ShowResetAllConfirmationCommand { get; set; }
		public ICommand ShowAddNewShortcutGridCommand { get; set; }
		public ICommand HideAddNewShortcutGridCommand { get; set; }
		public ICommand AddNewShortcutGridCommand { get; set; }
		public ICommand ResetAllCommand { get; set; }

		public ActionsViewModel()
		{
			LoadCommandsCommand = new AsyncRelayCommand(LoadCommands);
			ShowResetAllConfirmationCommand = new RelayCommand(ExecuteShowResetAllConfirmationCommand);
			ShowAddNewShortcutGridCommand = new RelayCommand(ExecuteShowAddNewShortcutGridCommand);
			HideAddNewShortcutGridCommand = new RelayCommand(ExecuteHideAddNewShortcutGridCommand);
			AddNewShortcutGridCommand = new RelayCommand(ExecuteAddNewShortcutGridCommand);
			ResetAllCommand = new RelayCommand(ExecuteResetAllCommand);
		}

		private async Task LoadCommands()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				KeyboardShortcuts.Clear();
				ExcludedKeyboardShortcuts.Clear();

				foreach (var command in Commands)
				{
					var defaultKeys = command.DefaultHotKeys;

					if (command is NoneCommand)
						continue;

					if (command.HotKeys.IsEmpty)
					{
						ExcludedKeyboardShortcuts.Add(new()
						{
							CommandCode = command.Code,
							Label = command.Label,
							Description = command.Description,
							HotKey = new(),
						});

						continue;
					}

					foreach (var hotkey in command.HotKeys)
					{
						if (!hotkey.IsVisible)
							continue;

						KeyboardShortcuts.Add(new()
						{
							CommandCode = command.Code,
							Label = command.Label,
							Description = command.Description,
							HotKey = hotkey,
							DefaultHotKeyCollection = defaultKeys,
							PreviousHotKey = hotkey,
							IsCustomized = !defaultKeys.Contains(hotkey),
						});
					}
				}
			});
		}

		private void ExecuteShowResetAllConfirmationCommand()
		{
			IsResetAllConfirmationTeachingTipOpened = true;
		}

		private void ExecuteShowAddNewShortcutGridCommand()
		{
			ShowAddNewShortcutGrid = true;

			// Reset edit mode for each item
			foreach (var hotkey in KeyboardShortcuts)
			{
				hotkey.IsEditMode = false;
				hotkey.HotKeyText = hotkey.HotKey.Label;
			}
		}

		private void ExecuteHideAddNewShortcutGridCommand()
		{
			ShowAddNewShortcutGrid = false;

			if (SelectedNewShortcutItem is null)
				return;

			SelectedNewShortcutItem.HotKeyText = "";
			SelectedNewShortcutItem = null;
		}

		private void ExecuteAddNewShortcutGridCommand()
		{
			if (SelectedNewShortcutItem is null)
				return;

			var actions =
				GeneralSettingsService.Actions is not null
					? new Dictionary<string, string>(GeneralSettingsService.Actions)
					: [];

			// Remove existing setting
			foreach (var action in actions)
			{
				if (Enum.TryParse(action.Key, true, out CommandCodes code) && code == SelectedNewShortcutItem.CommandCode)
					actions.Remove(action.Key);
			}

			// Create a new one
			actions.Add(SelectedNewShortcutItem.CommandCode.ToString(), SelectedNewShortcutItem.HotKeyText);

			// Set
			SelectedNewShortcutItem.HotKey = HotKey.Parse(SelectedNewShortcutItem.HotKeyText);
			GeneralSettingsService.Actions = actions;

			// Create a clone
			var selectedNewItem = new ModifiableCommandHotKeyItem()
			{
				CommandCode = SelectedNewShortcutItem.CommandCode,
				Label = SelectedNewShortcutItem.Label,
				Description = SelectedNewShortcutItem.Description,
				HotKey = HotKey.Parse(SelectedNewShortcutItem.HotKey.Code),
				DefaultHotKeyCollection = new(SelectedNewShortcutItem.DefaultHotKeyCollection),
				PreviousHotKey = HotKey.Parse(SelectedNewShortcutItem.PreviousHotKey.Code),
			};

			// Hide the grid
			ShowAddNewShortcutGrid = false;

			// Remove from excluded list and set null
			SelectedNewShortcutItem.HotKeyText = "";
			SelectedNewShortcutItem = null;

			// Add to existing list
			KeyboardShortcuts.Insert(0, selectedNewItem);
		}

		private void ExecuteResetAllCommand()
		{
			GeneralSettingsService.Actions = null;
			IsResetAllConfirmationTeachingTipOpened = false;

			_ = LoadCommands();
		}
	}
}
