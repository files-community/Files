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
		public IRichCommand OpenInVS => commands[CommandCodes.OpenInVS];
		public IRichCommand OpenInVSCode => commands[CommandCodes.OpenInVSCode];
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
			[CommandCodes.OpenHelp] = new DebouncedActionDecorator(new OpenHelpAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleFullScreen] = new DebouncedActionDecorator(new ToggleFullScreenAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.EnterCompactOverlay] = new DebouncedActionDecorator(new EnterCompactOverlayAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ExitCompactOverlay] = new DebouncedActionDecorator(new ExitCompactOverlayAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleCompactOverlay] = new DebouncedActionDecorator(new ToggleCompactOverlayAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.Search] = new DebouncedActionDecorator(new SearchAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SearchUnindexedItems] = new DebouncedActionDecorator(new SearchUnindexedItemsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.EditPath] = new DebouncedActionDecorator(new EditPathAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.Redo] = new DebouncedActionDecorator(new RedoAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.Undo] = new DebouncedActionDecorator(new UndoAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleShowHiddenItems] = new DebouncedActionDecorator(new ToggleShowHiddenItemsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleShowFileExtensions] = new DebouncedActionDecorator(new ToggleShowFileExtensionsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.TogglePreviewPane] = new DebouncedActionDecorator(new TogglePreviewPaneAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SelectAll] = new DebouncedActionDecorator(new SelectAllAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.InvertSelection] = new DebouncedActionDecorator(new InvertSelectionAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ClearSelection] = new DebouncedActionDecorator(new ClearSelectionAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleSelect] = new DebouncedActionDecorator(new ToggleSelectAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ShareItem] = new DebouncedActionDecorator(new ShareItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.EmptyRecycleBin] = new DebouncedActionDecorator(new EmptyRecycleBinAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RestoreRecycleBin] = new DebouncedActionDecorator(new RestoreRecycleBinAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RestoreAllRecycleBin] = new DebouncedActionDecorator(new RestoreAllRecycleBinAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RefreshItems] = new DebouncedActionDecorator(new RefreshItemsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.Rename] = new DebouncedActionDecorator(new RenameAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CreateShortcut] = new DebouncedActionDecorator(new CreateShortcutAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CreateShortcutFromDialog] = new DebouncedActionDecorator(new CreateShortcutFromDialogAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CreateFolder] = new DebouncedActionDecorator(new CreateFolderAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CreateFolderWithSelection] = new DebouncedActionDecorator(new CreateFolderWithSelectionAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.AddItem] = new DebouncedActionDecorator(new AddItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.PinToStart] = new DebouncedActionDecorator(new PinToStartAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.UnpinFromStart] = new DebouncedActionDecorator(new UnpinFromStartAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.PinItemToFavorites] = new DebouncedActionDecorator(new PinItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.UnpinItemFromFavorites] = new DebouncedActionDecorator(new UnpinItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SetAsWallpaperBackground] = new DebouncedActionDecorator(new SetAsWallpaperBackgroundAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SetAsSlideshowBackground] = new DebouncedActionDecorator(new SetAsSlideshowBackgroundAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SetAsLockscreenBackground] = new DebouncedActionDecorator(new SetAsLockscreenBackgroundAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CopyItem] = new DebouncedActionDecorator(new CopyItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CopyPath] = new DebouncedActionDecorator(new CopyPathAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CutItem] = new DebouncedActionDecorator(new CutItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.PasteItem] = new DebouncedActionDecorator(new PasteItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.PasteItemToSelection] = new DebouncedActionDecorator(new PasteItemToSelectionAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DeleteItem] = new DebouncedActionDecorator(new DeleteItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DeleteItemPermanently] = new DebouncedActionDecorator(new DeleteItemPermanentlyAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.InstallFont] = new DebouncedActionDecorator(new InstallFontAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.InstallInfDriver] = new DebouncedActionDecorator(new InstallInfDriverAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.InstallCertificate] = new DebouncedActionDecorator(new InstallCertificateAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RunAsAdmin] = new DebouncedActionDecorator(new RunAsAdminAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RunAsAnotherUser] = new DebouncedActionDecorator(new RunAsAnotherUserAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RunWithPowershell] = new DebouncedActionDecorator(new RunWithPowershellAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LaunchPreviewPopup] = new DebouncedActionDecorator(new LaunchPreviewPopupAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CompressIntoArchive] = new DebouncedActionDecorator(new CompressIntoArchiveAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CompressIntoSevenZip] = new DebouncedActionDecorator(new CompressIntoSevenZipAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CompressIntoZip] = new DebouncedActionDecorator(new CompressIntoZipAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DecompressArchive] = new DebouncedActionDecorator(new DecompressArchive(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DecompressArchiveHere] = new DebouncedActionDecorator(new DecompressArchiveHere(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DecompressArchiveToChildFolder] = new DebouncedActionDecorator(new DecompressArchiveToChildFolderAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RotateLeft] = new DebouncedActionDecorator(new RotateLeftAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.RotateRight] = new DebouncedActionDecorator(new RotateRightAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenItem] = new DebouncedActionDecorator(new OpenItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenItemWithApplicationPicker] = new DebouncedActionDecorator(new OpenItemWithApplicationPickerAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenParentFolder] = new DebouncedActionDecorator(new OpenParentFolderAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenInVS] = new DebouncedActionDecorator(new OpenInVSAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenInVSCode] = new DebouncedActionDecorator(new OpenInVSCodeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenProperties] = new DebouncedActionDecorator(new OpenPropertiesAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenSettings] = new DebouncedActionDecorator(new OpenSettingsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenTerminal] = new DebouncedActionDecorator(new OpenTerminalAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenTerminalAsAdmin] = new DebouncedActionDecorator(new OpenTerminalAsAdminAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenCommandPalette] = new DebouncedActionDecorator(new OpenCommandPaletteAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutDecreaseSize] = new DebouncedActionDecorator(new LayoutDecreaseSizeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutIncreaseSize] = new DebouncedActionDecorator(new LayoutIncreaseSizeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutDetails] = new DebouncedActionDecorator(new LayoutDetailsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutTiles] = new DebouncedActionDecorator(new LayoutTilesAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutGridSmall] = new DebouncedActionDecorator(new LayoutGridSmallAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutGridMedium] = new DebouncedActionDecorator(new LayoutGridMediumAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutGridLarge] = new DebouncedActionDecorator(new LayoutGridLargeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutColumns] = new DebouncedActionDecorator(new LayoutColumnsAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.LayoutAdaptive] = new DebouncedActionDecorator(new LayoutAdaptiveAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByName] = new DebouncedActionDecorator(new SortByNameAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByDateModified] = new DebouncedActionDecorator(new SortByDateModifiedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByDateCreated] = new DebouncedActionDecorator(new SortByDateCreatedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortBySize] = new DebouncedActionDecorator(new SortBySizeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByType] = new DebouncedActionDecorator(new SortByTypeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortBySyncStatus] = new DebouncedActionDecorator(new SortBySyncStatusAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByTag] = new DebouncedActionDecorator(new SortByTagAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByPath] = new DebouncedActionDecorator(new SortByPathAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByOriginalFolder] = new DebouncedActionDecorator(new SortByOriginalFolderAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortByDateDeleted] = new DebouncedActionDecorator(new SortByDateDeletedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortAscending] = new DebouncedActionDecorator(new SortAscendingAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.SortDescending] = new DebouncedActionDecorator(new SortDescendingAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleSortDirection] = new DebouncedActionDecorator(new ToggleSortDirectionAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleSortDirectoriesAlongsideFiles] = new DebouncedActionDecorator(new ToggleSortDirectoriesAlongsideFilesAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByNone] = new DebouncedActionDecorator(new GroupByNoneAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByName] = new DebouncedActionDecorator(new GroupByNameAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateModified] = new DebouncedActionDecorator(new GroupByDateModifiedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateCreated] = new DebouncedActionDecorator(new GroupByDateCreatedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupBySize] = new DebouncedActionDecorator(new GroupBySizeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByType] = new DebouncedActionDecorator(new GroupByTypeAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupBySyncStatus] = new DebouncedActionDecorator(new GroupBySyncStatusAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByTag] = new DebouncedActionDecorator(new GroupByTagAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByOriginalFolder] = new DebouncedActionDecorator(new GroupByOriginalFolderAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateDeleted] = new DebouncedActionDecorator(new GroupByDateDeletedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByFolderPath] = new DebouncedActionDecorator(new GroupByFolderPathAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateModifiedYear] = new DebouncedActionDecorator(new GroupByDateModifiedYearAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateModifiedMonth] = new DebouncedActionDecorator(new GroupByDateModifiedMonthAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateCreatedYear] = new DebouncedActionDecorator(new GroupByDateCreatedYearAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateCreatedMonth] = new DebouncedActionDecorator(new GroupByDateCreatedMonthAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateDeletedYear] = new DebouncedActionDecorator(new GroupByDateDeletedYearAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByDateDeletedMonth] = new DebouncedActionDecorator(new GroupByDateDeletedMonthAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupAscending] = new DebouncedActionDecorator(new GroupAscendingAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupDescending] = new DebouncedActionDecorator(new GroupDescendingAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleGroupDirection] = new DebouncedActionDecorator(new ToggleGroupDirectionAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByYear] = new DebouncedActionDecorator(new GroupByYearAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GroupByMonth] = new DebouncedActionDecorator(new GroupByMonthAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ToggleGroupByDateUnit] = new DebouncedActionDecorator(new ToggleGroupByDateUnitAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.NewTab] = new DebouncedActionDecorator(new NewTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.FormatDrive] = new DebouncedActionDecorator(new FormatDriveAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.NavigateBack] = new DebouncedActionDecorator(new NavigateBackAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.NavigateForward] = new DebouncedActionDecorator(new NavigateForwardAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.NavigateUp] = new DebouncedActionDecorator(new NavigateUpAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DuplicateCurrentTab] = new DebouncedActionDecorator(new DuplicateCurrentTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.DuplicateSelectedTab] = new DebouncedActionDecorator(new DuplicateSelectedTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseTabsToTheLeftCurrent] = new DebouncedActionDecorator(new CloseTabsToTheLeftCurrentAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseTabsToTheLeftSelected] = new DebouncedActionDecorator(new CloseTabsToTheLeftSelectedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseTabsToTheRightCurrent] = new DebouncedActionDecorator(new CloseTabsToTheRightCurrentAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseTabsToTheRightSelected] = new DebouncedActionDecorator(new CloseTabsToTheRightSelectedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseOtherTabsCurrent] = new DebouncedActionDecorator(new CloseOtherTabsCurrentAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseOtherTabsSelected] = new DebouncedActionDecorator(new CloseOtherTabsSelectedAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenDirectoryInNewPane] = new DebouncedActionDecorator(new OpenDirectoryInNewPaneAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenDirectoryInNewTab] = new DebouncedActionDecorator(new OpenDirectoryInNewTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenInNewWindowItem] = new DebouncedActionDecorator(new OpenInNewWindowItemAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ReopenClosedTab] = new DebouncedActionDecorator(new ReopenClosedTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.PreviousTab] = new DebouncedActionDecorator(new PreviousTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.NextTab] = new DebouncedActionDecorator(new NextTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.CloseSelectedTab] = new DebouncedActionDecorator(new CloseSelectedTabAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenNewPane] = new DebouncedActionDecorator(new OpenNewPaneAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.ClosePane] = new DebouncedActionDecorator(new ClosePaneAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenFileLocation] = new DebouncedActionDecorator(new OpenFileLocationAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.PlayAll] = new DebouncedActionDecorator(new PlayAllAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GitFetch] = new DebouncedActionDecorator(new GitFetchAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GitInit] = new DebouncedActionDecorator(new GitInitAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GitPull] = new DebouncedActionDecorator(new GitPullAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GitPush] = new DebouncedActionDecorator(new GitPushAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.GitSync] = new DebouncedActionDecorator(new GitSyncAction(), TimeSpan.FromMilliseconds(800)),
			[CommandCodes.OpenAllTaggedItems] = new DebouncedActionDecorator(new OpenAllTaggedActions(), TimeSpan.FromMilliseconds(800))
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
