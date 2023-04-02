using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Properties;
using Files.Backend.Enums;
using Files.Backend.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
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

		private ContentDialog _dialog;

		private object _parameter;

		private IShellPage _appInstance;

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

			if (FilePropertiesHelpers.IsWinUI3)
			{
				// Set rectangle for the Titlebar
				TitlebarArea.SizeChanged += (_, _) => DragZoneHelper.SetDragZones(Window, (int)TitlebarArea.ActualHeight);
				DragZoneHelper.SetDragZones(Window, (int)TitlebarArea.ActualHeight, 40);
				Window.Closed += Window_Closed;

				await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
			}
			else
			{
				_dialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
				_dialog.Closed += PropertiesDialog_Closed;
			}
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
				if (!FilePropertiesHelpers.IsWinUI3)
					return;

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
			Type page = typeof(GeneralPage);

			var parameter = new PropertiesPageArguments()
			{
				AppInstance = _appInstance,
				CancellationTokenSource = _tokenSource,
				Parameter = _parameter
			};

			switch ((PropertiesNavigationViewItemType)args.SelectedItemContainer.Tag)
			{
				case PropertiesNavigationViewItemType.General:
					page = typeof(GeneralPage);
					break;
				case PropertiesNavigationViewItemType.Shortcut:
					page = typeof(ShortcutPage);
					break;
				case PropertiesNavigationViewItemType.Library:
					page = typeof(LibraryPage);
					break;
				case PropertiesNavigationViewItemType.Details:
					page = typeof(DetailsPage);
					break;
				case PropertiesNavigationViewItemType.Security:
					page = typeof(SecurityPage);
					break;
				case PropertiesNavigationViewItemType.Customization:
					page = typeof(CustomizationPage);
					break;
				case PropertiesNavigationViewItemType.Compatibility:
					page = typeof(CompatibilityPage);
					break;
				case PropertiesNavigationViewItemType.Hashes:
					page = typeof(HashesPage);
					break;
			}

			contentFrame.Navigate(page, parameter, args.RecommendedNavigationTransitionInfo);
		}

		#region Save/Cancel/Close
		private void CancelChangesButton_Click(object sender, RoutedEventArgs e)
		{
			ClosePage();
		}

		private async void SaveChangesButton_Click(object sender, RoutedEventArgs e)
		{
			await ApplyChanges();
			ClosePage();
		}

		private async Task ApplyChanges()
		{
			if (contentFrame.Content is not null)
			{
				if (contentFrame.Content is GeneralPage propertiesGeneral)
					await propertiesGeneral.SaveChangesAsync();
				else
					await ((BasePropertiesPage)contentFrame.Content).SaveChangesAsync();
			}
		}

		private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
				ClosePage();
		}

		private void ClosePage()
		{
			if (FilePropertiesHelpers.IsWinUI3)
				Window.Close();
			else
				_dialog?.Hide();
		}

		private void Window_Closed(object sender, WindowEventArgs args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			Window.Closed -= Window_Closed;

			if (_tokenSource is not null && !_tokenSource.IsCancellationRequested)
			{
				_tokenSource.Cancel();
			}
		}

		private void PropertiesDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Closed -= PropertiesDialog_Closed;

			if (_tokenSource is not null && !_tokenSource.IsCancellationRequested)
			{
				_tokenSource.Cancel();
			}

			_dialog.Hide();
		}
		#endregion
	}
}
