using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Actions;
using Files.App.DataModels;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.Commands
{
	internal class CommandManager : ICommandManager
	{
		private readonly IImmutableDictionary<CommandCodes, IHotKeyCommand> commands = new List<IAction>
		{
			new HelpAction(),
			new FullScreenAction(),
			new ShowHiddenItemsAction(),
			new ShowFileExtensionsAction(),
			new LayoutDetailsAction(),
			new LayoutTilesAction(),
			new LayoutGridSmallAction(),
			new LayoutGridMediumAction(),
			new LayoutGridLargeAction(),
			new LayoutColumnsAction(),
			new LayoutAdaptiveAction(),
			new MultiSelectAction(),
			new SelectAllAction(),
			new InvertSelectionAction(),
			new ClearSelectionAction(),
			new OpenFolderInNewTabAction(),
			new RenameAction(),
			new PropertiesAction(),
		}
		.Select(action => new ActionCommand(action))
		.Cast<IHotKeyCommand>()
		.Append(new NoneCommand())
		.ToImmutableDictionary(action => action.Code);

		public IRichCommand this[CommandCodes commandCode]
			=> commands.TryGetValue(commandCode, out var command) ? command : None;

		public IRichCommand None => commands[CommandCodes.None];
		public IRichCommand Help => commands[CommandCodes.Help];
		public IRichCommand FullScreen => commands[CommandCodes.FullScreen];
		public IRichCommand ShowHiddenItems => commands[CommandCodes.ShowHiddenItems];
		public IRichCommand ShowFileExtensions => commands[CommandCodes.ShowFileExtensions];
		public IRichCommand LayoutDetails => commands[CommandCodes.LayoutDetails];
		public IRichCommand LayoutTiles => commands[CommandCodes.LayoutTiles];
		public IRichCommand LayoutGridSmall => commands[CommandCodes.LayoutGridSmall];
		public IRichCommand LayoutGridMedium => commands[CommandCodes.LayoutGridMedium];
		public IRichCommand LayoutGridLarge => commands[CommandCodes.LayoutGridLarge];
		public IRichCommand LayoutColumns => commands[CommandCodes.LayoutColumns];
		public IRichCommand LayoutAdaptive => commands[CommandCodes.LayoutAdaptive];
		public IRichCommand MultiSelect => commands[CommandCodes.MultiSelect];
		public IRichCommand SelectAll => commands[CommandCodes.SelectAll];
		public IRichCommand InvertSelection => commands[CommandCodes.InvertSelection];
		public IRichCommand ClearSelection => commands[CommandCodes.ClearSelection];
		public IRichCommand OpenFolderInNewTab => commands[CommandCodes.OpenFolderInNewTab];
		public IRichCommand Rename => commands[CommandCodes.Rename];
		public IRichCommand Properties => commands[CommandCodes.Properties];

		public CommandManager()
		{
			var hotKeyManager = Ioc.Default.GetService<IHotKeyManager>();
			if (hotKeyManager is null)
				return;

			if (hotKeyManager is HotKeyManager manager)
			{
				var hotKeys = commands.Values
					.Where(command => !command.DefaultHotKey.IsNone)
					.ToDictionary(command => command.DefaultHotKey, command => command.Code);
				manager.Initialize(hotKeys);
			}

			hotKeyManager.HotKeyChanged += HotKeyManager_HotKeyChanged;

			var commandCodes = Enum.GetValues<CommandCodes>();
			foreach (CommandCodes commandCode in commandCodes)
			{
				var command = commands[commandCode];
				var userHotKey = hotKeyManager[commandCode];
				if (userHotKey != command.UserHotKey)
					command.InitializeUserHotKey(userHotKey);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => commands.Values.GetEnumerator();

		private void HotKeyManager_HotKeyChanged(IHotKeyManager manager, HotKeyChangedEventArgs e)
		{
			commands[e.OldCommandCode].UpdateUserHotKey(manager[e.OldCommandCode]);
			commands[e.NewCommandCode].UpdateUserHotKey(manager[e.NewCommandCode]);
		}

		private interface IHotKeyCommand : IRichCommand
		{
			void InitializeUserHotKey(HotKey newUserHotKey);
			void UpdateUserHotKey(HotKey newUserHotKey);
		}

		[DebuggerDisplay("Command None")]
		private class NoneCommand : IHotKeyCommand
		{
			public event EventHandler? CanExecuteChanged;
			public event PropertyChangingEventHandler? PropertyChanging;
			public event PropertyChangedEventHandler? PropertyChanged;

			public CommandCodes Code => CommandCodes.None;
			public string Label => string.Empty;
			public string LabelWithHotKey => string.Empty;
			public IGlyph Glyph => Commands.Glyph.None;
			public HotKey UserHotKey => HotKey.None;
			public HotKey DefaultHotKey => HotKey.None;
			public string? HotKeyOverride => null;
			public bool IsOn { get => false; set {} }
			public bool IsExecutable => false;

			public bool CanExecute(object? _) => false;
			public void Execute(object? _) {}
			public Task ExecuteAsync() => Task.CompletedTask;
			public void ExecuteTapped(object _, TappedRoutedEventArgs e) {}

			public void InitializeUserHotKey(HotKey _) {}
			public void UpdateUserHotKey(HotKey _) {}
		}

		[DebuggerDisplay("Command {Code}")]
		private class ActionCommand : ObservableObject, IHotKeyCommand
		{
			public event EventHandler? CanExecuteChanged;

			private readonly IAction action;
			private readonly ICommand command;

			public CommandCodes Code => action.Code;

			public string Label => action.Label;
			public string LabelWithHotKey => UserHotKey.IsNone ? Label : $"{Label} ({UserHotKey})";

			public IGlyph Glyph => action.Glyph;

			private HotKey userHotKey;
			public HotKey UserHotKey => userHotKey;
			public HotKey DefaultHotKey => action.HotKey;

			public string? HotKeyOverride => !UserHotKey.IsNone ? UserHotKey.ToString() : null;

			public bool IsOn
			{
				get => action.IsOn;
				set
				{
					if (action.IsOn != value)
						command.Execute(null);
				}
			}

			public bool IsExecutable => action.IsExecutable;

			public ActionCommand(IAction action)
			{
				this.action = action;
				userHotKey = action.HotKey;

				command = new AsyncRelayCommand(ExecuteAsync, () => action.IsExecutable);

				if (action is INotifyPropertyChanging notifyPropertyChanging)
					notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
				if (action is INotifyPropertyChanged notifyPropertyChanged)
					notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
			}

			public bool CanExecute(object? parameter) => command.CanExecute(parameter);
			public void Execute(object? parameter) => command.Execute(parameter);

			public Task ExecuteAsync()
			{
				if (IsExecutable)
					return action.ExecuteAsync();
				return Task.CompletedTask;
			}

			public async void ExecuteTapped(object _, TappedRoutedEventArgs e) => await action.ExecuteAsync();

			public void InitializeUserHotKey(HotKey newUserHotKey)
				=> userHotKey = newUserHotKey;
			public void UpdateUserHotKey(HotKey newUserHotKey)
			{
				if (userHotKey != newUserHotKey)
				{
					userHotKey = newUserHotKey;
					OnPropertyChanged(nameof(UserHotKey));
					OnPropertyChanged(nameof(LabelWithHotKey));
				}
			}

			private void Action_PropertyChanging(object? sender, PropertyChangingEventArgs e)
			{
				switch (e.PropertyName)
				{
					case nameof(IAction.Label):
						OnPropertyChanging(nameof(Label));
						break;
					case nameof(IAction.Glyph):
						OnPropertyChanging(nameof(Glyph));
						break;
					case nameof(IAction.HotKey):
						OnPropertyChanging(nameof(DefaultHotKey));
						break;
					case nameof(IAction.IsOn):
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
						break;
					case nameof(IAction.Glyph):
						OnPropertyChanged(nameof(Glyph));
						break;
					case nameof(IAction.HotKey):
						OnPropertyChanged(nameof(DefaultHotKey));
						break;
					case nameof(IAction.IsOn):
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
}
