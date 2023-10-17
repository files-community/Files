// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Actions;
using Microsoft.AppCenter.Analytics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	internal class CommandManager : ICommandManager
	{
		private readonly IGeneralSettingsService settings = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		private readonly IImmutableDictionary<CommandCodes, IRichCommand> commands;
		private IImmutableDictionary<HotKey, IRichCommand> hotKeys = new Dictionary<HotKey, IRichCommand>().ToImmutableDictionary();

		public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
		public IRichCommand this[string code]
		{
			get
			{
				try
				{
					return commands[Enum.Parse<CommandCodes>(code, true)];
				}
				catch
				{
					return None;
				}
			}
		}
		public IRichCommand this[HotKey hotKey]
			=> hotKeys.TryGetValue(hotKey with { IsVisible = true }, out var command) ? command
			: hotKeys.TryGetValue(hotKey with { IsVisible = false }, out command) ? command
			: None;

		public IRichCommand None => commands[CommandCodes.None];
		public IRichCommand OpenHelp => commands[CommandCodes.OpenHelp];
		public IRichCommand ToggleFullScreen => commands[CommandCodes.ToggleFullScreen];
		public IRichCommand EnterCompactOverlay => commands[CommandCodes.EnterCompactOverlay];
		public IRichCommand ExitCompactOverlay => commands[CommandCodes.ExitCompactOverlay];
		public IRichCommand ToggleCompactOverlay => commands[CommandCodes.ToggleCompactOverlay];
		public IRichCommand Search => commands[CommandCodes.Search];
		public IRichCommand SearchUnindexedItems => commands[CommandCodes.SearchUnindexedItems];
		public IRichCommand EditPath => commands[CommandCodes.EditPath];
		public IRichCommand Redo => commands[CommandCodes.Redo];
		public IRichCommand Undo => commands[CommandCodes.Undo];
		public IRichCommand ToggleShowHiddenItems => commands[CommandCodes.ToggleShowHiddenItems];
		public IRichCommand ToggleShowFileExtensions => commands[CommandCodes.ToggleShowFileExtensions];
		public IRichCommand TogglePreviewPane => commands[CommandCodes.TogglePreviewPane];
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
		public IRichCommand CreateFolderWithSelection => commands[CommandCodes.CreateFolderWithSelection];
		public IRichCommand AddItem => commands[CommandCodes.AddItem];
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
		public IRichCommand DeleteItemPermanently => commands[CommandCodes.DeleteItemPermanently];
		public IRichCommand InstallFont => commands[CommandCodes.InstallFont];
		public IRichCommand InstallInfDriver => commands[CommandCodes.InstallInfDriver];
		public IRichCommand InstallCertificate => commands[CommandCodes.InstallCertificate];
		public IRichCommand RunAsAdmin => commands[CommandCodes.RunAsAdmin];
		public IRichCommand RunAsAnotherUser => commands[CommandCodes.RunAsAnotherUser];
		public IRichCommand RunWithPowershell => commands[CommandCodes.RunWithPowershell];
		public IRichCommand LaunchPreviewPopup => commands[CommandCodes.LaunchPreviewPopup];
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
		public IRichCommand OpenInVSCode => commands[CommandCodes.OpenInVSCode];
		public IRichCommand OpenRepoInVSCode => commands[CommandCodes.OpenRepoInVSCode];
		public IRichCommand OpenProperties => commands[CommandCodes.OpenProperties];
		public IRichCommand OpenSettings => commands[CommandCodes.OpenSettings];
		public IRichCommand OpenTerminal => commands[CommandCodes.OpenTerminal];
		public IRichCommand OpenTerminalAsAdmin => commands[CommandCodes.OpenTerminalAsAdmin];
		public IRichCommand OpenCommandPalette => commands[CommandCodes.OpenCommandPalette];
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
		public IRichCommand SortByPath => commands[CommandCodes.SortByPath];
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
		public IRichCommand GroupByDateModifiedYear => commands[CommandCodes.GroupByDateModifiedYear];
		public IRichCommand GroupByDateModifiedMonth => commands[CommandCodes.GroupByDateModifiedMonth];
		public IRichCommand GroupByDateCreatedYear => commands[CommandCodes.GroupByDateCreatedYear];
		public IRichCommand GroupByDateCreatedMonth => commands[CommandCodes.GroupByDateCreatedMonth];
		public IRichCommand GroupByDateDeletedYear => commands[CommandCodes.GroupByDateDeletedYear];
		public IRichCommand GroupByDateDeletedMonth => commands[CommandCodes.GroupByDateDeletedMonth];
		public IRichCommand GroupAscending => commands[CommandCodes.GroupAscending];
		public IRichCommand GroupDescending => commands[CommandCodes.GroupDescending];
		public IRichCommand ToggleGroupDirection => commands[CommandCodes.ToggleGroupDirection];
		public IRichCommand GroupByYear => commands[CommandCodes.GroupByYear];
		public IRichCommand GroupByMonth => commands[CommandCodes.GroupByMonth];
		public IRichCommand ToggleGroupByDateUnit => commands[CommandCodes.ToggleGroupByDateUnit];
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
		public IRichCommand OpenDirectoryInNewPaneAction => commands[CommandCodes.OpenDirectoryInNewPane];
		public IRichCommand OpenDirectoryInNewTabAction => commands[CommandCodes.OpenDirectoryInNewTab];
		public IRichCommand OpenInNewWindowItemAction => commands[CommandCodes.OpenInNewWindowItem];
		public IRichCommand ReopenClosedTab => commands[CommandCodes.ReopenClosedTab];
		public IRichCommand PreviousTab => commands[CommandCodes.PreviousTab];
		public IRichCommand NextTab => commands[CommandCodes.NextTab];
		public IRichCommand CloseSelectedTab => commands[CommandCodes.CloseSelectedTab];
		public IRichCommand OpenNewPane => commands[CommandCodes.OpenNewPane];
		public IRichCommand ClosePane => commands[CommandCodes.ClosePane];
		public IRichCommand OpenFileLocation => commands[CommandCodes.OpenFileLocation];
		public IRichCommand PlayAll => commands[CommandCodes.PlayAll];
		public IRichCommand GitFetch => commands[CommandCodes.GitFetch];
		public IRichCommand GitInit => commands[CommandCodes.GitInit];
		public IRichCommand GitPull => commands[CommandCodes.GitPull];
		public IRichCommand GitPush => commands[CommandCodes.GitPush];
		public IRichCommand GitSync => commands[CommandCodes.GitSync];
		public IRichCommand OpenAllTaggedItems => commands[CommandCodes.OpenAllTaggedItems];

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
			[CommandCodes.OpenHelp] = new DebouncedActionDecorator(new OpenHelpAction()),
			[CommandCodes.ToggleFullScreen] = new DebouncedActionDecorator(new ToggleFullScreenAction()),
			[CommandCodes.EnterCompactOverlay] = new DebouncedActionDecorator(new EnterCompactOverlayAction()),
			[CommandCodes.ExitCompactOverlay] = new DebouncedActionDecorator(new ExitCompactOverlayAction()),
			[CommandCodes.ToggleCompactOverlay] = new DebouncedActionDecorator(new ToggleCompactOverlayAction()),
			[CommandCodes.Search] = new DebouncedActionDecorator(new SearchAction()),
			[CommandCodes.SearchUnindexedItems] = new DebouncedActionDecorator(new SearchUnindexedItemsAction()),
			[CommandCodes.EditPath] = new DebouncedActionDecorator(new EditPathAction()),
			[CommandCodes.Redo] = new DebouncedActionDecorator(new RedoAction()),
			[CommandCodes.Undo] = new DebouncedActionDecorator(new UndoAction()),
			[CommandCodes.ToggleShowHiddenItems] = new DebouncedActionDecorator(new ToggleShowHiddenItemsAction()),
			[CommandCodes.ToggleShowFileExtensions] = new DebouncedActionDecorator(new ToggleShowFileExtensionsAction()),
			[CommandCodes.TogglePreviewPane] = new DebouncedActionDecorator(new TogglePreviewPaneAction()),
			[CommandCodes.SelectAll] = new DebouncedActionDecorator(new SelectAllAction()),
			[CommandCodes.InvertSelection] = new DebouncedActionDecorator(new InvertSelectionAction()),
			[CommandCodes.ClearSelection] = new DebouncedActionDecorator(new ClearSelectionAction()),
			[CommandCodes.ToggleSelect] = new DebouncedActionDecorator(new ToggleSelectAction()),
			[CommandCodes.ShareItem] = new DebouncedActionDecorator(new ShareItemAction()),
			[CommandCodes.EmptyRecycleBin] = new DebouncedActionDecorator(new EmptyRecycleBinAction()),
			[CommandCodes.RestoreRecycleBin] = new DebouncedActionDecorator(new RestoreRecycleBinAction()),
			[CommandCodes.RestoreAllRecycleBin] = new DebouncedActionDecorator(new RestoreAllRecycleBinAction()),
			[CommandCodes.RefreshItems] = new DebouncedActionDecorator(new RefreshItemsAction()),
			[CommandCodes.Rename] = new DebouncedActionDecorator(new RenameAction()),
			[CommandCodes.CreateShortcut] = new DebouncedActionDecorator(new CreateShortcutAction()),
			[CommandCodes.CreateShortcutFromDialog] = new DebouncedActionDecorator(new CreateShortcutFromDialogAction()),
			[CommandCodes.CreateFolder] = new DebouncedActionDecorator(new CreateFolderAction()),
			[CommandCodes.CreateFolderWithSelection] = new DebouncedActionDecorator(new CreateFolderWithSelectionAction()),
			[CommandCodes.AddItem] = new DebouncedActionDecorator(new AddItemAction()),
			[CommandCodes.PinToStart] = new DebouncedActionDecorator(new PinToStartAction()),
			[CommandCodes.UnpinFromStart] = new DebouncedActionDecorator(new UnpinFromStartAction()),
			[CommandCodes.PinItemToFavorites] = new DebouncedActionDecorator(new PinItemAction()),
			[CommandCodes.UnpinItemFromFavorites] = new DebouncedActionDecorator(new UnpinItemAction()),
			[CommandCodes.SetAsWallpaperBackground] = new DebouncedActionDecorator(new SetAsWallpaperBackgroundAction()),
			[CommandCodes.SetAsSlideshowBackground] = new DebouncedActionDecorator(new SetAsSlideshowBackgroundAction()),
			[CommandCodes.SetAsLockscreenBackground] = new DebouncedActionDecorator(new SetAsLockscreenBackgroundAction()),
			[CommandCodes.CopyItem] = new DebouncedActionDecorator(new CopyItemAction()),
			[CommandCodes.CopyPath] = new DebouncedActionDecorator(new CopyPathAction()),
			[CommandCodes.CutItem] = new DebouncedActionDecorator(new CutItemAction()),
			[CommandCodes.PasteItem] = new DebouncedActionDecorator(new PasteItemAction()),
			[CommandCodes.PasteItemToSelection] = new DebouncedActionDecorator(new PasteItemToSelectionAction()),
			[CommandCodes.DeleteItem] = new DebouncedActionDecorator(new DeleteItemAction()),
			[CommandCodes.DeleteItemPermanently] = new DebouncedActionDecorator(new DeleteItemPermanentlyAction()),
			[CommandCodes.InstallFont] = new DebouncedActionDecorator(new InstallFontAction()),
			[CommandCodes.InstallInfDriver] = new DebouncedActionDecorator(new InstallInfDriverAction()),
			[CommandCodes.InstallCertificate] = new DebouncedActionDecorator(new InstallCertificateAction()),
			[CommandCodes.RunAsAdmin] = new DebouncedActionDecorator(new RunAsAdminAction()),
			[CommandCodes.RunAsAnotherUser] = new DebouncedActionDecorator(new RunAsAnotherUserAction()),
			[CommandCodes.RunWithPowershell] = new DebouncedActionDecorator(new RunWithPowershellAction()),
			[CommandCodes.LaunchPreviewPopup] = new DebouncedActionDecorator(new LaunchPreviewPopupAction()),
			[CommandCodes.CompressIntoArchive] = new DebouncedActionDecorator(new CompressIntoArchiveAction()),
			[CommandCodes.CompressIntoSevenZip] = new DebouncedActionDecorator(new CompressIntoSevenZipAction()),
			[CommandCodes.CompressIntoZip] = new DebouncedActionDecorator(new CompressIntoZipAction()),
			[CommandCodes.DecompressArchive] = new DebouncedActionDecorator(new DecompressArchive()),
			[CommandCodes.DecompressArchiveHere] = new DebouncedActionDecorator(new DecompressArchiveHere()),
			[CommandCodes.DecompressArchiveToChildFolder] = new DebouncedActionDecorator(new DecompressArchiveToChildFolderAction()),
			[CommandCodes.RotateLeft] = new DebouncedActionDecorator(new RotateLeftAction()),
			[CommandCodes.RotateRight] = new DebouncedActionDecorator(new RotateRightAction()),
			[CommandCodes.OpenItem] = new DebouncedActionDecorator(new OpenItemAction()),
			[CommandCodes.OpenItemWithApplicationPicker] = new DebouncedActionDecorator(new OpenItemWithApplicationPickerAction()),
			[CommandCodes.OpenParentFolder] = new DebouncedActionDecorator(new OpenParentFolderAction()),
			[CommandCodes.OpenInVSCode] = new DebouncedActionDecorator(new OpenInVSCodeAction()),
			[CommandCodes.OpenRepoInVSCode] = new DebouncedActionDecorator(new OpenRepoInVSCodeAction()),
			[CommandCodes.OpenProperties] = new DebouncedActionDecorator(new OpenPropertiesAction()),
			[CommandCodes.OpenSettings] = new DebouncedActionDecorator(new OpenSettingsAction()),
			[CommandCodes.OpenTerminal] = new DebouncedActionDecorator(new OpenTerminalAction()),
			[CommandCodes.OpenTerminalAsAdmin] = new DebouncedActionDecorator(new OpenTerminalAsAdminAction()),
			[CommandCodes.OpenCommandPalette] = new DebouncedActionDecorator(new OpenCommandPaletteAction()),
			[CommandCodes.LayoutDecreaseSize] = new DebouncedActionDecorator(new LayoutDecreaseSizeAction()),
			[CommandCodes.LayoutIncreaseSize] = new DebouncedActionDecorator(new LayoutIncreaseSizeAction()),
			[CommandCodes.LayoutDetails] = new DebouncedActionDecorator(new LayoutDetailsAction()),
			[CommandCodes.LayoutTiles] = new DebouncedActionDecorator(new LayoutTilesAction()),
			[CommandCodes.LayoutGridSmall] = new DebouncedActionDecorator(new LayoutGridSmallAction()),
			[CommandCodes.LayoutGridMedium] = new DebouncedActionDecorator(new LayoutGridMediumAction()),
			[CommandCodes.LayoutGridLarge] = new DebouncedActionDecorator(new LayoutGridLargeAction()),
			[CommandCodes.LayoutColumns] = new DebouncedActionDecorator(new LayoutColumnsAction()),
			[CommandCodes.LayoutAdaptive] = new DebouncedActionDecorator(new LayoutAdaptiveAction()),
			[CommandCodes.SortByName] = new DebouncedActionDecorator(new SortByNameAction()),
			[CommandCodes.SortByDateModified] = new DebouncedActionDecorator(new SortByDateModifiedAction()),
			[CommandCodes.SortByDateCreated] = new DebouncedActionDecorator(new SortByDateCreatedAction()),
			[CommandCodes.SortBySize] = new DebouncedActionDecorator(new SortBySizeAction()),
			[CommandCodes.SortByType] = new DebouncedActionDecorator(new SortByTypeAction()),
			[CommandCodes.SortBySyncStatus] = new DebouncedActionDecorator(new SortBySyncStatusAction()),
			[CommandCodes.SortByTag] = new DebouncedActionDecorator(new SortByTagAction()),
			[CommandCodes.SortByPath] = new DebouncedActionDecorator(new SortByPathAction()),
			[CommandCodes.SortByOriginalFolder] = new DebouncedActionDecorator(new SortByOriginalFolderAction()),
			[CommandCodes.SortByDateDeleted] = new DebouncedActionDecorator(new SortByDateDeletedAction()),
			[CommandCodes.SortAscending] = new DebouncedActionDecorator(new SortAscendingAction()),
			[CommandCodes.SortDescending] = new DebouncedActionDecorator(new SortDescendingAction()),
			[CommandCodes.ToggleSortDirection] = new DebouncedActionDecorator(new ToggleSortDirectionAction()),
			[CommandCodes.ToggleSortDirectoriesAlongsideFiles] = new DebouncedActionDecorator(new ToggleSortDirectoriesAlongsideFilesAction()),
			[CommandCodes.GroupByNone] = new DebouncedActionDecorator(new GroupByNoneAction()),
			[CommandCodes.GroupByName] = new DebouncedActionDecorator(new GroupByNameAction()),
			[CommandCodes.GroupByDateModified] = new DebouncedActionDecorator(new GroupByDateModifiedAction()),
			[CommandCodes.GroupByDateCreated] = new DebouncedActionDecorator(new GroupByDateCreatedAction()),
			[CommandCodes.GroupBySize] = new DebouncedActionDecorator(new GroupBySizeAction()),
			[CommandCodes.GroupByType] = new DebouncedActionDecorator(new GroupByTypeAction()),
			[CommandCodes.GroupBySyncStatus] = new DebouncedActionDecorator(new GroupBySyncStatusAction()),
			[CommandCodes.GroupByTag] = new DebouncedActionDecorator(new GroupByTagAction()),
			[CommandCodes.GroupByOriginalFolder] = new DebouncedActionDecorator(new GroupByOriginalFolderAction()),
			[CommandCodes.GroupByDateDeleted] = new DebouncedActionDecorator(new GroupByDateDeletedAction()),
			[CommandCodes.GroupByFolderPath] = new DebouncedActionDecorator(new GroupByFolderPathAction()),
			[CommandCodes.GroupByDateModifiedYear] = new DebouncedActionDecorator(new GroupByDateModifiedYearAction()),
			[CommandCodes.GroupByDateModifiedMonth] = new DebouncedActionDecorator(new GroupByDateModifiedMonthAction()),
			[CommandCodes.GroupByDateCreatedYear] = new DebouncedActionDecorator(new GroupByDateCreatedYearAction()),
			[CommandCodes.GroupByDateCreatedMonth] = new DebouncedActionDecorator(new GroupByDateCreatedMonthAction()),
			[CommandCodes.GroupByDateDeletedYear] = new DebouncedActionDecorator(new GroupByDateDeletedYearAction()),
			[CommandCodes.GroupByDateDeletedMonth] = new DebouncedActionDecorator(new GroupByDateDeletedMonthAction()),
			[CommandCodes.GroupAscending] = new DebouncedActionDecorator(new GroupAscendingAction()),
			[CommandCodes.GroupDescending] = new DebouncedActionDecorator(new GroupDescendingAction()),
			[CommandCodes.ToggleGroupDirection] = new DebouncedActionDecorator(new ToggleGroupDirectionAction()),
			[CommandCodes.GroupByYear] = new DebouncedActionDecorator(new GroupByYearAction()),
			[CommandCodes.GroupByMonth] = new DebouncedActionDecorator(new GroupByMonthAction()),
			[CommandCodes.ToggleGroupByDateUnit] = new DebouncedActionDecorator(new ToggleGroupByDateUnitAction()),
			[CommandCodes.NewTab] = new DebouncedActionDecorator(new NewTabAction()),
			[CommandCodes.FormatDrive] = new DebouncedActionDecorator(new FormatDriveAction()),
			[CommandCodes.NavigateBack] = new DebouncedActionDecorator(new NavigateBackAction()),
			[CommandCodes.NavigateForward] = new DebouncedActionDecorator(new NavigateForwardAction()),
			[CommandCodes.NavigateUp] = new DebouncedActionDecorator(new NavigateUpAction()),
			[CommandCodes.DuplicateCurrentTab] = new DebouncedActionDecorator(new DuplicateCurrentTabAction()),
			[CommandCodes.DuplicateSelectedTab] = new DebouncedActionDecorator(new DuplicateSelectedTabAction()),
			[CommandCodes.CloseTabsToTheLeftCurrent] = new DebouncedActionDecorator(new CloseTabsToTheLeftCurrentAction()),
			[CommandCodes.CloseTabsToTheLeftSelected] = new DebouncedActionDecorator(new CloseTabsToTheLeftSelectedAction()),
			[CommandCodes.CloseTabsToTheRightCurrent] = new DebouncedActionDecorator(new CloseTabsToTheRightCurrentAction()),
			[CommandCodes.CloseTabsToTheRightSelected] = new DebouncedActionDecorator(new CloseTabsToTheRightSelectedAction()),
			[CommandCodes.CloseOtherTabsCurrent] = new DebouncedActionDecorator(new CloseOtherTabsCurrentAction()),
			[CommandCodes.CloseOtherTabsSelected] = new DebouncedActionDecorator(new CloseOtherTabsSelectedAction()),
			[CommandCodes.OpenDirectoryInNewPane] = new DebouncedActionDecorator(new OpenDirectoryInNewPaneAction()),
			[CommandCodes.OpenDirectoryInNewTab] = new DebouncedActionDecorator(new OpenDirectoryInNewTabAction()),
			[CommandCodes.OpenInNewWindowItem] = new DebouncedActionDecorator(new OpenInNewWindowItemAction()),
			[CommandCodes.ReopenClosedTab] = new DebouncedActionDecorator(new ReopenClosedTabAction()),
			[CommandCodes.PreviousTab] = new DebouncedActionDecorator(new PreviousTabAction()),
			[CommandCodes.NextTab] = new DebouncedActionDecorator(new NextTabAction()),
			[CommandCodes.CloseSelectedTab] = new DebouncedActionDecorator(new CloseSelectedTabAction()),
			[CommandCodes.OpenNewPane] = new DebouncedActionDecorator(new OpenNewPaneAction()),
			[CommandCodes.ClosePane] = new DebouncedActionDecorator(new ClosePaneAction()),
			[CommandCodes.OpenFileLocation] = new DebouncedActionDecorator(new OpenFileLocationAction()),
			[CommandCodes.PlayAll] = new DebouncedActionDecorator(new PlayAllAction()),
			[CommandCodes.GitFetch] = new DebouncedActionDecorator(new GitFetchAction()),
			[CommandCodes.GitInit] = new DebouncedActionDecorator(new GitInitAction()),
			[CommandCodes.GitPull] = new DebouncedActionDecorator(new GitPullAction()),
			[CommandCodes.GitPush] = new DebouncedActionDecorator(new GitPushAction()),
			[CommandCodes.GitSync] = new DebouncedActionDecorator(new GitSyncAction()),
			[CommandCodes.OpenAllTaggedItems] = new DebouncedActionDecorator(new OpenAllTaggedActions()),
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
			if (e.PropertyName is nameof(IGeneralSettingsService.Actions))
				UpdateHotKeys();
		}

		[DebuggerDisplay("Command {Code}")]
		internal class ActionCommand : ObservableObject, IRichCommand
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

			public Task ExecuteAsync()
			{
				if (IsExecutable)
				{
					Analytics.TrackEvent($"Triggered {Code} action");
					return Action.ExecuteAsync();
				}

				return Task.CompletedTask;
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
