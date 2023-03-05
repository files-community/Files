using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.SecureStore;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Files.App.ViewModels.Dialogs
{
	public class DecompressArchiveViewModel : ObservableObject
	{
		private readonly IStorageFile archive;

		public StorageFolder DestinationFolder { get; private set; }

		private string destinationFolderPath;
		public string DestinationFolderPath
		{
			get => destinationFolderPath;
			private set => SetProperty(ref destinationFolderPath, value);
		}

		private bool openDestinationFolderOnCompletion;
		public bool OpenDestinationFolderOnCompletion
		{
			get => openDestinationFolderOnCompletion;
			set => SetProperty(ref openDestinationFolderOnCompletion, value);
		}

		private bool isArchiveEncrypted;
		public bool IsArchiveEncrypted
		{
			get => isArchiveEncrypted;
			set => SetProperty(ref isArchiveEncrypted, value);
		}

		private bool showPathSelection;
		public bool ShowPathSelection
		{
			get => showPathSelection;
			set => SetProperty(ref showPathSelection, value);
		}

		public DisposableArray? Password { get; private set; }

		public IRelayCommand PrimaryButtonClickCommand { get; private set; }

		public ICommand SelectDestinationCommand { get; private set; }

		public DecompressArchiveViewModel(IStorageFile archive)
		{
			this.archive = archive;
			destinationFolderPath = DefaultDestinationFolderPath();

			// Create commands
			SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
			PrimaryButtonClickCommand = new RelayCommand<DisposableArray>(password => Password = password);
		}

		private async Task SelectDestination()
		{
			FolderPicker folderPicker = InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");

			DestinationFolder = await folderPicker.PickSingleFolderAsync();

			DestinationFolderPath = (DestinationFolder is not null) ? DestinationFolder.Path : DefaultDestinationFolderPath();
		}

		// WINUI3
		private FolderPicker InitializeWithWindow(FolderPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private string DefaultDestinationFolderPath()
		{
			return Path.Combine(Path.GetDirectoryName(archive.Path), Path.GetFileNameWithoutExtension(archive.Path));
		}
	}
}
