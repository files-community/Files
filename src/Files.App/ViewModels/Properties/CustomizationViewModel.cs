﻿using CommunityToolkit.WinUI;
using Files.App.Utils.Shell;
using System.IO;
using Windows.Storage.Pickers;
using Microsoft.UI.Windowing;
using System.Windows.Input;

namespace Files.App.ViewModels.Properties
{
	public sealed class CustomizationViewModel : ObservableObject
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private static string DefaultIconDllFilePath
			=> Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "SHELL32.dll");

		private readonly AppWindow _appWindow;

		private readonly IShellPage _appInstance;

		private readonly string _selectedItemPath;

		private bool _isIconChanged;

		public readonly bool IsShortcut;

		public ObservableCollection<IconFileInfo> DllIcons { get; }

		private string _IconResourceItemPath;
		public string IconResourceItemPath
		{
			get => _IconResourceItemPath;
			set => SetProperty(ref _IconResourceItemPath, value);
		}

		private IconFileInfo? _SelectedDllIcon;
		public IconFileInfo? SelectedDllIcon
		{
			get => _SelectedDllIcon;
			set
			{
				if (SetProperty(ref _SelectedDllIcon, value))
					_isIconChanged = true;
			}
		}

		public ICommand RestoreDefaultIconCommand { get; private set; }
		public ICommand OpenFilePickerCommand { get; private set; }

		public CustomizationViewModel(IShellPage appInstance, BaseProperties baseProperties, AppWindow appWindow)
		{
			ListedItem item;

			if (baseProperties is FileProperties fileProperties)
				item = fileProperties.Item;
			else if (baseProperties is FolderProperties folderProperties)
				item = folderProperties.Item;
			else
				return;

			_appInstance = appInstance;
			_appWindow = appWindow;
			IconResourceItemPath = DefaultIconDllFilePath;
			IsShortcut = item.IsShortcut;
			_selectedItemPath = item.ItemPath;

			DllIcons = [];

			// Get default
			LoadIconsForPath(IconResourceItemPath);

			RestoreDefaultIconCommand = new RelayCommand(ExecuteRestoreDefaultIconCommand);
			OpenFilePickerCommand = new RelayCommand(ExecuteOpenFilePickerCommand);
		}

		private void ExecuteRestoreDefaultIconCommand()
		{
			SelectedDllIcon = null;
			_isIconChanged = true;
		}

		private void ExecuteOpenFilePickerCommand()
		{
			var parentWindowId = _appWindow.Id;
			var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);

			string[] extensions =
			[
				"ApplicationExtension".GetLocalizedResource(), "*.dll",
				"Application".GetLocalizedResource(), "*.exe",
				"IcoFileCapitalized".GetLocalizedResource(), "*.ico",
			];

			var result = CommonDialogService.Open_FileOpenDialog(hWnd, false, extensions, Environment.SpecialFolder.MyComputer, out var filePath);
			if (result)
				LoadIconsForPath(filePath);
		}

		public async Task<bool> UpdateIcon()
		{
			if (!_isIconChanged)
				return false;

			bool result = false;

			if (SelectedDllIcon is null)
			{
				result = IsShortcut
					? Win32Helper.SetCustomFileIcon(_selectedItemPath, null)
					: Win32Helper.SetCustomDirectoryIcon(_selectedItemPath, null);
			}
			else
			{
				result = IsShortcut
					? Win32Helper.SetCustomFileIcon(_selectedItemPath, IconResourceItemPath, SelectedDllIcon.Index)
					: Win32Helper.SetCustomDirectoryIcon(_selectedItemPath, IconResourceItemPath, SelectedDllIcon.Index);
			}

			if (!result)
				return false;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				_appInstance?.ShellViewModel?.RefreshItems(null);
			});

			return true;
		}

		private void LoadIconsForPath(string path)
		{
			IconResourceItemPath = path;
			DllIcons.Clear();

			var icons = Win32Helper.ExtractIconsFromDLL(path);
			if (icons?.Count is null or 0)
				return;

			foreach(var item in icons)
				DllIcons.Add(item);
		}
	}
}
