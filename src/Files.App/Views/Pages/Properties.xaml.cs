using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Properties;
using Files.Core.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

namespace Files.App.Views
{
	public sealed partial class Properties : Page
	{
		private CancellationTokenSource? tokenSource = new CancellationTokenSource();
		private ContentDialog propertiesDialog;

		private object navParameterItem;
		private IShellPage AppInstance;

		public SettingsViewModel AppSettings => App.AppSettings;

		public AppWindow appWindow;

		public Properties()
		{
			InitializeComponent();

			var flowDirectionSetting = /*
				TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
				Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
				replace the new instance created below with correct instance.
				Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
			*/new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];

			if (flowDirectionSetting == "RTL")
				FlowDirection = FlowDirection.RightToLeft;

			contentFrame.Navigated += ContentFrame_Navigated;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var args = e.Parameter as PropertiesPageNavigationArguments;
			AppInstance = args.AppInstanceArgument;
			navParameterItem = args.Item;
			if (args.Item is ListedItem listedItem)
			{
				var isShortcut = listedItem.IsShortcut;
				var isLibrary = listedItem.IsLibrary;
				var fileExt = listedItem.FileExtension;
				TabShorcut.Visibility = isShortcut ? Visibility.Visible : Visibility.Collapsed;
				TabLibrary.Visibility = isLibrary ? Visibility.Visible : Visibility.Collapsed;
				TabDetails.Visibility = fileExt is not null && !isShortcut && !isLibrary ? Visibility.Visible : Visibility.Collapsed;
				TabCustomization.Visibility = !isLibrary && (
					(listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !listedItem.IsArchive) ||
					(isShortcut && !listedItem.IsLinkItem)) ? Visibility.Visible : Visibility.Collapsed;
				TabCompatibility.Visibility = (
						FileExtensionHelpers.IsExecutableFile(listedItem is ShortcutItem sht ? sht.TargetPath : fileExt, true)
					) ? Visibility.Visible : Visibility.Collapsed;
				TabSecurity.Visibility = !isLibrary && !listedItem.IsRecycleBinItem ? Visibility.Visible : Visibility.Collapsed;
			}
			else if (args.Item is DriveItem)
			{
				TabSecurity.Visibility = Visibility.Visible;
			}
			base.OnNavigatedTo(e);
		}

		private async void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				TitlebarArea.SizeChanged += TitlebarArea_SizeChanged;
				appWindow.Destroying += AppWindow_Destroying;
				await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
			}
			else
			{
				propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
				propertiesDialog.Closed += PropertiesDialog_Closed;
			}
		}

		private void TitlebarArea_SizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			/*
			 We have to calculate the width of NavigationView as 'ActualWidth' is bigger than the real size occupied by the control.
			 This code calculates the sum of all the visible tabs' widths.
			 If a tab is visible and its width is 0, it is shown in the overflow menu. In this case we add the overflow's size to the total.
			 */
			int navigationViewWidth = (int)NavigationView.MenuItems.Cast<NavigationViewItem>()
				.Where(item => item.Visibility == Visibility.Visible)
				.GroupBy(item => item.ActualWidth != 0)
				.Select(group => group.Key ? group.Select(item => item.ActualWidth).Sum() : group.First().CompactPaneLength)
				.Sum();

			var scaleAdjustment = XamlRoot.RasterizationScale;
			int x = (int)(navigationViewWidth * scaleAdjustment);
			var y = 0;
			var width = (int)((TitlebarArea.ActualWidth - navigationViewWidth) * scaleAdjustment);
			var height = (int)(TitlebarArea.ActualHeight * scaleAdjustment);

			// Sets properties window drag region.
			appWindow.TitleBar.SetDragRectangles(new RectInt32[]
			{
				// This area is on the right of NavigationView and stretches for all the remaining space.
				new RectInt32(x, y, width, height)
			});
		}

		private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
		{
			if (contentFrame.Content is Page propertiesMenu)
			{
				propertiesMenu.Loaded -= PropertiesMenu_Loaded;
				propertiesMenu.Loaded += PropertiesMenu_Loaded;
			}
		}

		private void PropertiesMenu_Loaded(object sender, RoutedEventArgs e)
		{
			// Drag region is calculated each time the active tab is changed
			TitlebarArea_SizeChanged(null, null);
		}

		private void AppWindow_Destroying(AppWindow sender, object args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Destroying -= AppWindow_Destroying;
			if (tokenSource is not null && !tokenSource.IsCancellationRequested)
			{
				tokenSource.Cancel();
				tokenSource = null;
			}
		}

		private void PropertiesDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Closed -= PropertiesDialog_Closed;
			if (tokenSource is not null && !tokenSource.IsCancellationRequested)
			{
				tokenSource.Cancel();
				tokenSource = null;
			}
			propertiesDialog.Hide();
		}

		private void Properties_Unloaded(object sender, RoutedEventArgs e)
		{
			// Why is this not called? Are we cleaning up properly?
		}

		private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
		{
			var selectedTheme = ThemeHelper.RootTheme;

			await DispatcherQueue.EnqueueAsync(() =>
			{
				((Frame)Parent).RequestedTheme = selectedTheme;

				if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					return;

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
			});
		}

		private async void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (contentFrame.Content is PropertiesGeneral propertiesGeneral)
			{
				await propertiesGeneral.SaveChangesAsync();
			}
			else
			{
				if (!await (contentFrame.Content as PropertiesTab).SaveChangesAsync())
					return;
			}

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				appWindow.Destroy();
			else
				propertiesDialog?.Hide();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				appWindow.Destroy();
			else
				propertiesDialog?.Hide();
		}

		private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (!e.Key.Equals(VirtualKey.Escape))
				return;
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				appWindow.Destroy();
			else
				propertiesDialog?.Hide();
		}

		private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			var navParam = new PropertyNavParam()
			{
				tokenSource = tokenSource,
				navParameter = navParameterItem,
				AppInstanceArgument = AppInstance
			};

			switch (args.SelectedItemContainer.Tag)
			{
				case "General":
					contentFrame.Navigate(typeof(PropertiesGeneral), navParam, args.RecommendedNavigationTransitionInfo);
					break;

				case "Shortcut":
					contentFrame.Navigate(typeof(PropertiesShortcut), navParam, args.RecommendedNavigationTransitionInfo);
					break;

				case "Library":
					contentFrame.Navigate(typeof(PropertiesLibrary), navParam, args.RecommendedNavigationTransitionInfo);
					break;

				case "Details":
					contentFrame.Navigate(typeof(PropertiesDetails), navParam, args.RecommendedNavigationTransitionInfo);
					break;

				case "Security":
					contentFrame.Navigate(typeof(PropertiesSecurity), navParam, args.RecommendedNavigationTransitionInfo);
					break;

				case "Customization":
					contentFrame.Navigate(typeof(PropertiesCustomization), navParam, args.RecommendedNavigationTransitionInfo);
					break;

				case "Compatibility":
					contentFrame.Navigate(typeof(PropertiesCompatibility), navParam, args.RecommendedNavigationTransitionInfo);
					break;
			}
		}

		public class PropertiesPageNavigationArguments
		{
			public object Item { get; set; }
			public IShellPage AppInstanceArgument { get; set; }
		}

		public class PropertyNavParam
		{
			public CancellationTokenSource tokenSource;
			public object navParameter;
			public IShellPage AppInstanceArgument { get; set; }
		}
	}
}