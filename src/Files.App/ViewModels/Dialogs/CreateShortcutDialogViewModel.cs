using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;

namespace Files.App.ViewModels.Dialogs
{
	public class CreateShortcutDialogViewModel : ObservableObject
	{
		public readonly string WorkingDirectory;

		private string _destinationItemPath;
		public string DestinationItemPath
		{
			get => _destinationItemPath;
			set => SetProperty(ref _destinationItemPath, value);
		}

		private bool _isLocationValid = true;
		public bool IsLocationValid
		{
			get => _isLocationValid;
			set => SetProperty(ref _isLocationValid, value);
		}

		public ICommand SelectDestinationCommand { get; private set; }

		public CreateShortcutDialogViewModel(string workingDirectory)
		{
			WorkingDirectory = workingDirectory;
			_destinationItemPath = string.Empty;

			SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
		}

		private async Task SelectDestination()
		{
			var folderPicker = InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");

			var selectedFolder = await folderPicker.PickSingleFolderAsync();
			if (selectedFolder is not null)
				DestinationItemPath = selectedFolder.Path;
		}

		private FolderPicker InitializeWithWindow(FolderPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}
	}
}
