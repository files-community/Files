using Files.DataModels;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Files.Interacts
{
    public class BaseLayoutCommandsViewModel : IDisposable
    {
        #region Private Members

        private readonly IBaseLayoutCommandImplementationModel commandsModel;

        #endregion Private Members

        #region Constructor

        public BaseLayoutCommandsViewModel(IBaseLayoutCommandImplementationModel commandsModel)
        {
            this.commandsModel = commandsModel;

            InitializeCommands();
        }

        #endregion Constructor

        #region Command Initialization

        private void InitializeCommands()
        {
            RenameItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.RenameItem);
            CreateShortcutCommand = new RelayCommand<RoutedEventArgs>(commandsModel.CreateShortcut);
            SetAsLockscreenBackgroundItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.SetAsLockscreenBackgroundItem);
            SetAsDesktopBackgroundItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.SetAsDesktopBackgroundItem);
            RunAsAdminCommand = new RelayCommand<RoutedEventArgs>(commandsModel.RunAsAdmin);
            RunAsAnotherUserCommand = new RelayCommand<RoutedEventArgs>(commandsModel.RunAsAnotherUser);
            SidebarPinItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.SidebarPinItem);
            SidebarUnpinItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.SidebarUnpinItem);
            UnpinDirectoryFromFavoritesCommand = new RelayCommand<RoutedEventArgs>(commandsModel.UnpinDirectoryFromFavorites);
            OpenItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenItem);
            EmptyRecycleBinCommand = new RelayCommand<RoutedEventArgs>(commandsModel.EmptyRecycleBin);
            QuickLookCommand = new RelayCommand<RoutedEventArgs>(commandsModel.QuickLook);
            CopyItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.CopyItem);
            CutItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.CutItem);
            RestoreItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.RestoreItem);
            DeleteItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.DeleteItem);
            ShowFolderPropertiesCommand = new RelayCommand<RoutedEventArgs>(commandsModel.ShowFolderProperties);
            ShowPropertiesCommand = new RelayCommand<RoutedEventArgs>(commandsModel.ShowProperties);
            OpenFileLocationCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenFileLocation);
            OpenParentFolderCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenParentFolder);
            OpenItemWithApplicationPickerCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenItemWithApplicationPicker);
            OpenDirectoryInNewTabCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenDirectoryInNewTab);
            OpenDirectoryInNewPaneCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenDirectoryInNewPane);
            OpenInNewWindowItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenInNewWindowItem);
            CreateNewFolderCommand = new RelayCommand<RoutedEventArgs>(commandsModel.CreateNewFolder);
            CreateNewFileCommand = new RelayCommand<ShellNewEntry>(commandsModel.CreateNewFile);
            PasteItemsFromClipboardCommand = new RelayCommand<RoutedEventArgs>(commandsModel.PasteItemsFromClipboard);
            CopyPathOfSelectedItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.CopyPathOfSelectedItem);
            OpenDirectoryInDefaultTerminalCommand = new RelayCommand<RoutedEventArgs>(commandsModel.OpenDirectoryInDefaultTerminal);
            ShareItemCommand = new RelayCommand<RoutedEventArgs>(commandsModel.ShareItem);
            PinDirectoryToFavoritesCommand = new RelayCommand<RoutedEventArgs>(commandsModel.PinDirectoryToFavorites);
            ItemPointerPressedCommand = new RelayCommand<PointerRoutedEventArgs>(commandsModel.ItemPointerPressed);
            UnpinItemFromStartCommand = new RelayCommand<RoutedEventArgs>(commandsModel.UnpinItemFromStart);
            PinItemToStartCommand = new RelayCommand<RoutedEventArgs>(commandsModel.PinItemToStart);
            PointerWheelChangedCommand = new RelayCommand<PointerRoutedEventArgs>(commandsModel.PointerWheelChanged);
            GridViewSizeDecreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(commandsModel.GridViewSizeDecrease);
            GridViewSizeIncreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(commandsModel.GridViewSizeIncrease);
            DragOverCommand = new RelayCommand<DragEventArgs>(commandsModel.DragOver);
            DropCommand = new RelayCommand<DragEventArgs>(commandsModel.Drop);
            RefreshCommand = new RelayCommand<RoutedEventArgs>(commandsModel.RefreshItems);
            SearchUnindexedItems = new RelayCommand<RoutedEventArgs>(commandsModel.SearchUnindexedItems);
            CreateFolderWithSelection = new RelayCommand<RoutedEventArgs>(commandsModel.CreateFolderWithSelection);
            DecompressArchiveCommand = new RelayCommand(commandsModel.DecompressArchive);
            DecompressArchiveHereCommand = new RelayCommand(commandsModel.DecompressArchiveHere);
            DecompressArchiveToChildFolderCommand = new RelayCommand(commandsModel.DecompressArchiveToChildFolder);
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
            commandsModel?.Dispose();
        }

        #endregion IDisposable
    }
}