using Files.Shared.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Windows.Input;

namespace Files.App.ViewModels.Properties
{
	public sealed partial class CustomizationViewModel : ObservableObject
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private static string DefaultIconDllFilePath
			=> Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "SHELL32.dll");

		private readonly AppWindow? _appWindow;

		private readonly IShellPage? _appInstance;

		private readonly string? _selectedItemPath;

		private bool _isIconChanged;

		public readonly bool IsShortcut;

		public ObservableCollection<IconFileInfo> DllIcons { get; } = [];

		private string? _IconResourceItemPath;
		public string? IconResourceItemPath
		{
			get => _IconResourceItemPath;
			set
			{
				if (SetProperty(ref _IconResourceItemPath, value))
				{
					DllIcons.Clear();

					if (IsConvertibleImagePath(_IconResourceItemPath))
					{
						ConvertImageInfoBarSeverity = InfoBarSeverity.Informational;
						ConvertImageInfoBarMessage = Strings.ConvertToIconRequiredMessage.GetLocalizedResource();
						IsConvertImageInfoBarOpen = true;
						return;
					}

					IsConvertImageInfoBarOpen = false;

					if (Path.Exists(_IconResourceItemPath))
					{
						var icons = Win32Helper.ExtractIconsFromDLL(_IconResourceItemPath);
						if (icons?.Count is null or 0)
							return;

						foreach (var item in icons)
							DllIcons.Add(item);
					}
				}
			}
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

		[ObservableProperty] public partial bool IsConvertImageInfoBarOpen { get; set; }
		[ObservableProperty] public partial InfoBarSeverity ConvertImageInfoBarSeverity { get; set; }
		[ObservableProperty] public partial string? ConvertImageInfoBarMessage { get; set; }

		public ICommand? RestoreDefaultIconCommand { get; private set; }
		public ICommand? OpenFilePickerCommand { get; private set; }
		public ICommand? ConvertImageToIconCommand { get; private set; }

		public CustomizationViewModel(IShellPage appInstance, BaseProperties baseProperties, AppWindow appWindow)
		{
			ListedItem? item;
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


			RestoreDefaultIconCommand = new RelayCommand(ExecuteRestoreDefaultIconCommand);
			OpenFilePickerCommand = new RelayCommand(ExecuteOpenFilePickerCommand);
			ConvertImageToIconCommand = new AsyncRelayCommand(ExecuteConvertImageToIconCommandAsync);
		}

		private static bool IsConvertibleImagePath(string? path)
		{
			return FileExtensionHelpers.IsConvertibleToIcoFile(path) && File.Exists(path);
		}

		private void ExecuteRestoreDefaultIconCommand()
		{
			SelectedDllIcon = null;
			_isIconChanged = true;
		}

		private void ExecuteOpenFilePickerCommand()
		{
			var parentWindowId = (_appWindow
				?? throw new InvalidOperationException("The customization window has not been initialized.")).Id;
			var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);

			string[] extensions =
			[
				Strings.AllSupportedFiles.GetLocalizedResource(), "*.dll;*.exe;*.ico;*.icl;*.png;*.bmp;*.jpg;*.jpeg;*.jfif",
				Strings.IconFiles.GetLocalizedResource(), "*.dll;*.exe;*.ico;*.icl",
				Strings.ApplicationExtension.GetLocalizedResource(), "*.dll",
				Strings.Application.GetLocalizedResource(), "*.exe",
				Strings.IcoFileCapitalized.GetLocalizedResource(), "*.ico",
				Strings.IclFileCapitalized.GetLocalizedResource(), "*.icl ",
				Strings.ImageFiles.GetLocalizedResource(), "*.png;*.bmp;*.jpg;*.jpeg;*.jfif",
			];

			var result = CommonDialogService.Open_FileOpenDialog(hWnd, false, extensions, Environment.SpecialFolder.MyComputer, out var filePath);
			if (result)
				IconResourceItemPath = filePath;
		}

		private async Task ExecuteConvertImageToIconCommandAsync()
		{
			var imagePath = IconResourceItemPath;
			if (imagePath is null || !IsConvertibleImagePath(imagePath))
				return;

			var appWindow = _appWindow
				?? throw new InvalidOperationException("The customization window has not been initialized.");
			var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(appWindow.Id);
			if (!CommonDialogService.Open_FileSaveDialog(hWnd, false, [Strings.IcoFileCapitalized.GetLocalizedResource(), "*.ico"], Environment.SpecialFolder.MyPictures, out var icoFilePath))
				return;

			// The save dialog doesn't enforce an extension, and the shell only accepts real .ico files here
			if (!Path.GetExtension(icoFilePath).Equals(".ico", StringComparison.OrdinalIgnoreCase))
				icoFilePath += ".ico";

			var converted = await Task.Run(() => Win32Helper.ConvertImageToIcoFile(imagePath, icoFilePath));
			if (converted)
			{
				IconResourceItemPath = icoFilePath;
				SelectedDllIcon = DllIcons.FirstOrDefault();
			}
			else
			{
				ConvertImageInfoBarSeverity = InfoBarSeverity.Error;
				ConvertImageInfoBarMessage = Strings.ConvertToIconError.GetLocalizedResource();
				IsConvertImageInfoBarOpen = true;
			}
		}

		public async Task<bool> UpdateIcon()
		{
			if (!_isIconChanged)
				return false;

			var selectedItemPath = _selectedItemPath
				?? throw new InvalidOperationException("The selected item path has not been initialized.");
			bool result = false;

			if (SelectedDllIcon is null)
			{
				result = IsShortcut
					? Win32Helper.SetCustomFileIcon(selectedItemPath, null)
					: Win32Helper.SetCustomDirectoryIcon(selectedItemPath, null);
			}
			else
			{
				result = IsShortcut
					? Win32Helper.SetCustomFileIcon(selectedItemPath, IconResourceItemPath, SelectedDllIcon.Index)
					: Win32Helper.SetCustomDirectoryIcon(selectedItemPath, IconResourceItemPath, SelectedDllIcon.Index);
			}

			if (!result)
				return false;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				_appInstance?.ShellViewModel?.RefreshItems(null);
			});

			return true;
		}
	}
}
