using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Files.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Views
{
	public sealed partial class PropertiesCustomization : PropertiesTab
	{
		private CustomizationViewModel CustomizationViewModel { get; set; }

		public PropertiesCustomization()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			string initialPath = Path.Combine(CommonPaths.SystemRootPath, "System32", "SHELL32.dll");
			var item = (BaseProperties as FileProperties)?.Item ?? (BaseProperties as FolderProperties)?.Item;

			CustomizationViewModel = new(item.ItemPath, initialPath, AppInstance, item.IsShortcut);
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

			// WINUI3: Create and initialize new window
			var parentWindowId = ((Properties)((Frame)XamlRoot.Content).Content).appWindow.Id;
			var handle = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);

			// Open picker
			var file = await picker.PickSingleFileAsync();
			if (file is null)
				return;

			// TODO: View-ViewModel
			CustomizationViewModel.LoadIconsForPath(file.Path);
		}

		private async void IconSelectionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((GridView)sender).SelectedItem is not IconFileInfo selectedIconInfo)
				return;

			// TODO: View-ViewModel
			await CustomizationViewModel.ChangeIcon(selectedIconInfo);
		}

		public override Task<bool> SaveChangesAsync()
		{
			return Task.FromResult(true);
		}

		public override void Dispose()
		{
		}
	}
}
