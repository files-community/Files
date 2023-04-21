using CommunityToolkit.WinUI;
using Files.App.Shell;
using System.IO;
using Windows.Storage.Pickers;
using Microsoft.UI.Windowing;

namespace Files.App.ViewModels.Properties
{
	/// <summary>
	/// Represents ViewModel for Files.App.CustomizationPage
	/// </summary>
	public class CustomizationViewModel : ObservableObject
	{
		private readonly AppWindow AppWindow;

		private static string DefaultIconDllFilePath
			=> Path.Combine(CommonPaths.SystemRootPath, "System32", "SHELL32.dll");

		private string _SelectedItemPath;
		public string SelectedItemPath
		{
			get => _SelectedItemPath;
			set => SetProperty(ref _SelectedItemPath, value);
		}

		private string _IconResourceItemPath;
		public string IconResourceItemPath
		{
			get => _IconResourceItemPath;
			set => SetProperty(ref _IconResourceItemPath, value);
		}

		private IShellPage _AppInstance;
		private IShellPage AppInstance
		{
			get => _AppInstance;
			set => SetProperty(ref _AppInstance, value);
		}

		private bool _IsShortcut;
		public bool IsShortcut
		{
			get => _IsShortcut;
			private set => SetProperty(ref _IsShortcut, value);
		}

		public ObservableCollection<IconFileInfo> DllIcons { get; }

		private IconFileInfo? _SelectedDllIcon;
		public IconFileInfo? SelectedDllIcon
		{
			get => _SelectedDllIcon;
			set
			{
				if (SetProperty(ref _SelectedDllIcon, value))
					IsIconChanged = true;
			}
		}

		private bool IsIconChanged;

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

			AppInstance = appInstance;
			AppWindow = appWindow;
			IconResourceItemPath = DefaultIconDllFilePath;
			IsShortcut = item.IsShortcut;
			SelectedItemPath = item.ItemPath;

			DllIcons = new();

			// Get default
			LoadIconsForPath(IconResourceItemPath);

			RestoreDefaultIconCommand = new RelayCommand(ExecuteRestoreDefaultIcon);
			OpenFilePickerCommand = new AsyncRelayCommand(ExecuteOpenFilePickerAsync);
		}

		private void ExecuteRestoreDefaultIcon()
		{
			SelectedDllIcon = null;
			IsIconChanged = true;
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
			var parentWindowId = AppWindow.Id;
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
			if (!IsIconChanged)
				return false;

			bool result = false;

			if (SelectedDllIcon is null)
			{
				result = IsShortcut
					? Win32API.SetCustomFileIcon(SelectedItemPath, null)
					: Win32API.SetCustomDirectoryIcon(SelectedItemPath, null);
			}
			else
			{
				result = IsShortcut
					? Win32API.SetCustomFileIcon(SelectedItemPath, IconResourceItemPath, SelectedDllIcon.Index)
					: Win32API.SetCustomDirectoryIcon(SelectedItemPath, IconResourceItemPath, SelectedDllIcon.Index);
			}

			if (!result)
				return false;

			await App.Window.DispatcherQueue.EnqueueAsync(() =>
			{
				AppInstance?.FilesystemViewModel?.RefreshItems(null);
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
