using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using System;
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

		private ListedItem listedItem;

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
			listedItem = args.Item as ListedItem;
			TabShorcut.Visibility = listedItem is not null && listedItem.IsShortcut ? Visibility.Visible : Visibility.Collapsed;
			TabLibrary.Visibility = listedItem is not null && listedItem.IsLibrary ? Visibility.Visible : Visibility.Collapsed;
			TabDetails.Visibility = listedItem is not null && listedItem.FileExtension is not null && !listedItem.IsShortcut && !listedItem.IsLibrary ? Visibility.Visible : Visibility.Collapsed;
			TabSecurity.Visibility = args.Item is DriveItem ||
				(listedItem is not null && !listedItem.IsLibrary && !listedItem.IsRecycleBinItem) ? Visibility.Visible : Visibility.Collapsed;
			TabCustomization.Visibility = listedItem is not null && !listedItem.IsLibrary && (
				(listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !listedItem.IsArchive) ||
				(listedItem.IsShortcut && !listedItem.IsLinkItem)) ? Visibility.Visible : Visibility.Collapsed;
			TabCompatibility.Visibility = listedItem is not null && (
					".exe".Equals(listedItem is ShortcutItem sht ? System.IO.Path.GetExtension(sht.TargetPath) : listedItem.FileExtension, StringComparison.OrdinalIgnoreCase)
				) ? Visibility.Visible : Visibility.Collapsed;
			base.OnNavigatedTo(e);
		}

		private async void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				NavigationView.SizeChanged += NavigationView_SizeChanged;
				appWindow.Destroying += AppWindow_Destroying;
				await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
			}
			else
			{
				propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
				propertiesDialog.Closed += PropertiesDialog_Closed;
			}
		}

		private void NavigationView_SizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			/*
			 We have to calculate the width of NavigationView as 'ActualWidth' is bigger than the real size occupied by the control.
			 This code calculates the sum of all the visible tabs' widths.
			 If a tab is visible and its width is 0, it is shown in the overflow menu. In this case we add the overflow's size to the total.
			 */
			int navigationViewWidth = 0;
			bool overflowAdded = false;
			foreach (NavigationViewItem item in NavigationView.MenuItems)
			{
				if (item.Visibility == Visibility.Visible)
				{
					if (item.ActualWidth != 0)
					{
						navigationViewWidth += (int)item.ActualWidth;
					}
					else if (!overflowAdded)
					{
						navigationViewWidth += (int)item.CompactPaneLength;
						overflowAdded = true;
					}
				}
			}

			// Sets properties window drag region.
			appWindow.TitleBar.SetDragRectangles(new RectInt32[]
			{
				// This area is over the top margin of NavigationView.
				new RectInt32(0, 0, navigationViewWidth, (int)NavigationView.ActualOffset.Y),
				// This area is on the right of NavigationView and stretches for all the remaining space.
				new RectInt32(navigationViewWidth, 0, (int)(TitleBarDragArea.ActualSize.X - navigationViewWidth), (int)TitleBarDragArea.ActualSize.Y)
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
			NavigationView_SizeChanged(null, null);
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
				await propertiesGeneral.SaveChangesAsync(listedItem);
			}
			else
			{
				if (!await (contentFrame.Content as PropertiesTab).SaveChangesAsync(listedItem))
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
	}
}