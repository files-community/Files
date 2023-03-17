using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Shell;
using Files.Shared;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.ViewModels.Properties
{
	public class CustomizationViewModel : ObservableObject
	{
		public CustomizationViewModel(string selectedItemPath, string iconResourceItemPath, IShellPage appInstance, bool isShortcut)
		{
			SelectedItemPath = selectedItemPath;
			IconResourceItemPath = iconResourceItemPath;
			AppInstance = appInstance;
			IsShortcut = isShortcut;

			_dllIcons = new();
			DllIcons = new(_dllIcons);

			RestoreButtonIsEnabled = true;

			// Get default
			LoadIconsForPath(IconResourceItemPath);

			RestoreDefaultIconCommand = new AsyncRelayCommand(RestoreDefaultIcon);
		}

		private string? _selectedItemPath;
		public string? SelectedItemPath { get => _selectedItemPath; set => SetProperty(ref _selectedItemPath, value); }

		private string? _iconResourceItemPath;
		public string? IconResourceItemPath { get => _iconResourceItemPath; set => SetProperty(ref _iconResourceItemPath, value); }

		private IShellPage? _appInstance;
		private IShellPage? AppInstance { get => _appInstance; set => SetProperty(ref _appInstance, value); }

		private bool _isShortcut;
		public bool IsShortcut { get => _isShortcut; private set => SetProperty(ref _isShortcut, value); }

		private bool _restoreButtonIsEnabled;
		public bool RestoreButtonIsEnabled { get => _restoreButtonIsEnabled; private set => SetProperty(ref _restoreButtonIsEnabled, value); }

		private readonly ObservableCollection<IconFileInfo> _dllIcons;
		public ReadOnlyObservableCollection<IconFileInfo> DllIcons { get; }

		public ICommand RestoreDefaultIconCommand { get; private set; }

		private async Task RestoreDefaultIcon()
		{
			RestoreButtonIsEnabled = false;

			var setIconResult = IsShortcut
				? SetCustomFileIcon(SelectedItemPath, null)
				: SetCustomFolderIcon(SelectedItemPath, null);

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

		public async Task ChangeIcon(IconFileInfo selectedIconInfo)
		{
			var setIconResult = IsShortcut
				? SetCustomFileIcon(SelectedItemPath, IconResourceItemPath, selectedIconInfo.Index)
				: SetCustomFolderIcon(SelectedItemPath, IconResourceItemPath, selectedIconInfo.Index);

			if (setIconResult)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					AppInstance?.FilesystemViewModel?.RefreshItems(null);
				});
			}
		}

		public void LoadIconsForPath(string path)
		{
			IconResourceItemPath = path;
			_dllIcons.Clear();

			var icons = Win32API.ExtractIconsFromDLL(path);
			if (icons?.Count is null or 0)
				return;

			foreach(var item in icons)
				_dllIcons.Add(item);
		}

		private bool SetCustomFolderIcon(string? folderPath, string? iconFile, int iconIndex = 0)
			=> Win32API.SetCustomDirectoryIcon(folderPath, iconFile, iconIndex);

		private bool SetCustomFileIcon(string? filePath, string? iconFile, int iconIndex = 0)
			=> Win32API.SetCustomFileIcon(filePath, iconFile, iconIndex);
	}
}
