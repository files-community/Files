using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.DataModels;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Properties;
using Files.Backend.Enums;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Properties
{
	public sealed partial class MainPropertiesPage : Page
	{
		public Window Window;

		public AppWindow AppWindow;

		private readonly SettingsViewModel AppSettings = Ioc.Default.GetRequiredService<SettingsViewModel>();

		private ObservableCollection<NavigationViewItemButtonStyleItem> NavigationViewItems { get; set; }

		private CancellationTokenSource _tokenSource = new();

		private object _parameter;

		private IShellPage _appInstance;

		private bool SelectionChangedAutomatically { get; set; }

		public MainPropertiesPage()
		{
			InitializeComponent();

			if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
				FlowDirection = FlowDirection.RightToLeft;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var args = (PropertiesPageArguments)e.Parameter;
			_appInstance = args.AppInstance;
			_parameter = args.Parameter;

			NavigationViewItems = PropertiesNavigationViewItemFactory.Initialize(args.Parameter);

			MainPropertiesWindowNavigationView.SelectedItem =
				NavigationViewItems.Where(x => x.ItemType == PropertiesNavigationViewItemType.General).First();

			base.OnNavigatedTo(e);
		}

		private async void Page_Loaded(object sender, RoutedEventArgs e)
		{
			AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;

			// Set rectangle for the Titlebar
			TitlebarArea.SizeChanged += (_, _) => DragZoneHelper.SetDragZones(Window, (int)TitlebarArea.ActualHeight);
			DragZoneHelper.SetDragZones(Window, (int)TitlebarArea.ActualHeight, 40);
			Window.Closed += Window_Closed;

			await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
		}

		private void MainPropertiesPage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			MainPropertiesWindowNavigationView.PaneDisplayMode =
				ActualWidth <= 600 ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;

			if (ActualWidth <= 600)
				foreach (var item in NavigationViewItems) item.IsCompact = true;
			else
				foreach (var item in NavigationViewItems) item.IsCompact = false;

			DragZoneHelper.SetDragZones(App.Window, (int)TitlebarArea.ActualHeight, 40);
		}

		private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
		{
			await DispatcherQueue.EnqueueAsync(() =>
			{
				((Frame)Parent).RequestedTheme = ThemeHelper.RootTheme;

				switch (ThemeHelper.RootTheme)
				{
					case ElementTheme.Default:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
						AppWindow.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
						break;
					case ElementTheme.Light:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0, 0, 0);
						AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
						break;
					case ElementTheme.Dark:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
						AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
						break;
				}
			});
		}

		private void MainPropertiesWindowNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (SelectionChangedAutomatically)
			{
				SelectionChangedAutomatically = false;
				return;
			}

			var parameter = new PropertiesPageArguments()
			{
				AppInstance = _appInstance,
				CancellationTokenSource = _tokenSource,
				Parameter = _parameter,
				Window = Window,
			};

			var page = (PropertiesNavigationViewItemType)args.SelectedItemContainer.Tag switch
			{
				PropertiesNavigationViewItemType.General =>       typeof(GeneralPage),
				PropertiesNavigationViewItemType.Shortcut =>      typeof(ShortcutPage),
				PropertiesNavigationViewItemType.Library =>       typeof(LibraryPage),
				PropertiesNavigationViewItemType.Details =>       typeof(DetailsPage),
				PropertiesNavigationViewItemType.Security =>      typeof(SecurityPage),
				PropertiesNavigationViewItemType.Customization => typeof(CustomizationPage),
				PropertiesNavigationViewItemType.Compatibility => typeof(CompatibilityPage),
				PropertiesNavigationViewItemType.Hashes =>        typeof(HashesPage),
				_ => typeof(GeneralPage),
			};

			MainContentFrame.Navigate(page, parameter, args.RecommendedNavigationTransitionInfo);
		}

		private void BackwardNavigationButton_Click(object sender, RoutedEventArgs e)
		{
			if (MainContentFrame.CanGoBack)
				MainContentFrame.GoBack();

			var pageTag = ((Page)MainContentFrame.Content).Tag.ToString();

			SelectionChangedAutomatically = true;

			// Move selection indicator
			MainPropertiesWindowNavigationView.SelectedItem =
				NavigationViewItems
				.FirstOrDefault(x => string.Compare(x.ItemType.ToString(), pageTag, true) == 0)
				?? NavigationViewItems.FirstOrDefault();
		}

		private void CancelChangesButton_Click(object sender, RoutedEventArgs e)
		{
			Window.Close();
		}

		private async void SaveChangesButton_Click(object sender, RoutedEventArgs e)
		{
			await ApplyChanges();
			Window.Close();
		}

		private async Task ApplyChanges()
		{
			if (MainContentFrame.Content is not null)
			{
				if (MainContentFrame.Content is GeneralPage propertiesGeneral)
					await propertiesGeneral.SaveChangesAsync();
				else
					await ((BasePropertiesPage)MainContentFrame.Content).SaveChangesAsync();
			}
		}

		private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
				Window.Close();
		}

		private void Window_Closed(object sender, WindowEventArgs args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			Window.Closed -= Window_Closed;

			if (_tokenSource is not null && !_tokenSource.IsCancellationRequested)
				_tokenSource.Cancel();
		}
	}
}
