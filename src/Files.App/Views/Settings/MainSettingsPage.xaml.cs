using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Settings;
using Files.App.Settings;
using Files.Backend.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Settings
{
	public sealed partial class MainSettingsPage : Page
	{
		private MainSettingsViewModel ViewModel { get; set; } = new();

		public AppWindow AppWindow { get; set; }

		private static SettingsViewModel AppSettings
			=> App.AppSettings;

		private readonly static bool UsingWinUI =
			ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8);

		public MainSettingsPage()
		{
			InitializeComponent();

			AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
		}

		private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
		{
			if (!UsingWinUI)
				return;

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
						AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x32, 0, 0, 0);
						AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
						break;
					case ElementTheme.Dark:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x32, 0xFE, 0xFE, 0xFE);
						AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
						break;
				}
			});
		}

		private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			_ = Enum.Parse<MainSettingsNavigationViewItem>(args.SelectedItemContainer.Tag.ToString()) switch
			{
				MainSettingsNavigationViewItem.Appearance  => SettingsContentFrame.Navigate(typeof(AppearancePage)),
				MainSettingsNavigationViewItem.Preferences => SettingsContentFrame.Navigate(typeof(PreferencesPage)),
				MainSettingsNavigationViewItem.Folders     => SettingsContentFrame.Navigate(typeof(FoldersPage)),
				MainSettingsNavigationViewItem.Tags        => SettingsContentFrame.Navigate(typeof(TagsPage)),
				MainSettingsNavigationViewItem.Advanced    => SettingsContentFrame.Navigate(typeof(AdvancedPage)),
				MainSettingsNavigationViewItem.About       => SettingsContentFrame.Navigate(typeof(AboutPage)),
				_ => SettingsContentFrame.Navigate(typeof(AppearancePage))
			};
		}

		private enum MainSettingsNavigationViewItem
		{
			Appearance,
			Preferences,
			Folders,
			Tags,
			Advanced,
			About
		}
	}
}
