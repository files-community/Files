// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Actions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Immutable;
using static Files.App.Data.Commands.CommandManager;

namespace Files.App.Data.Commands
{
	[DebuggerDisplay("Command {Code} (Modifiable)")]
	internal class ModifiableCommand : ObservableObject, IRichCommand
	{
		public event EventHandler? CanExecuteChanged;

		private IRichCommand BaseCommand;
		private ImmutableDictionary<KeyModifiers, IRichCommand> ModifiedCommands;

		public CommandCodes Code => BaseCommand.Code;

		public string Label => BaseCommand.Label;
		public string LabelWithHotKey => BaseCommand.LabelWithHotKey;
		public string AutomationName => BaseCommand.AutomationName;

		public string Description => BaseCommand.Description;

		public RichGlyph Glyph => BaseCommand.Glyph;
		public object? Icon => BaseCommand.Icon;
		public FontIcon? FontIcon => BaseCommand.FontIcon;
		public Style? OpacityStyle => BaseCommand.OpacityStyle;

		public bool IsCustomHotKeys => BaseCommand.IsCustomHotKeys;
		public string? HotKeyText => BaseCommand.HotKeyText;

		public HotKeyCollection HotKeys
		{
			get => BaseCommand.HotKeys;
			set => BaseCommand.HotKeys = value;
		}

		public bool IsToggle => BaseCommand.IsToggle;

		public bool IsOn
		{
			get => BaseCommand.IsOn;
			set => BaseCommand.IsOn = value;
		}

		public bool IsExecutable => BaseCommand.IsExecutable;

		public ModifiableCommand(IRichCommand baseCommand, Dictionary<KeyModifiers, IRichCommand> modifiedCommands)
		{
			BaseCommand = baseCommand;
			ModifiedCommands = modifiedCommands.ToImmutableDictionary();

			if (baseCommand is ActionCommand actionCommand)
			{
				if (actionCommand.Action is INotifyPropertyChanging notifyPropertyChanging)
					notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
				if (actionCommand.Action is INotifyPropertyChanged notifyPropertyChanged)
					notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
			}
		}

		public bool CanExecute(object? parameter) => BaseCommand.CanExecute(parameter);
		public async void Execute(object? parameter) => await ExecuteAsync();

		public Task ExecuteAsync()
		{
			if (ModifiedCommands.TryGetValue(HotKeyHelpers.GetCurrentKeyModifiers(), out var modifiedCommand) &&
				modifiedCommand.IsExecutable)
				return modifiedCommand.ExecuteAsync();
			else
				return BaseCommand.ExecuteAsync();
		}

		public async void ExecuteTapped(object sender, TappedRoutedEventArgs e) => await ExecuteAsync();

		public void ResetHotKeys() => BaseCommand.ResetHotKeys();

		private void Action_PropertyChanging(object? sender, PropertyChangingEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanging(nameof(Label));
					OnPropertyChanging(nameof(LabelWithHotKey));
					OnPropertyChanging(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanging(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanging(nameof(IsExecutable));
					break;
			}
		}
		private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanged(nameof(Label));
					OnPropertyChanged(nameof(LabelWithHotKey));
					OnPropertyChanged(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanged(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanged(nameof(IsExecutable));
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					break;
			}
		}
	}
}
