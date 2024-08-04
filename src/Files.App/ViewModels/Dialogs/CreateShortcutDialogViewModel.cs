﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Files.Shared.Helpers;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class CreateShortcutDialogViewModel : ObservableObject
	{
		// User's working directory
		public readonly string WorkingDirectory;

		// Placeholder text of destination path textbox
		public readonly string DestinationPlaceholder = $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\Users\";

		// Tells whether destination path exists
		public bool DestinationPathExists { get; set; }

		// Tells wheteher the shortcut has been created
		public bool ShortcutCreatedSuccessfully { get; private set; }

		// Shortcut name with extension
		public string ShortcutCompleteName { get; private set; } = string.Empty;

		// Destination of the shortcut chosen by the user (can be a path or a URL)
		private string _destinationItemPath;
		public string DestinationItemPath
		{
			get => _destinationItemPath;
			set
			{
				if (!SetProperty(ref _destinationItemPath, value))
					return;

				OnPropertyChanged(nameof(ShowWarningTip));
				if (string.IsNullOrWhiteSpace(DestinationItemPath))
				{
					IsLocationValid = false;
					return;
				}

				try
				{
					if (Path.Exists(DestinationItemPath) && DestinationItemPath != Path.GetPathRoot(DestinationItemPath))
					{
						DestinationPathExists = true;
						IsLocationValid = true;
						_isLocationInPath = false;
						_fullPath = DestinationItemPath;
						return;
					}

					var fileName = DestinationItemPath.Split(' ')[0];
					if (Path.GetFileName(fileName) == fileName && PathHelpers.TryGetFullPath(fileName, out _fullPath))
					{
						DestinationPathExists = true;
						IsLocationValid = true;
						_isLocationInPath = true;
						return;
					}

					var uri = new Uri(DestinationItemPath);
					DestinationPathExists = false;
					IsLocationValid = uri.IsWellFormedOriginalString();
					_isLocationInPath = false;
					_fullPath = DestinationItemPath;
				}
				catch (Exception)
				{
					DestinationPathExists = false;
					IsLocationValid = false;
					_isLocationInPath = false;
				}
			}
		}

		// Tells if the selected destination is valid (Path exists or URL is well-formed). Used to enable primary button
		private bool _isLocationValid;
		public bool IsLocationValid
		{
			get => _isLocationValid;
			set
			{
				if (SetProperty(ref _isLocationValid, value))
					OnPropertyChanged(nameof(ShowWarningTip));
			}
		}

		private bool _isLocationInPath;
		private string _fullPath;

		public bool ShowWarningTip => !string.IsNullOrEmpty(DestinationItemPath) && !_isLocationValid;

		// Command invoked when the user clicks the 'Browse' button
		public ICommand SelectDestinationCommand { get; private set; }

		// Command invoked when the user clicks primary button
		public ICommand PrimaryButtonCommand { get; private set; }

		public CreateShortcutDialogViewModel(string workingDirectory)
		{
			WorkingDirectory = workingDirectory;
			_destinationItemPath = string.Empty;

			SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
			PrimaryButtonCommand = new AsyncRelayCommand(CreateShortcutAsync);
		}

		private Task SelectDestination()
		{
			Win32PInvoke.BROWSEINFO bi = new Win32PInvoke.BROWSEINFO();
			bi.ulFlags = 0x00004000;
			bi.lpszTitle = "Select a folder";
			nint pidl = Win32PInvoke.SHBrowseForFolder(ref bi);
			if (pidl != nint.Zero)
			{
				StringBuilder path = new StringBuilder(260);
				if (Win32PInvoke.SHGetPathFromIDList(pidl, path))
				{
					DestinationItemPath = path.ToString();
				}
				Marshal.FreeCoTaskMem(pidl);
			}

			return Task.CompletedTask;
		}

		private async Task CreateShortcutAsync()
		{
			string? destinationName;
			var extension = DestinationPathExists ? ".lnk" : ".url";
			var arguments = string.Empty;

			if (DestinationPathExists)
			{
				destinationName = Path.GetFileName(_fullPath);
				if (_isLocationInPath)
					arguments = DestinationItemPath.Split(' ')[1..].Aggregate(arguments, (current, arg) => current + arg + " ");

				if(string.IsNullOrEmpty(destinationName))
				{
					
					var destinationPath = DestinationItemPath.Replace('/', '\\');

					if (destinationPath.EndsWith('\\'))
						destinationPath = destinationPath.Substring(0, destinationPath.Length - 1);

					destinationName = destinationPath.Substring(destinationPath.LastIndexOf('\\') + 1);
				}
			}
			else
			{
				var uri = new Uri(DestinationItemPath);
				destinationName = uri.Host;
			}

			var shortcutName = FilesystemHelpers.GetShortcutNamingPreference(destinationName);
			ShortcutCompleteName = shortcutName + extension;
			var filePath = Path.Combine(WorkingDirectory, ShortcutCompleteName);

			int fileNumber = 1;
			while (Path.Exists(filePath))
			{
				ShortcutCompleteName = shortcutName + $" ({++fileNumber})" + extension;
				filePath = Path.Combine(WorkingDirectory, ShortcutCompleteName);
			}

			ShortcutCreatedSuccessfully = await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, _fullPath, arguments);
		}
	}
}