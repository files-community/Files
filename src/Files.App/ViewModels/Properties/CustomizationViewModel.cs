using Files.App.Shell;
using System.IO;
using Windows.Storage.Pickers;
using Microsoft.UI.Windowing;

namespace Files.App.ViewModels.Properties
{
	public class CustomizationViewModel : ObservableObject
	{
		private static string DefaultIconDllFilePath
			=> Path.Combine(CommonPaths.SystemRootPath, "System32", "SHELL32.dll");

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

		public IRelayCommand RestoreDefaultIconCommand { get; private set; }
		public IAsyncRelayCommand OpenFilePickerCommand { get; private set; }

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

			DllIcons = new();

			// Get default
			LoadIconsForPath(IconResourceItemPath);

			RestoreDefaultIconCommand = new RelayCommand(ExecuteRestoreDefaultIcon);
			OpenFilePickerCommand = new AsyncRelayCommand(ExecuteOpenFilePickerAsync);
		}

		private void ExecuteRestoreDefaultIcon()
		{
			SelectedDllIcon = null;
			_isIconChanged = true;
		}

		private async Task ExecuteOpenFilePickerAsync()
		{
			// Initialize picker
			FileOpenPicker picker = new()
			{
				SuggestedStartLocation = PickerLocationId.ComputerFolder,
				ViewMode = PickerViewMode.Thumbnail,
			};

			picker.FileTypeFilter.Add(".dll");
			picker.FileTypeFilter.Add(".exe");
			picker.FileTypeFilter.Add(".ico");

			// WINUI3: Create and initialize new window
			var parentWindowId = _appWindow.Id;
			var handle = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);

			// Open picker
			var file = await picker.PickSingleFileAsync();
			if (file is null)
				return;

			LoadIconsForPath(file.Path);
		}

		public async Task<bool> UpdateIcon()
		{
			if (!_isIconChanged)
				return false;

			bool result = false;

			if (SelectedDllIcon is null)
			{
				result = IsShortcut
					? Win32API.SetCustomFileIcon(_selectedItemPath, null)
					: Win32API.SetCustomDirectoryIcon(_selectedItemPath, null);
			}
			else
			{
				result = IsShortcut
					? Win32API.SetCustomFileIcon(_selectedItemPath, IconResourceItemPath, SelectedDllIcon.Index)
					: Win32API.SetCustomDirectoryIcon(_selectedItemPath, IconResourceItemPath, SelectedDllIcon.Index);
			}

			if (!result)
				return false;

			await App.Window.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				_appInstance?.FilesystemViewModel?.RefreshItems(null);
			});

			return true;
		}

		private void LoadIconsForPath(string path)
		{
			IconResourceItemPath = path;
			DllIcons.Clear();

			var icons = Win32API.ExtractIconsFromDLL(path);
			if (icons?.Count is null or 0)
				return;

			foreach(var item in icons)
				DllIcons.Add(item);
		}
	}
}
