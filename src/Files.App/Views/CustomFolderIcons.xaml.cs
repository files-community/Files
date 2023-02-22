using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Shell;
using Files.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using static Files.App.Views.PropertiesCustomization;

namespace Files.App.Views
{
	public sealed partial class CustomFolderIcons : Page
	{
		private string? selectedItemPath;
		private string? iconResourceItemPath;
		private IShellPage? appInstance;

		public ICommand RestoreDefaultIconCommand { get; private set; }
		public bool IsShortcut { get; private set; }

		public CustomFolderIcons()
		{
			InitializeComponent();
			RestoreDefaultIconCommand = new AsyncRelayCommand(RestoreDefaultIcon);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is not IconSelectorInfo selectorInfo)
				return;

			selectedItemPath = selectorInfo.SelectedItem;
			IsShortcut = selectorInfo.IsShortcut;
			iconResourceItemPath = selectorInfo.InitialPath;
			appInstance = selectorInfo.AppInstance;
			ItemDisplayedPath.Text = iconResourceItemPath;

			LoadIconsForPath(iconResourceItemPath);
		}

		private async void PickDllButton_Click(object sender, RoutedEventArgs e)
		{
			Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
			var parentWindowId = ((Properties)((Frame)XamlRoot.Content).Content).appWindow.Id;
			var handle = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			picker.FileTypeFilter.Add(".dll");
			picker.FileTypeFilter.Add(".exe");
			picker.FileTypeFilter.Add(".ico");

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
			var selectedIconInfo = ((GridView)sender).SelectedItem as IconFileInfo;
			if (selectedIconInfo is null)
				return;

			var setIconResult = IsShortcut ?
				SetCustomFileIcon(selectedItemPath, iconResourceItemPath, selectedIconInfo.Index) :
				SetCustomFolderIcon(selectedItemPath, iconResourceItemPath, selectedIconInfo.Index);
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

			var setIconResult = IsShortcut ?
				SetCustomFileIcon(selectedItemPath, null) :
				SetCustomFolderIcon(selectedItemPath, null);
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
		{
			return Win32API.SetCustomDirectoryIcon(folderPath, iconFile, iconIndex);
		}

		private bool SetCustomFileIcon(string? filePath, string? iconFile, int iconIndex = 0)
			=> Win32API.SetCustomFileIcon(filePath, iconFile, iconIndex);
	}
}
