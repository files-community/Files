﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Helpers;
using Files.Backend.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;

namespace Files.App.ViewModels.Dialogs
{
	public class CreateShortcutDialogViewModel : ObservableObject
	{
		// User's working directory
		public readonly string WorkingDirectory;

		// Tells whether destination path exists
		public bool DestinationPathExists { get; set; } = false;

		// Destination of the shortcut chosen by the user (can be a path or a URL)
		private string _destinationItemPath;
		public string DestinationItemPath
		{
			get => _destinationItemPath;
			set => SetProperty(ref _destinationItemPath, value);
		}

		// Tells if the selected destination is valid (Path exists or URL is well-formed). Used to enable primary button
		private bool _isLocationValid;
		public bool IsLocationValid
		{
			get => _isLocationValid;
			set => SetProperty(ref _isLocationValid, value);
		}

		// Command invoked when the user clicks the 'Browse' button
		public ICommand SelectDestinationCommand { get; private set; }

		// Command invoked when the user clicks primary button
		public ICommand PrimaryButtonCommand { get; private set; }

		public CreateShortcutDialogViewModel(string workingDirectory)
		{
			WorkingDirectory = workingDirectory;
			_destinationItemPath = string.Empty;

			SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
			PrimaryButtonCommand = new AsyncRelayCommand(CreateShortcut);
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

		private async Task CreateShortcut()
		{
			string? destinationName;
			var extension = DestinationPathExists ? ".lnk" : ".url";

			if (DestinationPathExists)
			{
				destinationName = Path.GetFileName(DestinationItemPath);
				destinationName ??= Path.GetDirectoryName(DestinationItemPath);
			}
			else
			{
				var uri = new Uri(DestinationItemPath);
				destinationName = uri.Host;
			}

			var shortcutName = string.Format("ShortcutCreateNewSuffix".ToLocalized(), destinationName);
			var filePath = Path.Combine(
				WorkingDirectory,
				shortcutName + extension);

			int fileNumber = 1;
			while (Path.Exists(filePath))
			{
				filePath = Path.Combine(
					WorkingDirectory,
					shortcutName + $" ({++fileNumber})" + extension);
			}

			await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, DestinationItemPath);
		}
	}
}
