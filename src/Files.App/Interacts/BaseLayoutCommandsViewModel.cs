using Files.Shared;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace Files.App.Interacts
{
    public class BaseLayoutCommandsViewModel : IDisposable
    {
        #region Constructor

        public BaseLayoutCommandsViewModel(IBaseLayoutCommandImplementationModel commandsModel)
        {
            this.CommandsModel = commandsModel;

            InitializeCommands();
        }

        #endregion Constructor


        public IBaseLayoutCommandImplementationModel CommandsModel { get; }

        #region Command Initialization

        private void InitializeCommands()
        {
            RenameItemCommand = new RelayCommand(CommandsModel.RenameItem);
            CreateShortcutCommand = new RelayCommand(CommandsModel.CreateShortcut);
            SetAsLockscreenBackgroundItemCommand = new RelayCommand(CommandsModel.SetAsLockscreenBackgroundItem);
            SetAsDesktopBackgroundItemCommand = new RelayCommand(CommandsModel.SetAsDesktopBackgroundItem);
            SetAsSlideshowItemCommand = new RelayCommand(CommandsModel.SetAsSlideshowItem);
            RunAsAdminCommand = new RelayCommand(CommandsModel.RunAsAdmin);
            RunAsAnotherUserCommand = new RelayCommand(CommandsModel.RunAsAnotherUser);
            SidebarPinItemCommand = new RelayCommand(CommandsModel.SidebarPinItem);
            SidebarUnpinItemCommand = new RelayCommand(CommandsModel.SidebarUnpinItem);
            UnpinDirectoryFromFavoritesCommand = new RelayCommand(CommandsModel.UnpinDirectoryFromFavorites);
            OpenItemCommand = new RelayCommand(CommandsModel.OpenItem);
            EmptyRecycleBinCommand = new RelayCommand(CommandsModel.EmptyRecycleBin);
            RestoreRecycleBinCommand = new RelayCommand(CommandsModel.RestoreRecycleBin);
            RestoreSelectionRecycleBinCommand = new RelayCommand(CommandsModel.RestoreSelectionRecycleBin);
            QuickLookCommand = new RelayCommand(CommandsModel.QuickLook);
            CopyItemCommand = new RelayCommand(CommandsModel.CopyItem);
            CutItemCommand = new RelayCommand(CommandsModel.CutItem);
            RestoreItemCommand = new RelayCommand(CommandsModel.RestoreItem);
            DeleteItemCommand = new RelayCommand(CommandsModel.DeleteItem);
            ShowFolderPropertiesCommand = new RelayCommand(CommandsModel.ShowFolderProperties);
            ShowPropertiesCommand = new RelayCommand(CommandsModel.ShowProperties);
            OpenFileLocationCommand = new RelayCommand(CommandsModel.OpenFileLocation);
            OpenParentFolderCommand = new RelayCommand(CommandsModel.OpenParentFolder);
            OpenItemWithApplicationPickerCommand = new RelayCommand(CommandsModel.OpenItemWithApplicationPicker);
            OpenDirectoryInNewTabCommand = new RelayCommand(CommandsModel.OpenDirectoryInNewTab);
            OpenDirectoryInNewPaneCommand = new RelayCommand(CommandsModel.OpenDirectoryInNewPane);
            OpenInNewWindowItemCommand = new RelayCommand(CommandsModel.OpenInNewWindowItem);
            CreateNewFolderCommand = new RelayCommand(CommandsModel.CreateNewFolder);
            CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CommandsModel.CreateNewFile);
            PasteItemsFromClipboardCommand = new RelayCommand(CommandsModel.PasteItemsFromClipboard);
            CopyPathOfSelectedItemCommand = new RelayCommand(CommandsModel.CopyPathOfSelectedItem);
            OpenDirectoryInDefaultTerminalCommand = new RelayCommand(CommandsModel.OpenDirectoryInDefaultTerminal);
            ShareItemCommand = new RelayCommand(CommandsModel.ShareItem);
            PinDirectoryToFavoritesCommand = new RelayCommand(CommandsModel.PinDirectoryToFavorites);
            ItemPointerPressedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.ItemPointerPressed);
            UnpinItemFromStartCommand = new RelayCommand(CommandsModel.UnpinItemFromStart);
            PinItemToStartCommand = new RelayCommand(CommandsModel.PinItemToStart);
            PointerWheelChangedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.PointerWheelChanged);
            GridViewSizeDecreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CommandsModel.GridViewSizeDecrease);
            GridViewSizeIncreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CommandsModel.GridViewSizeIncrease);
            DragOverCommand = new AsyncRelayCommand<DragEventArgs>(CommandsModel.DragOver);
            DropCommand = new AsyncRelayCommand<DragEventArgs>(CommandsModel.Drop);
            RefreshCommand = new RelayCommand(CommandsModel.RefreshItems);
            SearchUnindexedItems = new RelayCommand(CommandsModel.SearchUnindexedItems);
            CreateFolderWithSelection = new AsyncRelayCommand(CommandsModel.CreateFolderWithSelection);
            DecompressArchiveCommand = new AsyncRelayCommand(CommandsModel.DecompressArchive);
            DecompressArchiveHereCommand = new AsyncRelayCommand(CommandsModel.DecompressArchiveHere);
            DecompressArchiveToChildFolderCommand = new AsyncRelayCommand(CommandsModel.DecompressArchiveToChildFolder);
            InstallInfDriver = new AsyncRelayCommand(CommandsModel.InstallInfDriver);
            RotateImageLeftCommand = new AsyncRelayCommand(CommandsModel.RotateImageLeft);
            RotateImageRightCommand = new AsyncRelayCommand(CommandsModel.RotateImageRight);
            InstallFontCommand = new AsyncRelayCommand(CommandsModel.InstallFont);
        }

        #endregion Command Initialization

        #region Commands

        public ICommand RenameItemCommand { get; private set; }

        public ICommand CreateShortcutCommand { get; private set; }

        public ICommand SetAsLockscreenBackgroundItemCommand { get; private set; }

        public ICommand SetAsDesktopBackgroundItemCommand { get; private set; }

        public ICommand SetAsSlideshowItemCommand { get; private set; }

        public ICommand RunAsAdminCommand { get; private set; }

        public ICommand RunAsAnotherUserCommand { get; private set; }

        public ICommand SidebarPinItemCommand { get; private set; }

        public ICommand SidebarUnpinItemCommand { get; private set; }

        public ICommand OpenItemCommand { get; private set; }

        public ICommand UnpinDirectoryFromFavoritesCommand { get; private set; }

        public ICommand EmptyRecycleBinCommand { get; private set; }

        public ICommand RestoreRecycleBinCommand { get; private set; }

        public ICommand RestoreSelectionRecycleBinCommand { get; private set; }

        public ICommand QuickLookCommand { get; private set; }

        public ICommand CopyItemCommand { get; private set; }

        public ICommand CutItemCommand { get; private set; }

        public ICommand RestoreItemCommand { get; private set; }

        public ICommand DeleteItemCommand { get; private set; }

        public ICommand ShowFolderPropertiesCommand { get; private set; }

        public ICommand ShowPropertiesCommand { get; private set; }

        public ICommand OpenFileLocationCommand { get; private set; }

        public ICommand OpenParentFolderCommand { get; private set; }

        public ICommand OpenItemWithApplicationPickerCommand { get; private set; }

        public ICommand OpenDirectoryInNewTabCommand { get; private set; }

        public ICommand OpenDirectoryInNewPaneCommand { get; private set; }

        public ICommand OpenInNewWindowItemCommand { get; private set; }

        public ICommand CreateNewFolderCommand { get; private set; }

        public ICommand CreateNewFileCommand { get; private set; }

        public ICommand PasteItemsFromClipboardCommand { get; private set; }

        public ICommand CopyPathOfSelectedItemCommand { get; private set; }

        public ICommand OpenDirectoryInDefaultTerminalCommand { get; private set; }

        public ICommand ShareItemCommand { get; private set; }

        public ICommand PinDirectoryToFavoritesCommand { get; private set; }

        public ICommand ItemPointerPressedCommand { get; private set; }

        public ICommand UnpinItemFromStartCommand { get; private set; }

        public ICommand PinItemToStartCommand { get; private set; }

        public ICommand PointerWheelChangedCommand { get; private set; }

        public ICommand GridViewSizeDecreaseCommand { get; private set; }

        public ICommand GridViewSizeIncreaseCommand { get; private set; }

        public ICommand DragOverCommand { get; private set; }

        public ICommand DropCommand { get; private set; }

        public ICommand RefreshCommand { get; private set; }

        public ICommand SearchUnindexedItems { get; private set; }

        public ICommand CreateFolderWithSelection { get; private set; }

        public ICommand DecompressArchiveCommand { get; private set; }

        public ICommand DecompressArchiveHereCommand { get; private set; }

        public ICommand DecompressArchiveToChildFolderCommand { get; private set; }

        public ICommand InstallInfDriver { get; set; }

        public ICommand RotateImageLeftCommand { get; private set; }

        public ICommand RotateImageRightCommand { get; private set; }

        public ICommand InstallFontCommand { get; private set; }

        #endregion Commands

        #region IDisposable

        public void Dispose()
        {
            CommandsModel?.Dispose();
        }

        #endregion IDisposable
    }
}