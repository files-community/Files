using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Actions;
using Files.App.Actions.Content.Archives;
using Files.App.Actions.Content.Background;
using Files.App.Actions.Content.ImageEdition;
using Files.App.Actions.Favorites;
using Microsoft.UI.Xaml;
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
		private readonly IImmutableDictionary<CommandCodes, IRichCommand> commands;
		private readonly IImmutableDictionary<HotKey, IRichCommand> hotKeys;

		public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
		public IRichCommand this[HotKey hotKey] => hotKeys.TryGetValue(hotKey, out var command) ? command : None;

		public IRichCommand None => commands[CommandCodes.None];
		public IRichCommand OpenHelp => commands[CommandCodes.OpenHelp];
		public IRichCommand ToggleFullScreen => commands[CommandCodes.ToggleFullScreen];
		public IRichCommand ToggleShowHiddenItems => commands[CommandCodes.ToggleShowHiddenItems];
		public IRichCommand ToggleShowFileExtensions => commands[CommandCodes.ToggleShowFileExtensions];
		public IRichCommand TogglePreviewPane => commands[CommandCodes.TogglePreviewPane];
		public IRichCommand MultiSelect => commands[CommandCodes.MultiSelect];
		public IRichCommand SelectAll => commands[CommandCodes.SelectAll];
		public IRichCommand InvertSelection => commands[CommandCodes.InvertSelection];
		public IRichCommand ClearSelection => commands[CommandCodes.ClearSelection];
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
		public IRichCommand CopyItem => commands[CommandCodes.CopyItem];
		public IRichCommand CutItem => commands[CommandCodes.CutItem];
		public IRichCommand DeleteItem => commands[CommandCodes.DeleteItem];
		public IRichCommand RunAsAdmin => commands[CommandCodes.RunAsAdmin];
		public IRichCommand RunAsAnotherUser => commands[CommandCodes.RunAsAnotherUser];
		public IRichCommand CompressIntoArchive => commands[CommandCodes.CompressIntoArchive];
		public IRichCommand CompressIntoSevenZip => commands[CommandCodes.CompressIntoSevenZip];
		public IRichCommand CompressIntoZip => commands[CommandCodes.CompressIntoZip];
		public IRichCommand RotateLeft => commands[CommandCodes.RotateLeft];
		public IRichCommand RotateRight => commands[CommandCodes.RotateRight];

		public CommandManager()
		{
			commands = CreateActions()
				.Select(action => new ActionCommand(this, action.Key, action.Value))
				.Cast<IRichCommand>()
				.Append(new NoneCommand())
				.ToImmutableDictionary(command => command.Code);

			var hotKeys = new Dictionary<HotKey, IRichCommand>();
			foreach (var command in commands.Values)
			{
				if (!command.HotKey.IsNone)
					hotKeys.Add(command.HotKey, command);
				if (!command.SecondHotKey.IsNone)
					hotKeys.Add(command.SecondHotKey, command);
				if (!command.ThirdHotKey.IsNone)
					hotKeys.Add(command.ThirdHotKey, command);
				if (!command.MediaHotKey.IsNone)
					hotKeys.Add(command.MediaHotKey, command);
			}
			this.hotKeys = hotKeys.ToImmutableDictionary();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => commands.Values.GetEnumerator();

		private static IDictionary<CommandCodes, IAction> CreateActions() => new Dictionary<CommandCodes, IAction>
		{
			[CommandCodes.OpenHelp] = new OpenHelpAction(),
			[CommandCodes.ToggleFullScreen] = new ToggleFullScreenAction(),
			[CommandCodes.ToggleShowHiddenItems] = new ToggleShowHiddenItemsAction(),
			[CommandCodes.ToggleShowFileExtensions] = new ToggleShowFileExtensionsAction(),
			[CommandCodes.TogglePreviewPane] = new TogglePreviewPaneAction(),
			[CommandCodes.MultiSelect] = new MultiSelectAction(),
			[CommandCodes.SelectAll] = new SelectAllAction(),
			[CommandCodes.InvertSelection] = new InvertSelectionAction(),
			[CommandCodes.ClearSelection] = new ClearSelectionAction(),
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
			[CommandCodes.CopyItem] = new CopyItemAction(),
			[CommandCodes.CutItem] = new CutItemAction(),
			[CommandCodes.DeleteItem] = new DeleteItemAction(),
			[CommandCodes.RunAsAdmin] = new RunAsAdminAction(),
			[CommandCodes.RunAsAnotherUser] = new RunAsAnotherUserAction(),
			[CommandCodes.CompressIntoArchive] = new CompressIntoArchiveAction(),
			[CommandCodes.CompressIntoSevenZip] = new CompressIntoSevenZipAction(),
			[CommandCodes.CompressIntoZip] = new CompressIntoZipAction(),
			[CommandCodes.RotateLeft] = new RotateLeftAction(),
			[CommandCodes.RotateRight] = new RotateRightAction()
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
			public object? Icon => null;
			public FontIcon? FontIcon => null;
			public Style? OpacityStyle => null;

			public string? HotKeyText => null;
			public HotKey HotKey => HotKey.None;
			public HotKey SecondHotKey => HotKey.None;
			public HotKey ThirdHotKey => HotKey.None;
			public HotKey MediaHotKey => HotKey.None;

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
			public string LabelWithHotKey { get; }
			public string AutomationName => Label;

			public RichGlyph Glyph => action.Glyph;
			public object? Icon { get; }
			public FontIcon? FontIcon { get; }
			public Style? OpacityStyle { get; }

			public string? HotKeyText { get; }
			public HotKey HotKey => action.HotKey;
			public HotKey SecondHotKey => action.SecondHotKey;
			public HotKey ThirdHotKey => action.ThirdHotKey;
			public HotKey MediaHotKey => action.MediaHotKey;

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
				Icon = action.Glyph.ToIcon();
				FontIcon = action.Glyph.ToFontIcon();
				OpacityStyle = action.Glyph.ToOpacityStyle();
				HotKeyText = GetHotKeyText();
				LabelWithHotKey = HotKeyText is null ? Label : $"{Label} ({HotKeyText})";
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

			private string? GetHotKeyText()
			{
				string text = string.Join(',',
					new List<HotKey> { HotKey, SecondHotKey, ThirdHotKey }
					.Where(hotKey => !hotKey.IsNone)
					.Select(HotKey => HotKey.ToString())
				);
				return text.Length > 0 ? text : null;
			}
		}
	}
}
