using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Actions;
using Files.Backend.Services.Settings;
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
using Windows.Web.Http.Headers;

namespace Files.App.Commands
{
	internal class CommandManager : ICommandManager
	{
		private readonly IPreferencesSettingsService settings = Ioc.Default.GetRequiredService<IPreferencesSettingsService>();

		private readonly IImmutableDictionary<CommandCodes, IRichCommand> commands;
		private IImmutableDictionary<HotKey, IRichCommand> hotKeys = new Dictionary<HotKey, IRichCommand>().ToImmutableDictionary();

		public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
		public IRichCommand this[HotKey hotKey] => hotKeys.TryGetValue(hotKey, out var command) ? command : None;

		public IRichCommand None => commands[CommandCodes.None];
		public IRichCommand OpenHelp => commands[CommandCodes.OpenHelp];
		public IRichCommand ToggleFullScreen => commands[CommandCodes.ToggleFullScreen];
		public IRichCommand EnterCompactOverlay => commands[CommandCodes.EnterCompactOverlay];
		public IRichCommand ExitCompactOverlay => commands[CommandCodes.ExitCompactOverlay];
		public IRichCommand ToggleCompactOverlay => commands[CommandCodes.ToggleCompactOverlay];
		public IRichCommand Search => commands[CommandCodes.Search];
		public IRichCommand ToggleShowHiddenItems => commands[CommandCodes.ToggleShowHiddenItems];
		public IRichCommand ToggleShowFileExtensions => commands[CommandCodes.ToggleShowFileExtensions];
		public IRichCommand TogglePreviewPane => commands[CommandCodes.TogglePreviewPane];
		public IRichCommand ToggleSidebar => commands[CommandCodes.ToggleSidebar];
		public IRichCommand SelectAll => commands[CommandCodes.SelectAll];
		public IRichCommand InvertSelection => commands[CommandCodes.InvertSelection];
		public IRichCommand ClearSelection => commands[CommandCodes.ClearSelection];
		public IRichCommand ToggleSelect => commands[CommandCodes.ToggleSelect];
		public IRichCommand ShareItem => commands[CommandCodes.ShareItem];
		public IRichCommand EmptyRecycleBin => commands[CommandCodes.EmptyRecycleBin];
		public IRichCommand RestoreRecycleBin => commands[CommandCodes.RestoreRecycleBin];
		public IRichCommand RestoreAllRecycleBin => commands[CommandCodes.RestoreAllRecycleBin];
		public IRichCommand RefreshItems => commands[CommandCodes.RefreshItems];
		public IRichCommand Rename => commands[CommandCodes.Rename];
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
		public IRichCommand CopyPath => commands[CommandCodes.CopyPath];
		public IRichCommand CutItem => commands[CommandCodes.CutItem];
		public IRichCommand PasteItem => commands[CommandCodes.PasteItem];
		public IRichCommand PasteItemToSelection => commands[CommandCodes.PasteItemToSelection];
		public IRichCommand DeleteItem => commands[CommandCodes.DeleteItem];
		public IRichCommand InstallFont => commands[CommandCodes.InstallFont];
		public IRichCommand InstallInfDriver => commands[CommandCodes.InstallInfDriver];
		public IRichCommand RunAsAdmin => commands[CommandCodes.RunAsAdmin];
		public IRichCommand RunAsAnotherUser => commands[CommandCodes.RunAsAnotherUser];
		public IRichCommand RunWithPowershell => commands[CommandCodes.RunWithPowershell];
		public IRichCommand LaunchQuickLook => commands[CommandCodes.LaunchQuickLook];
		public IRichCommand CompressIntoArchive => commands[CommandCodes.CompressIntoArchive];
		public IRichCommand CompressIntoSevenZip => commands[CommandCodes.CompressIntoSevenZip];
		public IRichCommand CompressIntoZip => commands[CommandCodes.CompressIntoZip];
		public IRichCommand DecompressArchive => commands[CommandCodes.DecompressArchive];
		public IRichCommand DecompressArchiveHere => commands[CommandCodes.DecompressArchiveHere];
		public IRichCommand DecompressArchiveToChildFolder => commands[CommandCodes.DecompressArchiveToChildFolder];
		public IRichCommand RotateLeft => commands[CommandCodes.RotateLeft];
		public IRichCommand RotateRight => commands[CommandCodes.RotateRight];
		public IRichCommand OpenItem => commands[CommandCodes.OpenItem];
		public IRichCommand OpenItemWithApplicationPicker => commands[CommandCodes.OpenItemWithApplicationPicker];
		public IRichCommand OpenParentFolder => commands[CommandCodes.OpenParentFolder];
		public IRichCommand OpenSettings => commands[CommandCodes.OpenSettings];
		public IRichCommand OpenTerminal => commands[CommandCodes.OpenTerminal];
		public IRichCommand OpenTerminalAsAdmin => commands[CommandCodes.OpenTerminalAsAdmin];
		public IRichCommand LayoutDecreaseSize => commands[CommandCodes.LayoutDecreaseSize];
		public IRichCommand LayoutIncreaseSize => commands[CommandCodes.LayoutIncreaseSize];
		public IRichCommand LayoutDetails => commands[CommandCodes.LayoutDetails];
		public IRichCommand LayoutTiles => commands[CommandCodes.LayoutTiles];
		public IRichCommand LayoutGridSmall => commands[CommandCodes.LayoutGridSmall];
		public IRichCommand LayoutGridMedium => commands[CommandCodes.LayoutGridMedium];
		public IRichCommand LayoutGridLarge => commands[CommandCodes.LayoutGridLarge];
		public IRichCommand LayoutColumns => commands[CommandCodes.LayoutColumns];
		public IRichCommand LayoutAdaptive => commands[CommandCodes.LayoutAdaptive];
		public IRichCommand SortByName => commands[CommandCodes.SortByName];
		public IRichCommand SortByDateModified => commands[CommandCodes.SortByDateModified];
		public IRichCommand SortByDateCreated => commands[CommandCodes.SortByDateCreated];
		public IRichCommand SortBySize => commands[CommandCodes.SortBySize];
		public IRichCommand SortByType => commands[CommandCodes.SortByType];
		public IRichCommand SortBySyncStatus => commands[CommandCodes.SortBySyncStatus];
		public IRichCommand SortByTag => commands[CommandCodes.SortByTag];
		public IRichCommand SortByOriginalFolder => commands[CommandCodes.SortByOriginalFolder];
		public IRichCommand SortByDateDeleted => commands[CommandCodes.SortByDateDeleted];
		public IRichCommand SortAscending => commands[CommandCodes.SortAscending];
		public IRichCommand SortDescending => commands[CommandCodes.SortDescending];
		public IRichCommand ToggleSortDirection => commands[CommandCodes.ToggleSortDirection];
		public IRichCommand ToggleSortDirectoriesAlongsideFiles => commands[CommandCodes.ToggleSortDirectoriesAlongsideFiles];
		public IRichCommand GroupByNone => commands[CommandCodes.GroupByNone];
		public IRichCommand GroupByName => commands[CommandCodes.GroupByName];
		public IRichCommand GroupByDateModified => commands[CommandCodes.GroupByDateModified];
		public IRichCommand GroupByDateCreated => commands[CommandCodes.GroupByDateCreated];
		public IRichCommand GroupBySize => commands[CommandCodes.GroupBySize];
		public IRichCommand GroupByType => commands[CommandCodes.GroupByType];
		public IRichCommand GroupBySyncStatus => commands[CommandCodes.GroupBySyncStatus];
		public IRichCommand GroupByTag => commands[CommandCodes.GroupByTag];
		public IRichCommand GroupByOriginalFolder => commands[CommandCodes.GroupByOriginalFolder];
		public IRichCommand GroupByDateDeleted => commands[CommandCodes.GroupByDateDeleted];
		public IRichCommand GroupByFolderPath => commands[CommandCodes.GroupByFolderPath];
		public IRichCommand GroupAscending => commands[CommandCodes.GroupAscending];
		public IRichCommand GroupDescending => commands[CommandCodes.GroupDescending];
		public IRichCommand ToggleGroupDirection => commands[CommandCodes.ToggleGroupDirection];
		public IRichCommand NewTab => commands[CommandCodes.NewTab];
		public IRichCommand FormatDrive => commands[CommandCodes.FormatDrive];
		public IRichCommand NavigateBack => commands[CommandCodes.NavigateBack];
		public IRichCommand NavigateForward => commands[CommandCodes.NavigateForward];
		public IRichCommand NavigateUp => commands[CommandCodes.NavigateUp];
		public IRichCommand DuplicateCurrentTab => commands[CommandCodes.DuplicateCurrentTab];
		public IRichCommand DuplicateSelectedTab => commands[CommandCodes.DuplicateSelectedTab];
		public IRichCommand CloseTabsToTheLeftCurrent => commands[CommandCodes.CloseTabsToTheLeftCurrent];
		public IRichCommand CloseTabsToTheLeftSelected => commands[CommandCodes.CloseTabsToTheLeftSelected];
		public IRichCommand CloseTabsToTheRightCurrent => commands[CommandCodes.CloseTabsToTheRightCurrent];
		public IRichCommand CloseTabsToTheRightSelected => commands[CommandCodes.CloseTabsToTheRightSelected];
		public IRichCommand CloseOtherTabsCurrent => commands[CommandCodes.CloseOtherTabsCurrent];
		public IRichCommand CloseOtherTabsSelected => commands[CommandCodes.CloseOtherTabsSelected];
		public IRichCommand ReopenClosedTab => commands[CommandCodes.ReopenClosedTab];
		public IRichCommand PreviousTab => commands[CommandCodes.PreviousTab];
		public IRichCommand NextTab => commands[CommandCodes.NextTab];
		public IRichCommand CloseSelectedTab => commands[CommandCodes.CloseSelectedTab];
		public IRichCommand OpenNewPane => commands[CommandCodes.OpenNewPane];
		public IRichCommand ClosePane => commands[CommandCodes.ClosePane];

