using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Actions;
using Files.App.Actions.Background;
using Files.App.Actions.Favorites;
using Files.App.UserControls;
using Microsoft.UI.Xaml.Controls;
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
		public event EventHandler<HotKeyChangedEventArgs>? HotKeyChanged;

		private readonly IImmutableDictionary<CommandCodes, IRichCommand> commands;
		private readonly IDictionary<HotKey, IRichCommand> customHotKeys;

		public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
		public IRichCommand this[HotKey customHotKey] => customHotKeys.TryGetValue(customHotKey, out var command) ? command : None;

		public IRichCommand None => commands[CommandCodes.None];
		public IRichCommand OpenHelp => commands[CommandCodes.OpenHelp];
		public IRichCommand ToggleFullScreen => commands[CommandCodes.ToggleFullScreen];
		public IRichCommand ToggleShowHiddenItems => commands[CommandCodes.ToggleShowHiddenItems];
		public IRichCommand ToggleShowFileExtensions => commands[CommandCodes.ToggleShowFileExtensions];
		public IRichCommand EmptyRecycleBin => commands[CommandCodes.EmptyRecycleBin];
		public IRichCommand RestoreRecycleBin => commands[CommandCodes.RestoreRecycleBin];
		public IRichCommand RestoreAllRecycleBin => commands[CommandCodes.RestoreAllRecycleBin];
		public IRichCommand CreateShortcut => commands[CommandCodes.CreateShortcut];
		public IRichCommand CreateShortcutFromDialog => commands[CommandCodes.CreateShortcutFromDialog];
		public IRichCommand CreateFolder => commands[CommandCodes.CreateFolder];
		public IRichCommand PinToStart => commands[CommandCodes.PinToStart];
		public IRichCommand UnpinFromStart => commands[CommandCodes.UnpinFromStart];
		public IRichCommand PinItemToFavorites => commands[CommandCodes.PinItemToFavorites];
		public IRichCommand UnpinItemFromFavorites => commands[CommandCodes.UnpinItemFromFavorites];
		public IRichCommand SetAsWallpaperBackground => commands[CommandCodes.SetAsWallpaperBackground];
		public IRichCommand SetAsSlideshowBackground => commands[CommandCodes.SetAsSlideshowBackground];
		public IRichCommand SetAsLockscreenBackground => commands[CommandCodes.SetAsLockscreenBackground];

		public CommandManager()
		{
			commands = CreateActions()
				.Select(action => new ActionCommand(this, action.Key, action.Value))
				.Cast<IRichCommand>()
				.Append(new NoneCommand())
				.ToImmutableDictionary(command => command.Code);

			customHotKeys = commands.Values
				.Where(command => !command.CustomHotKey.IsNone)
				.ToDictionary(command => command.CustomHotKey);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => commands.Values.GetEnumerator();

		private static IDictionary<CommandCodes, IAction> CreateActions() => new Dictionary<CommandCodes, IAction>
		{
			[CommandCodes.OpenHelp] = new OpenHelpAction(),
			[CommandCodes.ToggleFullScreen] = new ToggleFullScreenAction(),
			[CommandCodes.ToggleShowHiddenItems] = new ToggleShowHiddenItemsAction(),
			[CommandCodes.ToggleShowFileExtensions] = new ToggleShowFileExtensionsAction(),
			[CommandCodes.EmptyRecycleBin] = new EmptyRecycleBinAction(),
			[CommandCodes.RestoreRecycleBin] = new RestoreRecycleBinAction(),
			[CommandCodes.RestoreAllRecycleBin] = new RestoreAllRecycleBinAction(),
			[CommandCodes.CreateShortcut] = new CreateShortcutAction(),
			[CommandCodes.CreateShortcutFromDialog] = new CreateShortcutFromDialogAction(),
			[CommandCodes.CreateFolder] = new CreateFolderAction(),
			[CommandCodes.PinToStart] = new PinToStartAction(),
			[CommandCodes.UnpinFromStart] = new UnpinFromStartAction(),
			[CommandCodes.PinItemToFavorites] = new PinItemAction(),
			[CommandCodes.UnpinItemFromFavorites] = new UnpinItemAction(),
			[CommandCodes.SetAsWallpaperBackground] = new SetAsWallpaperBackgroundAction(),
			[CommandCodes.SetAsSlideshowBackground] = new SetAsSlideshowBackgroundAction(),
			[CommandCodes.SetAsLockscreenBackground] = new SetAsLockscreenBackgroundAction(),
		};

		[DebuggerDisplay("Command None")]
		private class NoneCommand : IRichCommand
		{
			public event EventHandler? CanExecuteChanged { add {} remove {} }
			public event PropertyChangingEventHandler? PropertyChanging { add {} remove {} }
			public event PropertyChangedEventHandler? PropertyChanged { add {} remove {} }

			public CommandCodes Code => CommandCodes.None;

			public string Label => string.Empty;
			public string LabelWithHotKey => string.Empty;
			public string AutomationName => string.Empty;

			public RichGlyph Glyph => RichGlyph.None;
			public FontIcon? FontIcon => null;
			public OpacityIcon? OpacityIcon => null;

			public HotKey DefaultHotKey => HotKey.None;

			public HotKey CustomHotKey
			{
				get => HotKey.None;
				set => throw new InvalidOperationException("The None command cannot have hotkey.");
			}

			public bool IsToggle => false;
			public bool IsOn { get => false; set {} }
			public bool IsExecutable => false;

			public bool CanExecute(object? parameter) => false;
			public void Execute(object? parameter) {}
			public Task ExecuteAsync() => Task.CompletedTask;
			public void ExecuteTapped(object sender, TappedRoutedEventArgs e) {}
		}

		[DebuggerDisplay("Command {Code}")]
		private class ActionCommand : ObservableObject, IRichCommand
		{
			private readonly CommandManager manager;

			public event EventHandler? CanExecuteChanged;

			private readonly IAction action;
			private readonly ICommand command;

			public CommandCodes Code { get; }

			public string Label => action.Label;
			public string LabelWithHotKey => $"{Label} ({CustomHotKey})";
			public string AutomationName => Label;

			public RichGlyph Glyph => action.Glyph;

			private readonly Lazy<FontIcon?> fontIcon;
			public FontIcon? FontIcon => fontIcon.Value;

			private readonly Lazy<OpacityIcon?> opacityIcon;
			public OpacityIcon? OpacityIcon => opacityIcon.Value;

			public HotKey DefaultHotKey => action.HotKey;

			private HotKey customHotKey;
			public HotKey CustomHotKey
			{
				get => customHotKey;
				set
				{
					if (customHotKey == value)
						return;

					if (manager.customHotKeys.ContainsKey(value))
						manager[value].CustomHotKey = HotKey.None;

					if (!customHotKey.IsNone)
						manager.customHotKeys.Remove(customHotKey);

					if (!value.IsNone)
						manager.customHotKeys.Add(value, this);

					var args = new HotKeyChangedEventArgs
					{
						Command = this,
						OldHotKey = customHotKey,
						NewHotKey= value,
					};

					SetProperty(ref customHotKey, value);
					manager.HotKeyChanged?.Invoke(manager, args);
				}
			}

			public bool IsToggle => action is IToggleAction;

			public bool IsOn
			{
				get => action is IToggleAction toggleAction && toggleAction.IsOn;
				set
				{
					if (action is IToggleAction toggleAction && toggleAction.IsOn != value)
						command.Execute(null);
				}
			}

			public bool IsExecutable => action.IsExecutable;

			public ActionCommand(CommandManager manager, CommandCodes code, IAction action)
			{
				this.manager = manager;
				Code = code;
				this.action = action;
				fontIcon = new(action.Glyph.ToFontIcon);
				opacityIcon = new(action.Glyph.ToOpacityIcon);
				customHotKey = action.HotKey;
				command = new AsyncRelayCommand(ExecuteAsync, () => action.IsExecutable);

				if (action is INotifyPropertyChanging notifyPropertyChanging)
					notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
				if (action is INotifyPropertyChanged notifyPropertyChanged)
					notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
			}

			public bool CanExecute(object? parameter) => command.CanExecute(parameter);
			public void Execute(object? parameter) => command.Execute(parameter);

			public async Task ExecuteAsync()
			{
				if (IsExecutable)
					await action.ExecuteAsync();
			}

			public async void ExecuteTapped(object sender, TappedRoutedEventArgs e) => await action.ExecuteAsync();

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
}
