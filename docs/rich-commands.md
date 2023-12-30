# Concept of Rich Commands

> [!INFO]
> Write down here

# Commands

This is the list of all commands defined in `CommandCodes` enum except `None`.

| Category           | Name                                | Label                                     | Description                                              | HotKey              |
| ------------------ | ----------------------------------- | ----------------------------------------- | -------------------------------------------------------- | ------------------- |
| Global             | OpenHelp                            | Help                                      | Open online help page in browser                         | F1                  |
|                    | ToggleFullScreen                    | FullScreen                                | Toggle full screen                                       | F11                 |
|                    | EnterCompactOverlay                 | Enter compact overlay                     | Enter compact overlay                                    | Ctrl+Alt+Up         |
|                    | ExitCompactOverlay                  | Exit compact overlay                      | Exit compact overlay                                     | Ctrl+Alt+Down       |
|                    | ToggleCompactOverlay                | Toggle compact overlay                    | Toggle compact overlay                                   | F12                 |
|                    | Search                              | Search                                    | Go to search box                                         | Ctrl+F, F3          |
|                    | SearchUnindexedItems                | Search unindexed items                    | Search for unindexed items                               |                     |
|                    | Redo                                | Redo                                      | Redo the last file operation                             | Ctrl+Y              |
|                    | Undo                                | Undo                                      | Undo the last file operation                             | Ctrl+Z              |
|                    | EditPath                            | Edit path                                 | Focus path bar                                           | Ctrl+L, Alt+D       |
| Show               | ToggleShowHiddenItems               | Show hidden items                         | Toggle whether to show hidden items                      | Ctrl+H              |
|                    | ToggleShowFileExtensions            | Show file extensions                      | Toggle whether to show file extensions                   |                     |
|                    | TogglePreviewPane                   | Toggle the preview pane                   | Toggle whether to show preview pane                      | Ctrl+P              |
|                    | ToggleSidebar                       | Toggle the sidebar                        | Toggle whether to show sidebar                           | Ctrl+B              |
| File System        | CopyItem                            | Copy                                      | Copy item(s) to clipboard                                | Ctrl+C              |
|                    | CopyPath                            | Copy path                                 | Copy path of item to clipboard                           | Ctrl+Shift+C        |
|                    | CutItem                             | Cut                                       | Cut item(s) to clipboard                                 | Ctrl+X              |
|                    | PasteItemToSelection                | Paste                                     | Paste item(s) from clipboard to selected folder          | Ctrl+Shift+V        |
|                    | DeleteItem                          | Delete                                    | Delete item(s)                                           | Delete, Ctrl+D      |
|                    | DeletemeItemPermanently             | Delete permanently                        | Delete item(s) permanently                               | Shift+Delete        |
|                    | CreateFolder                        | Folder                                    | Create new folder                                        |                     |
|                    | CreateFolderWithSelection           | Create folder with selection              | Create a folder with the currently selected item(s)      |                     |
|                    | AddItem                             | New                                       | Create new item                                          | Ctrl+Shift+N        |
|                    | CreateShortcut                      | Create shortcut                           | Create new shortcut(s) to selected item(s)               |                     |
|                    | CreateShortcutFromDialog            | Shortcut                                  | Create new shortcut to any item                          |                     |
|                    | EmptyRecycleBin                     | Empty Recycle Bin                         | Empty recycle bin                                        |                     |
|                    | FormatDrive                         | Format...                                 | Open "Format Drive" menu for selected item               |                     |
|                    | RestoreRecycleBin                   | Restore                                   | Restore selected item(s) from recycle bin                |                     |
|                    | RestoreAllRecycleBin                | Restore All Items                         | Restore all items from recycle bin                       |                     |
|                    | OpenItem                            | Open                                      | Open item(s)                                             | Enter               |
|                    | OpenItemWithApplicationPicker       | Open with                                 | Open item(s) with selected application                   |                     |
|                    | OpenParentFolder                    | Open parent folder                        | Open parent folder of searched item                      |                     |
|                    | OpenFileLocation                    | Open file location                        | Open the item's location                                 |                     |
|                    | RefreshItems                        | Refresh                                   | Refresh page contents                                    | Ctrl+R, F5          |
|                    | Rename                              | Rename                                    | Rename selected item                                     | F2                  |
| Selection          | SelectAll                           | Select All                                | Select all items                                         | Ctrl+A              |
|                    | InvertSelection                     | Invert Selection                          | Invert item selection                                    |                     |
|                    | ClearSelection                      | Clear Selection                           | Clear item selection                                     |                     |
|                    | ToggleSelect                        | Toggle Selection                          | Toggle item selection                                    | Ctrl+Space          |
| Share              | ShareItem                           | Share                                     | Share selected file(s) with others                       |                     |
| Start              | PinToStart                          | Pin to the Start Menu                     | Pin item(s) to the Start Menu                            |                     |
|                    | UnpinFromStart                      | Unpin from the Start Menu                 | Unpin item(s) from the Start Menu                        |                     |
| Favorites          | PinItemToFavorites                  | Pin to Favorites                          | Pin folder(s) to Favorites                               |                     |
|                    | UnpinItemFromFavorites              | Unpin from Favorites                      | Unpin folder(s) from Favorites                           |                     |
| Backgrounds        | SetAsWallpaperBackground            | Set as desktop background                 | Set selected picture as desktop background               |                     |
|                    | SetAsSlideshowBackground            | Set as desktop slideshow                  | Set selected pictures as desktop slideshow               |                     |
|                    | SetAsLockscreenBackground           | Set as lockscreen background              | Set selected picture as lockscreen background            |                     |
| Install            | InstallFont                         | Install                                   | Install selected font(s)                                 |                     |
|                    | InstallInfDriver                    | Install                                   | Install driver(s) using selected inf file(s)             |                     |
|                    | InstallCertificate                  | Install                                   | Install selected certificate(s)                          |                     |
| Run                | RunAsAdmin                          | Run as administrator                      | Run selected application as administrator                |                     |
|                    | RunAsAnotherUser                    | Run as another user                       | Run selected application as another user                 |                     |
|                    | RunWithPowershell                   | Run with PowerShell                       | Run selected PowerShell script                           |                     |
| Preview Popup      | LaunchPreviewPopup                  | Launch preview popup                      | Launch preview in popup window                           | Space               |
| Archives           | CompressIntoArchive                 | Create archive                            | Create archive with selected item(s)                     |                     |
|                    | CompressIntoSevenZip                | Create _ArchiveName_.7z                   | Create 7z archive instantly with selected item(s)        |                     |
|                    | CompressIntoZip                     | Create _ArchiveName_.zip                  | Create zip archive instantly with selected item(s)       |                     |
|                    | DecompressArchive                   | Extract files                             | Extract items from selected archive(s) to any folder     | Ctrl+E              |
|                    | DecompressArchiveHere               | Extract here                              | Extract items from selected archive(s) to current folder |                     |
|                    | DecompressArchiveToChildFolder      | Extract to _NewFolderName_                | Extract items from selected archive(s) to new folder     |                     |
| Image Manipulation | RotateLeft                          | Rotate left                               | Rotate selected image(s) to the left                     |                     |
|                    | RotateRight                         | Rotate right                              | Rotate selected image(s) to the right                    |                     |
| Open               | OpenInVS                            | Visual Studio                             | Open the current directory in Visual Studio              |                     |
|                    | OpenInVSCode                        | VS Code                                   | Open the current directory in Visual Studio Code         |                     |
|                    | OpenProperties                      | Open properties                           | Open properties window                                   | Alt+Enter           |
|                    | OpenSettings                        | Settings                                  | Open settings page                                       | Ctrl+,              |
|                    | OpenTerminal                        | Open in terminal                          | Open folder in terminal                                  | Ctrl+\`             |
|                    | OpenTerminalAsAdmin                 | Open in terminal as administrator         | Open folder in terminal as administrator                 | Ctrl+Shift+\`       |
|                    | OpenCommandPalette                  | Command palette                           | Open command palette                                     | Ctrl+Shift+P        |
| Layout             | LayoutDecreaseSize                  | Decrease size                             | Decrease icon size in grid view                          | Ctrl+-              |
|                    | LayoutIncreaseSize                  | Increase size                             | Increase icon size in grid view                          | Ctrl++              |
|                    | LayoutDetails                       | Details                                   | Switch to details view                                   | Ctrl+Shift+1        |
|                    | LayoutTiles                         | Tiles                                     | Switch to tiles view                                     | Ctrl+Shift+2        |
|                    | LayoutGridSmall                     | Small Icons                               | Switch to grid view with small icons                     | Ctrl+Shift+3        |
|                    | LayoutGridMedium                    | Medium Icons                              | Switch to grid view with medium icons                    | Ctrl+Shift+4        |
|                    | LayoutGridLarge                     | Large Icons                               | Switch to grid view with large icons                     | Ctrl+Shift+5        |
|                    | LayoutColumns                       | Columns                                   | Switch to columns view                                   | Ctrl+Shift+6        |
|                    | LayoutAdaptive                      | Adaptive                                  | Switch views adaptively                                  | Ctrl+Shift+7        |
| Sort by            | SortByName                          | Name                                      | Sort items by name                                       |                     |
|                    | SortByDateModified                  | Date modified                             | Sort items by date modified                              |                     |
|                    | SortByDateCreated                   | Date created                              | Sort items by date created                               |                     |
|                    | SortBySize                          | Size                                      | Sort items by size                                       |                     |
|                    | SortByType                          | Type                                      | Sort items by type                                       |                     |
|                    | SortBySyncStatus                    | Sync status                               | Sort items by sync status                                |                     |
|                    | SortByTag                           | Tags                                      | Sort items by tags                                       |                     |
|                    | SortByPath                          | Path                                      | Sort items by path                                       |                     |
|                    | SortByOriginalFolder                | Original folder                           | Sort items by original folder                            |                     |
|                    | SortByDateDeleted                   | Date deleted                              | Sort items by date deleted                               |                     |
|                    | SortAscending                       | Ascending                                 | Sort items in ascending order                            |                     |
|                    | SortDescending                      | Descending                                | Sort items in descending order                           |                     |
|                    | ToggleSortDirection                 | Toggle sort direction                     | Toggle item sort direction                               |                     |
|                    | ToggleSortDirectoriesAlongsideFiles | List and sort directories alongside files | List and sort directories alongside files                |                     |
| Group by           | GroupByNone                         | None                                      | List items without grouping                              |                     |
|                    | GroupByName                         | Name                                      | Group items by name                                      |                     |
|                    | GroupByDateModified                 | Date modified                             | Group items by date modified                             |                     |
|                    | GroupByDateCreated                  | Date created                              | Group items by date created                              |                     |
|                    | GroupBySize                         | Size                                      | Group items by size                                      |                     |
|                    | GroupByType                         | Type                                      | Group items by type                                      |                     |
|                    | GroupBySyncStatus                   | Sync status                               | Group items by sync status                               |                     |
|                    | GroupByTag                          | Tags                                      | Group items by tags                                      |                     |
|                    | GroupByOriginalFolder               | Original folder                           | Group items by original folder                           |                     |
|                    | GroupByDateDeleted                  | Date deleted                              | Group items by date deleted                              |                     |
|                    | GroupByFolderPath                   | Folder path                               | Group items by folder path                               |                     |
|                    | GroupByDateModifiedYear             | Year                                      | Group items by year of date modified                     |                     |
|                    | GroupByDateModifiedMonth            | Month                                     | Group items by month of date modified                    |                     |
|                    | GroupByDateModifiedDay              | Day                                       | Group items by day of date modified                      |                     |
|                    | GroupByDateCreatedYear              | Year                                      | Group items by year of date created                      |                     |
|                    | GroupByDateCreatedMonth             | Month                                     | Group items by month of date created                     |                     |
|                    | GroupByDateCreatedDay               | Day                                       | Group items by day of date created                       |                     |
|                    | GroupByDateDeletedYear              | Year                                      | Group items by year of date deleted                      |                     |
|                    | GroupByDateDeletedMonth             | Month                                     | Group items by month of date deleted                     |                     |
|                    | GroupByDateDeletedDay               | Day                                       | Group items by day of date deleted                       |                     |
|                    | GroupAscending                      | Ascending                                 | Sort groups in ascending order                           |                     |
|                    | GroupDescending                     | Descending                                | Sort groups in descending order                          |                     |
|                    | ToggleGroupDirection                | Toggle sort direction                     | Toggle group sort direction                              |                     |
|                    | GroupByYear                         | Year                                      | Group items by year                                      |                     |
|                    | GroupByMonth                        | Month                                     | Group items by month                                     |                     |
|                    | ToggleGroupByDateUnit               | Toggle grouping unit                      | Toggle unit for grouping by date                         |                     |
| Navigation         | NewTab                              | New tab                                   | Open new tab                                             | Ctrl+T              |
|                    | NavigateBack                        | Back                                      | Navigate backward in navigation history                  | Alt+Left, Backspace |
|                    | NavigateForward                     | Forward                                   | Navigate forward in navigation history                   | Alt+Right           |
|                    | NavigateUp                          | Up                                        | Navigate up one directory                                | Alt+Up              |
| Other              | DuplicateCurrentTab                 | Duplicate tab                             | Duplicate current tab                                    |                     |
|                    | DuplicateSelectedTab                | Duplicate tab                             | Duplicate selected tab                                   | Ctrl+Shift+K        |
|                    | CloseTabsToTheLeftCurrent           | Close tabs to the left                    | Close tabs to the left of current tab                    |                     |
|                    | CloseTabsToTheLeftSelected          | Close tabs to the left                    | Close tabs to the left of selected tab                   |                     |
|                    | CloseTabsToTheRightCurrent          | Close tabs to the right                   | Close tabs to the right of current tab                   |                     |
|                    | CloseTabsToTheRightSelected         | Close tabs to the right                   | Close tabs to the right of selected tab                  |                     |
|                    | CloseOtherTabsCurrent               | Close other tabs                          | Close tabs other than current tab                        |                     |
|                    | CloseOtherTabsSelected              | Close other tabs                          | Close tabs other than selected tab                       |                     |
|                    | OpenDirectoryInNewPane              | Open in new pane                          | Open directory in new pane                               |                     |
|                    | OpenDirectoryInNewTab               | Open in new tab                           | Open directory in new tab                                |                     |
|                    | OpenInNewWindowItem                 | Open in new window                        | Open directory in new window                             |                     |
|                    | ReopenClosedTab                     | Reopen closed tab                         | Reopen last closed tab                                   | Ctrl+Shift+T        |
|                    | PreviousTab                         | Moves to the previous tab                 | Move to the previous tab                                 | Ctrl+Shift+Tab      |
|                    | NextTab                             | Moves to the next tab                     | Move to the next tab                                     | Ctrl+Tab            |
|                    | CloseSelectedTab                    | Closes current tab                        | Close current tab                                        | Ctrl+W, Ctrl+F4     |
|                    | OpenNewPane                         | New pane                                  | Open new pane                                            | Alt+Shift++         |
|                    | ClosePane                           | Close pane                                | Close right pane                                         | Ctrl+Shift+W        |
| Play               | PlayAll                             | Play all                                  | Play the selected media files                            |                     |
| Git                | GitFetch                            | Fetch                                     | Run git fetch                                            |                     |
|                    | GitInit                             | Initialize repo                           | Initialize a Git repository                              |                     |
|                    | GitPull                             | Pull                                      | Run git pull                                             |                     |
|                    | GitPush                             | Push                                      | Run git push                                             |                     |
|                    | GitSync                             | Sync                                      | Run git pull and then git push                           |                     |
| Tags               | OpenAllTaggedItems                  | Open all                                  | Open all tagged items                                    |                     |
