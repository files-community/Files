using CommunityToolkit.Mvvm.Input;
using Files.App.Filesystem;
using Files.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Windows.Input;

namespace Files.App.Interacts
{
	public class BaseLayoutCommandsViewModel : IDisposable
	{
		#region Constructor

		public BaseLayoutCommandsViewModel(IBaseLayoutCommandImplementationModel commandsModel)
		{
			CommandsModel = commandsModel;

			InitializeCommands();
		}

		#endregion Constructor

		public IBaseLayoutCommandImplementationModel CommandsModel { get; }

		#region Command Initialization

		private void InitializeCommands()
		{
			RenameItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RenameItem);
			RunAsAdminCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RunAsAdmin);
			RunAsAnotherUserCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RunAsAnotherUser);
			OpenItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenItem);
			RestoreRecycleBinCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RestoreRecycleBin);
			RestoreSelectionRecycleBinCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RestoreSelectionRecycleBin);
			QuickLookCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.QuickLook);
			RestoreItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RestoreItem);
			ShowFolderPropertiesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShowFolderProperties);
			ShowPropertiesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShowProperties);
			OpenFileLocationCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenFileLocation);
			OpenParentFolderCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenParentFolder);
			OpenItemWithApplicationPickerCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenItemWithApplicationPicker);
			OpenDirectoryInNewTabCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInNewTab);
			OpenDirectoryInNewPaneCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInNewPane);
			OpenInNewWindowItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenInNewWindowItem);
			CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CommandsModel.CreateNewFile);
			PasteItemsFromClipboardCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.PasteItemsFromClipboard);
			CopyPathOfSelectedItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.CopyPathOfSelectedItem);
			ShareItemCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShareItem);
			ItemPointerPressedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.ItemPointerPressed);
			PointerWheelChangedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.PointerWheelChanged);
			GridViewSizeDecreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CommandsModel.GridViewSizeDecrease);
			GridViewSizeIncreaseCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CommandsModel.GridViewSizeIncrease);
			DragOverCommand = new AsyncRelayCommand<DragEventArgs>(CommandsModel.DragOver);
			DropCommand = new AsyncRelayCommand<DragEventArgs>(CommandsModel.Drop);
			RefreshCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.RefreshItems);
			SearchUnindexedItems = new RelayCommand<RoutedEventArgs>(CommandsModel.SearchUnindexedItems);
			CreateFolderWithSelection = new AsyncRelayCommand<RoutedEventArgs>(CommandsModel.CreateFolderWithSelection);
			CompressIntoSevenZipCommand = new AsyncRelayCommand(CommandsModel.CompressIntoSevenZip);
			DecompressArchiveCommand = new AsyncRelayCommand(CommandsModel.DecompressArchive);
			DecompressArchiveHereCommand = new AsyncRelayCommand(CommandsModel.DecompressArchiveHere);
			DecompressArchiveToChildFolderCommand = new AsyncRelayCommand(CommandsModel.DecompressArchiveToChildFolder);
			InstallInfDriver = new AsyncRelayCommand(CommandsModel.InstallInfDriver);
			RotateImageLeftCommand = new AsyncRelayCommand(CommandsModel.RotateImageLeft);
			RotateImageRightCommand = new AsyncRelayCommand(CommandsModel.RotateImageRight);
			InstallFontCommand = new AsyncRelayCommand(CommandsModel.InstallFont);
			PlayAllCommand = new AsyncRelayCommand(CommandsModel.PlayAll);
			FormatDriveCommand = new RelayCommand<ListedItem>(CommandsModel.FormatDrive);
		}

		#endregion Command Initialization

		#region Commands

		public ICommand RenameItemCommand { get; private set; }

		public ICommand RunAsAdminCommand { get; private set; }

		public ICommand RunAsAnotherUserCommand { get; private set; }

		public ICommand OpenItemCommand { get; private set; }

		public ICommand RestoreRecycleBinCommand { get; private set; }

		public ICommand RestoreSelectionRecycleBinCommand { get; private set; }

		public ICommand QuickLookCommand { get; private set; }

		public ICommand RestoreItemCommand { get; private set; }

		public ICommand ShowFolderPropertiesCommand { get; private set; }

		public ICommand ShowPropertiesCommand { get; private set; }

		public ICommand OpenFileLocationCommand { get; private set; }

		public ICommand OpenParentFolderCommand { get; private set; }

		public ICommand OpenItemWithApplicationPickerCommand { get; private set; }

		public ICommand OpenDirectoryInNewTabCommand { get; private set; }

		public ICommand OpenDirectoryInNewPaneCommand { get; private set; }

		public ICommand OpenInNewWindowItemCommand { get; private set; }

		public ICommand CreateNewFileCommand { get; private set; }

		public ICommand PasteItemsFromClipboardCommand { get; private set; }

		public ICommand CopyPathOfSelectedItemCommand { get; private set; }

		public ICommand ShareItemCommand { get; private set; }

		public ICommand ItemPointerPressedCommand { get; private set; }

		public ICommand PointerWheelChangedCommand { get; private set; }

		public ICommand GridViewSizeDecreaseCommand { get; private set; }

		public ICommand GridViewSizeIncreaseCommand { get; private set; }

		public ICommand DragOverCommand { get; private set; }

		public ICommand DropCommand { get; private set; }

		public ICommand RefreshCommand { get; private set; }

		public ICommand SearchUnindexedItems { get; private set; }

		public ICommand CreateFolderWithSelection { get; private set; }

		public ICommand CompressIntoSevenZipCommand { get; private set; }

		public ICommand DecompressArchiveCommand { get; private set; }

		public ICommand DecompressArchiveHereCommand { get; private set; }

		public ICommand DecompressArchiveToChildFolderCommand { get; private set; }

		public ICommand InstallInfDriver { get; set; }

		public ICommand RotateImageLeftCommand { get; private set; }

		public ICommand RotateImageRightCommand { get; private set; }

		public ICommand InstallFontCommand { get; private set; }

		public ICommand PlayAllCommand { get; private set; }
		public ICommand FormatDriveCommand { get; private set; }

		#endregion Commands

		#region IDisposable

		public void Dispose()
		{
			CommandsModel?.Dispose();
		}

		#endregion IDisposable
	}
}
