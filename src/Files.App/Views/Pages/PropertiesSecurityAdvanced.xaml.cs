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
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.App.Views
{
	/// <summary>
	/// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
	/// </summary>
	public sealed partial class PropertiesSecurityAdvanced : Page
	{
		private object navParameterItem;

		public string DialogTitle => string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), ViewModel.Item.Name);

		public SecurityProperties ViewModel { get; set; }

		public AppWindow appWindow;

		public PropertiesSecurityAdvanced()
		{
			this.InitializeComponent();

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
				ViewModel = new SecurityProperties(listedItem);
			}
			else if (args.Item is DriveItem driveitem)
			{
				ViewModel = new SecurityProperties(driveitem);
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

		private void Page_Loading(FrameworkElement sender, object args)
		{
			// This manually adds the user's theme resources to the page
			// I was unable to get this to work any other way
			try
			{
				var xaml = XamlReader.Load(App.ExternalResourcesHelper.CurrentThemeResources) as ResourceDictionary;
				App.Current.Resources.MergedDictionaries.Add(xaml);
			}
			catch (Exception)
			{
			}
		}

		public class PropertiesPageNavigationArguments
		{
			public object Item { get; set; }
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.SelectedAccessRules = (sender as ListView).SelectedItems.Cast<FileSystemAccessRuleForUI>().ToList();

			if (e.AddedItems is not null)
			{
				foreach (var item in e.AddedItems)
				{
					(item as FileSystemAccessRuleForUI).IsSelected = true;
				}
			}
			if (e.RemovedItems is not null)
			{
				foreach (var item in e.RemovedItems)
				{
					(item as FileSystemAccessRuleForUI).IsSelected = false;
				}
			}
		}
	}
}