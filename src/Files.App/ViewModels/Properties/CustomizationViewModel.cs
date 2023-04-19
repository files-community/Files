using CommunityToolkit.WinUI;
using Files.App.Shell;
using Files.App.Views.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Files.App.ViewModels.Properties
{
	public class CustomizationViewModel : ObservableObject
	{
		public CustomizationViewModel(IShellPage appInstance, BaseProperties baseProperties)
		{
			Filesystem.ListedItem item;

			if (baseProperties is FileProperties fileProperties)
				item = fileProperties.Item;
			else if (baseProperties is FolderProperties folderProperties)
				item = folderProperties.Item;
			else
				return;

			AppInstance = appInstance;
			IconResourceItemPath = Path.Combine(CommonPaths.SystemRootPath, "System32", "SHELL32.dll");
			IsShortcut = item.IsShortcut;
			SelectedItemPath = item.ItemPath;

			_dllIcons = new();
			DllIcons = new(_dllIcons);

			// Get default
			LoadIconsForPath(IconResourceItemPath);

			RestoreDefaultIconCommand = new AsyncRelayCommand(ExecuteRestoreDefaultIconAsync);
			PickDllFileCommand = new AsyncRelayCommand<XamlRoot>(ExecuteOpenFilePickerAsync);
		}

		private string? _selectedItemPath;
		public string? SelectedItemPath
		{
			get => _selectedItemPath;
			set => SetProperty(ref _selectedItemPath, value);
		}

		private string? _iconResourceItemPath;
		public string? IconResourceItemPath
		{
			get => _iconResourceItemPath;
			set => SetProperty(ref _iconResourceItemPath, value);
		}

		private IShellPage? _appInstance;
		private IShellPage? AppInstance
		{
			get => _appInstance;
			set => SetProperty(ref _appInstance, value);
		}

		private bool _isShortcut;
		public bool IsShortcut
		{
			get => _isShortcut;
			private set => SetProperty(ref _isShortcut, value);
		}

		private bool _restoreButtonIsEnabled = true;
		public bool RestoreButtonIsEnabled
		{
			get => _restoreButtonIsEnabled;
			private set => SetProperty(ref _restoreButtonIsEnabled, value);
		}

		private readonly ObservableCollection<IconFileInfo> _dllIcons;
		public ReadOnlyObservableCollection<IconFileInfo> DllIcons { get; }

		private IconFileInfo _SelectedDllIcon;
		public IconFileInfo SelectedDllIcon
		{
			get => _SelectedDllIcon;
			set
			{
				if (value is not null && SetProperty(ref _SelectedDllIcon, value))
				{
					ChangeIcon(value);
				}
			}
		}

		public IAsyncRelayCommand RestoreDefaultIconCommand { get; private set; }
		public IAsyncRelayCommand<XamlRoot> PickDllFileCommand { get; private set; }

		private async Task ExecuteRestoreDefaultIconAsync()
		{
			RestoreButtonIsEnabled = false;

			var setIconResult = IsShortcut
				? Win32API.SetCustomFileIcon(SelectedItemPath, null)
				: Win32API.SetCustomDirectoryIcon(SelectedItemPath, null);

			if (setIconResult)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					AppInstance?.FilesystemViewModel?.RefreshItems(null, async () =>
					{
						await App.Window.DispatcherQueue.EnqueueAsync(() => RestoreButtonIsEnabled = true);
						
					});
				});
			}
		}

		private async Task ExecuteOpenFilePickerAsync(XamlRoot? xamlRoot)
		{
			if (xamlRoot is null)
				return;

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
			var parentWindowId = ((MainPropertiesPage)((Frame)xamlRoot.Content).Content).AppWindow.Id;
			var handle = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);

			// Open picker
			var file = await picker.PickSingleFileAsync();
			if (file is null)
				return;

			LoadIconsForPath(file.Path);
		}

		private async Task ChangeIcon(IconFileInfo selectedIconInfo)
		{
			var setIconResult = IsShortcut
				? Win32API.SetCustomFileIcon(SelectedItemPath, IconResourceItemPath, selectedIconInfo.Index)
				: Win32API.SetCustomDirectoryIcon(SelectedItemPath, IconResourceItemPath, selectedIconInfo.Index);

			if (setIconResult)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					AppInstance?.FilesystemViewModel?.RefreshItems(null);
				});
			}
		}

		private void LoadIconsForPath(string path)
		{
			IconResourceItemPath = path;
			_dllIcons.Clear();

			var icons = Win32API.ExtractIconsFromDLL(path);
			if (icons?.Count is null or 0)
				return;

			foreach(var item in icons)
				_dllIcons.Add(item);
		}
	}
}
