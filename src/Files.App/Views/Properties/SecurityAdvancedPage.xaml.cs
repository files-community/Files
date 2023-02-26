using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Permissions;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityAdvancedPage : Page
	{
		private object navParameterItem;

		public string DialogTitle => string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), ViewModel.Item.Name);

		public SecurityViewModel ViewModel { get; set; }

		public AppWindow appWindow;

		public SecurityAdvancedPage()
		{
			InitializeComponent();

			var flowDirectionSetting = /*
                TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
                Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
                replace the new instance created below with correct instance.
                Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
            */new ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];

			if (flowDirectionSetting == "RTL")
			{
				FlowDirection = FlowDirection.RightToLeft;
			}
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var args = e.Parameter as PropertiesPageNavigationArguments;
			navParameterItem = args.Item;

			if (args.Item is ListedItem listedItem)
			{
				ViewModel = new SecurityViewModel(listedItem);
			}
			else if (args.Item is DriveItem driveitem)
			{
				ViewModel = new SecurityViewModel(driveitem);
			}

			base.OnNavigatedTo(e);
		}

		private async void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			App.AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				appWindow.Destroying += AppWindow_Destroying;
				await App.Window.DispatcherQueue.EnqueueAsync(() => App.AppSettings.UpdateThemeElements.Execute(null));
			}
			else
			{
			}

			ViewModel.GetFilePermissions();
		}

		private void AppWindow_Destroying(AppWindow sender, object args)
		{
			App.AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Destroying -= AppWindow_Destroying;
		}

		private void Properties_Unloaded(object sender, RoutedEventArgs e)
		{
			// Why is this not called? Are we cleaning up properly?
		}

		private async void AppSettings_ThemeModeChanged(object sender, EventArgs e)
		{
			var selectedTheme = ThemeHelper.RootTheme;
			await DispatcherQueue.EnqueueAsync(() =>
			{
				((Frame)Parent).RequestedTheme = selectedTheme;
				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				{
					switch (selectedTheme)
					{
						case ElementTheme.Default:
							appWindow.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
							appWindow.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
							break;

						case ElementTheme.Light:
							appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
							appWindow.TitleBar.ButtonForegroundColor = Colors.Black;
							break;

						case ElementTheme.Dark:
							appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
							appWindow.TitleBar.ButtonForegroundColor = Colors.White;
							break;
					}
				}
			});
		}

		private async void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				if (ViewModel.SetFilePermissions())
				{
					appWindow.Destroy();
				}
			}
			else
			{
			}
		}

		private async void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				appWindow.Destroy();
			}
			else
			{
			}
		}

		private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
			{
				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				{
					appWindow.Destroy();
				}
				else
				{
				}
			}
		}

		public class PropertiesPageNavigationArguments
		{
			public object Item { get; set; }
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.SelectedAccessRules = (sender as ListView).SelectedItems.Cast<AccessControlEntryAdvanced>().ToList();

			if (e.AddedItems is not null)
			{
				foreach (var item in e.AddedItems)
				{
					(item as AccessControlEntryAdvanced).IsSelected = true;
				}
			}
			if (e.RemovedItems is not null)
			{
				foreach (var item in e.RemovedItems)
				{
					(item as AccessControlEntryAdvanced).IsSelected = false;
				}
			}
		}
	}
}