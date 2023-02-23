using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Helpers;
using Files.App.Shell;
using Files.App.ViewModels.Properties;
using Files.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.Views
{
	public sealed partial class PropertiesCustomization : PropertiesTab
	{
		private string? selectedItemPath;

		private string? iconResourceItemPath;

		private IShellPage? appInstance;

		public ICommand RestoreDefaultIconCommand { get; private set; }

		public bool IsShortcut { get; private set; }

		public PropertiesCustomization()
		{
			InitializeComponent();

			RestoreDefaultIconCommand = new AsyncRelayCommand(RestoreDefaultIcon);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			string initialPath = Path.Combine(CommonPaths.SystemRootPath, "System32", "SHELL32.dll");

			var item = (BaseProperties as FileProperties)?.Item ?? (BaseProperties as FolderProperties)?.Item;

			appInstance = AppInstance;
			iconResourceItemPath = initialPath;
			selectedItemPath = item.ItemPath;
			IsShortcut = item.IsShortcut;

			ItemDisplayedPath.Text = iconResourceItemPath;

			LoadIconsForPath(iconResourceItemPath);
		}

		private async void PickDllButton_Click(object sender, RoutedEventArgs e)
		{
			// Initialize picker
			Windows.Storage.Pickers.FileOpenPicker picker = new()
			{
				SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder,
				ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
			};

			picker.FileTypeFilter.Add(".dll");
			picker.FileTypeFilter.Add(".exe");
			picker.FileTypeFilter.Add(".ico");

			// WINUI3
			var parentWindowId = ((Properties)((Frame)XamlRoot.Content).Content).appWindow.Id;
			var handle = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);

			var file = await picker.PickSingleFileAsync();
			if (file is null)
				return;

			iconResourceItemPath = file.Path;
			ItemDisplayedPath.Text = iconResourceItemPath;

			LoadIconsForPath(file.Path);
		}

		private void LoadIconsForPath(string path)
		{
			var icons = Win32API.ExtractIconsFromDLL(path);
			IconSelectionGrid.ItemsSource = icons;
		}

		private async void IconSelectionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			IconFileInfo? selectedIconInfo = ((GridView)sender).SelectedItem as IconFileInfo;
			if (selectedIconInfo is null)
				return;

			var setIconResult = IsShortcut
				? SetCustomFileIcon(selectedItemPath, iconResourceItemPath, selectedIconInfo.Index)
				: SetCustomFolderIcon(selectedItemPath, iconResourceItemPath, selectedIconInfo.Index);

			if (setIconResult)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					appInstance?.FilesystemViewModel?.RefreshItems(null);
				});
			}
		}

		private async Task RestoreDefaultIcon()
		{
			RestoreDefaultButton.IsEnabled = false;

			var setIconResult = IsShortcut
				? SetCustomFileIcon(selectedItemPath, null)
				: SetCustomFolderIcon(selectedItemPath, null);

			if (setIconResult)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					appInstance?.FilesystemViewModel?.RefreshItems(null, async () =>
					{
						await DispatcherQueue.EnqueueAsync(() => RestoreDefaultButton.IsEnabled = true);
					});
				});
			}
		}

		private bool SetCustomFolderIcon(string? folderPath, string? iconFile, int iconIndex = 0)
			=> Win32API.SetCustomDirectoryIcon(folderPath, iconFile, iconIndex);

		private bool SetCustomFileIcon(string? filePath, string? iconFile, int iconIndex = 0)
			=> Win32API.SetCustomFileIcon(filePath, iconFile, iconIndex);

		public override Task<bool> SaveChangesAsync()
		{
			return Task.FromResult(true);
		}

		public override void Dispose()
		{
		}
	}
}
