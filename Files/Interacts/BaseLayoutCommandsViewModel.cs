using Files.Common;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Files.Interacts
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
            RenameItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RenameItem);
            CreateShortcutCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.CreateShortcut);
            SetAsLockscreenBackgroundItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.SetAsLockscreenBackgroundItem);
            SetAsDesktopBackgroundItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.SetAsDesktopBackgroundItem);
            RunAsAdminCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RunAsAdmin);
            RunAsAnotherUserCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RunAsAnotherUser);
            SidebarPinItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.SidebarPinItem);
            SidebarUnpinItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.SidebarUnpinItem);
            UnpinDirectoryFromFavoritesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.UnpinDirectoryFromFavorites);
            OpenItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenItem);
            EmptyRecycleBinCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.EmptyRecycleBin);
            QuickLookCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.QuickLook);
            CopyItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.CopyItem);
            CutItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.CutItem);
            RestoreItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RestoreItem);
            DeleteItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.DeleteItem);
            ShowFolderPropertiesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShowFolderProperties);
            ShowPropertiesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShowProperties);
            OpenFileLocationCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenFileLocation);
            OpenParentFolderCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenParentFolder);
            OpenItemWithApplicationPickerCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenItemWithApplicationPicker);
            OpenDirectoryInNewTabCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInNewTab);
            OpenDirectoryInNewPaneCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInNewPane);
            OpenInNewWindowItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenInNewWindowItem);
            CreateNewFolderCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.CreateNewFolder);
            CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CommandsModel.CreateNewFile);
            PasteItemsFromClipboardCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.PasteItemsFromClipboard);
            CopyPathOfSelectedItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.CopyPathOfSelectedItem);
            OpenDirectoryInDefaultTerminalCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInDefaultTerminal);
            ShareItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShareItem);
            PinDirectoryToFavoritesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.PinDirectoryToFavorites);
            ItemPointerPressedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.ItemPointerPressed);
            UnpinItemFromStartCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.UnpinItemFromStart);
            PinItemToStartCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.PinItemToStart);
            PointerWheelChangedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.PointerWheelChanged);
            GridViewSizeDecreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CommandsModel.GridViewSizeDecrease);
            GridViewSizeIncreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CommandsModel.GridViewSizeIncrease);
            DragOverCommand = new RelayCommand<DragEventArgs>(e => _ = CommandsModel.DragOver(e));
            DropCommand = new RelayCommand<DragEventArgs>(e => _ = CommandsModel.Drop(e));
            RefreshCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RefreshItems);
            SearchUnindexedItems = new RelayCommand<RoutedEventArgs>(CommandsModel.SearchUnindexedItems);
            CreateFolderWithSelection = new RelayCommand<RoutedEventArgs>(CommandsModel.CreateFolderWithSelection);
            DecompressArchiveCommand = new RelayCommand(CommandsModel.DecompressArchive);
            DecompressArchiveHereCommand = new RelayCommand(CommandsModel.DecompressArchiveHere);
            DecompressArchiveToChildFolderCommand = new RelayCommand(CommandsModel.DecompressArchiveToChildFolder);
        }

        #endregion Command Initialization

        #region Commands

        public ICommand RenameItemCommand { get; private set; }

        public ICommand CreateShortcutCommand { get; private set; }

        public ICommand SetAsLockscreenBackgroundItemCommand { get; private set; }

        public ICommand SetAsDesktopBackgroundItemCommand { get; private set; }

        public ICommand RunAsAdminCommand { get; private set; }

        public ICommand RunAsAnotherUserCommand { get; private set; }

        public ICommand SidebarPinItemCommand { get; private set; }

        public ICommand SidebarUnpinItemCommand { get; private set; }

        public ICommand OpenItemCommand { get; private set; }

        public ICommand UnpinDirectoryFromFavoritesCommand { get; private set; }

        public ICommand EmptyRecycleBinCommand { get; private set; }

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

        #endregion Commands

        #region IDisposable

        public void Dispose()
        {
            CommandsModel?.Dispose();
        }

        #endregion IDisposable
    }
}