		public CommandManager()
		{
			commands = CreateActions()
				.Select(action => new ActionCommand(this, action.Key, action.Value))
				.Cast<IRichCommand>()
				.Append(new NoneCommand())
				.ToImmutableDictionary(command => command.Code);

			settings.PropertyChanged += Settings_PropertyChanged;
			UpdateHotKeys();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => commands.Values.GetEnumerator();

		private static IDictionary<CommandCodes, IAction> CreateActions() => new Dictionary<CommandCodes, IAction>
		{
			[CommandCodes.OpenHelp] = new OpenHelpAction(),
			[CommandCodes.ToggleFullScreen] = new ToggleFullScreenAction(),
			[CommandCodes.EnterCompactOverlay] = new EnterCompactOverlayAction(),
			[CommandCodes.ExitCompactOverlay] = new ExitCompactOverlayAction(),
			[CommandCodes.ToggleCompactOverlay] = new ToggleCompactOverlayAction(),
			[CommandCodes.Search] = new SearchAction(),
			[CommandCodes.ToggleShowHiddenItems] = new ToggleShowHiddenItemsAction(),
			[CommandCodes.ToggleShowFileExtensions] = new ToggleShowFileExtensionsAction(),
			[CommandCodes.TogglePreviewPane] = new TogglePreviewPaneAction(),
			[CommandCodes.ToggleSidebar] = new ToggleSidebarAction(),
			[CommandCodes.SelectAll] = new SelectAllAction(),
			[CommandCodes.InvertSelection] = new InvertSelectionAction(),
			[CommandCodes.ClearSelection] = new ClearSelectionAction(),
			[CommandCodes.ToggleSelect] = new ToggleSelectAction(),
			[CommandCodes.ShareItem] = new ShareItemAction(),
			[CommandCodes.EmptyRecycleBin] = new EmptyRecycleBinAction(),
			[CommandCodes.RestoreRecycleBin] = new RestoreRecycleBinAction(),
			[CommandCodes.RestoreAllRecycleBin] = new RestoreAllRecycleBinAction(),
			[CommandCodes.RefreshItems] = new RefreshItemsAction(),
			[CommandCodes.Rename] = new RenameAction(),
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
			[CommandCodes.CopyPath] = new CopyPathAction(),
			[CommandCodes.CutItem] = new CutItemAction(),
			[CommandCodes.PasteItem] = new PasteItemAction(),
			[CommandCodes.PasteItemToSelection] = new PasteItemToSelectionAction(),
			[CommandCodes.DeleteItem] = new DeleteItemAction(),
			[CommandCodes.InstallFont] = new InstallFontAction(),
			[CommandCodes.InstallInfDriver] = new InstallInfDriverAction(),
			[CommandCodes.RunAsAdmin] = new RunAsAdminAction(),
			[CommandCodes.RunAsAnotherUser] = new RunAsAnotherUserAction(),
			[CommandCodes.RunWithPowershell] = new RunWithPowershellAction(),
			[CommandCodes.LaunchQuickLook] = new LaunchQuickLookAction(),
			[CommandCodes.CompressIntoArchive] = new CompressIntoArchiveAction(),
			[CommandCodes.CompressIntoSevenZip] = new CompressIntoSevenZipAction(),
			[CommandCodes.CompressIntoZip] = new CompressIntoZipAction(),
			[CommandCodes.DecompressArchive] = new DecompressArchive(),
			[CommandCodes.DecompressArchiveHere] = new DecompressArchiveHere(),
			[CommandCodes.DecompressArchiveToChildFolder] = new DecompressArchiveToChildFolderAction(),
			[CommandCodes.RotateLeft] = new RotateLeftAction(),
			[CommandCodes.RotateRight] = new RotateRightAction(),
			[CommandCodes.OpenItem] = new OpenItemAction(),
			[CommandCodes.OpenItemWithApplicationPicker] = new OpenItemWithApplicationPickerAction(),
			[CommandCodes.OpenParentFolder] = new OpenParentFolderAction(),
			[CommandCodes.OpenSettings] = new OpenSettingsAction(),
			[CommandCodes.OpenTerminal] = new OpenTerminalAction(),
			[CommandCodes.OpenTerminalAsAdmin] = new OpenTerminalAsAdminAction(),
			[CommandCodes.LayoutDecreaseSize] = new LayoutDecreaseSizeAction(),
			[CommandCodes.LayoutIncreaseSize] = new LayoutIncreaseSizeAction(),
			[CommandCodes.LayoutDetails] = new LayoutDetailsAction(),
			[CommandCodes.LayoutTiles] = new LayoutTilesAction(),
			[CommandCodes.LayoutGridSmall] = new LayoutGridSmallAction(),
			[CommandCodes.LayoutGridMedium] = new LayoutGridMediumAction(),
			[CommandCodes.LayoutGridLarge] = new LayoutGridLargeAction(),
			[CommandCodes.LayoutColumns] = new LayoutColumnsAction(),
			[CommandCodes.LayoutAdaptive] = new LayoutAdaptiveAction(),
			[CommandCodes.SortByName] = new SortByNameAction(),
			[CommandCodes.SortByDateModified] = new SortByDateModifiedAction(),
			[CommandCodes.SortByDateCreated] = new SortByDateCreatedAction(),
			[CommandCodes.SortBySize] = new SortBySizeAction(),
			[CommandCodes.SortByType] = new SortByTypeAction(),
			[CommandCodes.SortBySyncStatus] = new SortBySyncStatusAction(),
			[CommandCodes.SortByTag] = new SortByTagAction(),
			[CommandCodes.SortByOriginalFolder] = new SortByOriginalFolderAction(),
			[CommandCodes.SortByDateDeleted] = new SortByDateDeletedAction(),
			[CommandCodes.SortAscending] = new SortAscendingAction(),
			[CommandCodes.SortDescending] = new SortDescendingAction(),
			[CommandCodes.ToggleSortDirection] = new ToggleSortDirectionAction(),
			[CommandCodes.ToggleSortDirectoriesAlongsideFiles] = new ToggleSortDirectoriesAlongsideFilesAction(),
			[CommandCodes.GroupByNone] = new GroupByNoneAction(),
			[CommandCodes.GroupByName] = new GroupByNameAction(),
			[CommandCodes.GroupByDateModified] = new GroupByDateModifiedAction(),
			[CommandCodes.GroupByDateCreated] = new GroupByDateCreatedAction(),
			[CommandCodes.GroupBySize] = new GroupBySizeAction(),
			[CommandCodes.GroupByType] = new GroupByTypeAction(),
			[CommandCodes.GroupBySyncStatus] = new GroupBySyncStatusAction(),
			[CommandCodes.GroupByTag] = new GroupByTagAction(),
			[CommandCodes.GroupByOriginalFolder] = new GroupByOriginalFolderAction(),
			[CommandCodes.GroupByDateDeleted] = new GroupByDateDeletedAction(),
			[CommandCodes.GroupByFolderPath] = new GroupByFolderPathAction(),
			[CommandCodes.GroupAscending] = new GroupAscendingAction(),
			[CommandCodes.GroupDescending] = new GroupDescendingAction(),
			[CommandCodes.ToggleGroupDirection] = new ToggleGroupDirectionAction(),
			[CommandCodes.NewTab] = new NewTabAction(),
			[CommandCodes.FormatDrive] = new FormatDriveAction(),
			[CommandCodes.NavigateBack] = new NavigateBackAction(),
			[CommandCodes.NavigateForward] = new NavigateForwardAction(),
			[CommandCodes.NavigateUp] = new NavigateUpAction(),
			[CommandCodes.DuplicateCurrentTab] = new DuplicateCurrentTabAction(),
			[CommandCodes.DuplicateSelectedTab] = new DuplicateSelectedTabAction(),
			[CommandCodes.CloseTabsToTheLeftCurrent] = new CloseTabsToTheLeftCurrentAction(),
			[CommandCodes.CloseTabsToTheLeftSelected] = new CloseTabsToTheLeftSelectedAction(),
			[CommandCodes.CloseTabsToTheRightCurrent] = new CloseTabsToTheRightCurrentAction(),
			[CommandCodes.CloseTabsToTheRightSelected] = new CloseTabsToTheRightSelectedAction(),
			[CommandCodes.CloseOtherTabsCurrent] = new CloseOtherTabsCurrentAction(),
			[CommandCodes.CloseOtherTabsSelected] = new CloseOtherTabsSelectedAction(),
			[CommandCodes.ReopenClosedTab] = new ReopenClosedTabAction(),
			[CommandCodes.PreviousTab] = new PreviousTabAction(),
			[CommandCodes.NextTab] = new NextTabAction(),
			[CommandCodes.CloseSelectedTab] = new CloseSelectedTabAction(),
			[CommandCodes.OpenNewPane] = new OpenNewPaneAction(),
			[CommandCodes.ClosePane] = new ClosePaneAction(),
		};

		private void UpdateHotKeys()
		{
			ISet<HotKey> useds = new HashSet<HotKey>();

			var customs = new Dictionary<CommandCodes, HotKeyCollection>();
			foreach (var custom in settings.Actions)
			{
				if (Enum.TryParse(custom.Key, true, out CommandCodes code))
				{
					if (code is CommandCodes.None)
						continue;

					var hotKeys = new HotKeyCollection(HotKeyCollection.Parse(custom.Value).Except(useds));
					customs.Add(code, new(hotKeys));

					foreach (var hotKey in hotKeys)
					{
						useds.Add(hotKey with { IsVisible = true });
						useds.Add(hotKey with { IsVisible = false });
					}
				}
			}

			foreach (var command in commands.Values.OfType<ActionCommand>())
			{
				bool isCustom = customs.ContainsKey(command.Code);

				var hotkeys = isCustom
					? customs[command.Code]
					: new HotKeyCollection(GetHotKeys(command.Action).Except(useds));

				command.UpdateHotKeys(isCustom, hotkeys);
			}

			hotKeys = commands.Values
				.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
				.ToImmutableDictionary(item => item.HotKey, item => item.Command);
		}

		private static HotKeyCollection GetHotKeys(IAction action)
			=> new(action.HotKey, action.SecondHotKey, action.ThirdHotKey, action.MediaHotKey);

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IPreferencesSettingsService.Actions))
				UpdateHotKeys();
		}

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

			public string Description => string.Empty;

			public RichGlyph Glyph => RichGlyph.None;
			public object? Icon => null;
			public FontIcon? FontIcon => null;
			public Style? OpacityStyle => null;

			public bool IsCustomHotKeys => false;
			public string? HotKeyText => null;
			public HotKeyCollection HotKeys
			{
				get => HotKeyCollection.Empty;
				set => throw new InvalidOperationException("This command is readonly.");
			}

			public bool IsToggle => false;
			public bool IsOn { get => false; set {} }
			public bool IsExecutable => false;

			public bool CanExecute(object? parameter) => false;
			public void Execute(object? parameter) {}
			public Task ExecuteAsync() => Task.CompletedTask;
			public void ExecuteTapped(object sender, TappedRoutedEventArgs e) {}

			public void ResetHotKeys() {}
		}

		[DebuggerDisplay("Command {Code}")]
		private class ActionCommand : ObservableObject, IRichCommand
		{
			public event EventHandler? CanExecuteChanged;

			private readonly CommandManager manager;

			public IAction Action { get; }
			public CommandCodes Code { get; }

			public string Label => Action.Label;
			public string LabelWithHotKey => HotKeyText is null ? Label : $"{Label} ({HotKeyText})";
			public string AutomationName => Label;

			public string Description => Action.Description;

			public RichGlyph Glyph => Action.Glyph;
			public object? Icon { get; }
			public FontIcon? FontIcon { get; }
			public Style? OpacityStyle { get; }

			private bool isCustomHotKeys = false;
			public bool IsCustomHotKeys => isCustomHotKeys;

			public string? HotKeyText
			{
				get
				{
					string text = HotKeys.Label;
					if (string.IsNullOrEmpty(text))
						return null;
					return text;
				}
			}

			private HotKeyCollection hotKeys;
			public HotKeyCollection HotKeys
			{
				get => hotKeys;
				set
				{
					if (hotKeys == value)
						return;

					string code = Code.ToString();
					var customs = new Dictionary<string, string>(manager.settings.Actions);

					if (!customs.ContainsKey(code))
						customs.Add(code, value.Code);
					else if (value != GetHotKeys(Action))
						customs[code] = value.Code;
					else
						customs.Remove(code);

					manager.settings.Actions = customs;
				}
			}

			public bool IsToggle => Action is IToggleAction;

			public bool IsOn
			{
				get => Action is IToggleAction toggleAction && toggleAction.IsOn;
				set
				{
					if (Action is IToggleAction toggleAction && toggleAction.IsOn != value)
						Execute(null);
				}
			}

			public bool IsExecutable => Action.IsExecutable;

			public ActionCommand(CommandManager manager, CommandCodes code, IAction action)
			{
				this.manager = manager;
				Code = code;
				Action = action;
				Icon = action.Glyph.ToIcon();
				FontIcon = action.Glyph.ToFontIcon();
				OpacityStyle = action.Glyph.ToOpacityStyle();
				hotKeys = GetHotKeys(action);

				if (action is INotifyPropertyChanging notifyPropertyChanging)
					notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
				if (action is INotifyPropertyChanged notifyPropertyChanged)
					notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
			}

			public bool CanExecute(object? parameter) => Action.IsExecutable;
			public async void Execute(object? parameter) => await ExecuteAsync();

			public async Task ExecuteAsync()
			{
				if (IsExecutable)
					await Action.ExecuteAsync();
			}

			public async void ExecuteTapped(object sender, TappedRoutedEventArgs e) => await ExecuteAsync();

			public void ResetHotKeys()
			{
				if (!IsCustomHotKeys)
					return;

				var customs = new Dictionary<string, string>(manager.settings.Actions);
				customs.Remove(Code.ToString());
				manager.settings.Actions = customs;
			}

			internal void UpdateHotKeys(bool isCustom, HotKeyCollection hotKeys)
			{
				if (Code is CommandCodes.SelectAll or CommandCodes.ClearSelection)
				{

				}

				SetProperty(ref isCustomHotKeys, isCustom, nameof(IsCustomHotKeys));

				if (SetProperty(ref this.hotKeys, hotKeys, nameof(HotKeys)))
				{
					OnPropertyChanged(nameof(HotKeyText));
					OnPropertyChanged(nameof(LabelWithHotKey));
				}
			}

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
