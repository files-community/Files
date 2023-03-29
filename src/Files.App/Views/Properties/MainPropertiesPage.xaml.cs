using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Properties;
using Files.Backend.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Properties
{
	public sealed partial class MainPropertiesPage : Page
	{
		public readonly SettingsViewModel AppSettings;

		public Window Window;

		public AppWindow AppWindow;

		public ObservableCollection<NavigationViewItemButtonStyleItem> NavViewItems { get; set; }

		public MainPropertiesPage()
		{
			InitializeComponent();

			AppSettings = Ioc.Default.GetRequiredService<SettingsViewModel>();
			_tokenSource = new();

			NavViewItems = new();

			// TODO:
			//  ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
			//  Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
			//  replace the new instance created below with correct instance.
			//  Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
			var flowDirectionSetting = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];
			if (flowDirectionSetting == "RTL")
				FlowDirection = FlowDirection.RightToLeft;
		}

		private CancellationTokenSource? _tokenSource;

		private ContentDialog _propertiesDialog;

		private object _navParamItem;

		private IShellPage _appInstance;

		private static bool IsWinUI3
			=> FilePropertiesHelpers.IsWinUI3;

		private bool SelectionChangedAutomatically { get; set; }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var args = e.Parameter as PropertiesPageNavigationArguments;
			_appInstance = args.AppInstanceArgument;
			_navParamItem = args.Item;

			AddNavigationViewItemsToControl(args.Item);

			base.OnNavigatedTo(e);
		}

		private async void Page_Loaded(object sender, RoutedEventArgs e)
		{
			AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;

			if (IsWinUI3)
			{
				DragZoneHelper.SetDragZones(Window, (int)TitlebarAreaGrid.ActualHeight, (int)TitlebarAreaTitle.ActualOffset.X);
				AppWindow.Destroying += AppWindow_Destroying;

				await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
			}
			else
			{
				_propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
				_propertiesDialog.Closed += PropertiesDialog_Closed;
			}
		}

		private void MainPropertiesPage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// Update NavigationView pane mode
			UpdateDialogLayout();

			// Update drag region
			DragZoneHelper.SetDragZones(Window, (int)TitlebarAreaGrid.ActualHeight, (int)TitlebarAreaTitle.ActualOffset.X);
		}

		private void UpdateDialogLayout()
		{
			MainPropertiesWindowNavigationView.IsPaneOpen =
				ActualWidth <= 600 ? false : true;

			if (ActualWidth <= 600)
				foreach (var item in NavViewItems) item.IsCompact = true;
			else
				foreach (var item in NavViewItems) item.IsCompact = false;
		}

		private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
		{
			await DispatcherQueue.EnqueueAsync(() =>
			{
				((Frame)Parent).RequestedTheme = ThemeHelper.RootTheme;

				if (!IsWinUI3)
					return;

				switch (ThemeHelper.RootTheme)
				{
					case ElementTheme.Default:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
						AppWindow.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
						break;

					case ElementTheme.Light:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
						AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
						break;

					case ElementTheme.Dark:
						AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
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

			var navParam = new PropertyNavParam()
			{
				tokenSource = _tokenSource,
				navParameter = _navParamItem,
				AppInstanceArgument = _appInstance
			};

			switch ((PropertyNavigationViewItemEnums)args.SelectedItemContainer.Tag)
			{
				case PropertyNavigationViewItemEnums.General:
					MainContentFrame.Navigate(typeof(GeneralPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Shortcut:
					MainContentFrame.Navigate(typeof(ShortcutPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Library:
					MainContentFrame.Navigate(typeof(LibraryPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Details:
					MainContentFrame.Navigate(typeof(DetailsPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Security:
					MainContentFrame.Navigate(typeof(SecurityPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Customization:
					MainContentFrame.Navigate(typeof(CustomizationPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Compatibility:
					MainContentFrame.Navigate(typeof(CompatibilityPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.Hashes:
					MainContentFrame.Navigate(typeof(HashesPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
			}
		}

		private void AddNavigationViewItemsToControl(object item)
		{
			var generalItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "General".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.General,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconGeneralProperties"],
			};
			var securityItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Security".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Security,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconSecurityProperties"],
			};
			var hashItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Hashes".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Hashes,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconHashesProperties"],
			};
			var shortcutItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Shortcut".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Shortcut,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconShortcutProperties"],
			};
			var libraryItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Library".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Library,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconLibraryProperties"],
			};
			var detailsItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Details".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Details,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconDetailsProperties"],
			};
			var customizationItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Customization".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Customization,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconCustomizationProperties"],
			};
			var compatibilityItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Compatibility".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.Compatibility,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconCompatibilityProperties"],
			};

			NavViewItems.Add(generalItem);
			NavViewItems.Add(securityItem);
			NavViewItems.Add(hashItem);
			NavViewItems.Add(shortcutItem);
			NavViewItems.Add(libraryItem);
			NavViewItems.Add(detailsItem);
			NavViewItems.Add(customizationItem);
			NavViewItems.Add(compatibilityItem);

			MainPropertiesWindowNavigationView.SelectedItem = generalItem;

			if (item is List<ListedItem> listedItems)
			{
				var commonFileExt = listedItems.Select(x => x.FileExtension).Distinct().Count() == 1 ? listedItems.First().FileExtension : null;
				var compatibilityItemEnabled = listedItems.All(listedItem => FileExtensionHelpers.IsExecutableFile(listedItem is ShortcutItem sht ? sht.TargetPath : commonFileExt, true));

				if (!compatibilityItemEnabled)
					NavViewItems.Remove(compatibilityItem);

				NavViewItems.Remove(libraryItem);
				NavViewItems.Remove(shortcutItem);
				NavViewItems.Remove(detailsItem);
				NavViewItems.Remove(securityItem);
				NavViewItems.Remove(customizationItem);
				NavViewItems.Remove(hashItem);
			}
			else if (item is ListedItem listedItem)
			{
				var isShortcut = listedItem.IsShortcut;
				var isLibrary = listedItem.IsLibrary;
				var fileExt = listedItem.FileExtension;
				var isFolder = listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder;

				var securityItemEnabled = !isLibrary && !listedItem.IsRecycleBinItem;
				var hashItemEnabled = !(isFolder && !listedItem.IsArchive) && !isLibrary && !listedItem.IsRecycleBinItem;
				var detailsItemEnabled = fileExt is not null && !isShortcut && !isLibrary;
				var customizationItemEnabled = !isLibrary && ((isFolder && !listedItem.IsArchive) || (isShortcut && !listedItem.IsLinkItem));
				var compatibilityItemEnabled = FileExtensionHelpers.IsExecutableFile(listedItem is ShortcutItem sht ? sht.TargetPath : fileExt, true);

				if (!securityItemEnabled)
					NavViewItems.Remove(securityItem);

				if (!hashItemEnabled)
					NavViewItems.Remove(hashItem);

				if (!isShortcut)
					NavViewItems.Remove(shortcutItem);

				if (!isLibrary)
					NavViewItems.Remove(libraryItem);

				if (!detailsItemEnabled)
					NavViewItems.Remove(detailsItem);

				if (!customizationItemEnabled)
					NavViewItems.Remove(customizationItem);

				if (!compatibilityItemEnabled)
					NavViewItems.Remove(compatibilityItem);
			}
			else if (item is DriveItem)
			{
				NavViewItems.Remove(hashItem);
				NavViewItems.Remove(shortcutItem);
				NavViewItems.Remove(libraryItem);
				NavViewItems.Remove(detailsItem);
				NavViewItems.Remove(customizationItem);
				NavViewItems.Remove(compatibilityItem);
			}
		}

		private void MainPropertiesWindowNavigationView_PaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
		{
			foreach (var item in NavViewItems)
				item.IsCompact = true;
		}

		private void MainPropertiesWindowNavigationView_PaneOpening(NavigationView sender, object args)
		{
			foreach (var item in NavViewItems)
				item.IsCompact = false;
		}

		private void TitlebarAreaBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (MainContentFrame.CanGoBack)
				MainContentFrame.GoBack();

			var pageTag = ((Page)MainContentFrame.Content).Tag.ToString();

			var defaultItem =
				MainPropertiesWindowNavigationView
				.MenuItemsSource
				.OfType<NavigationViewItemButtonStyleItem>()
				.FirstOrDefault();

			MainPropertiesWindowNavigationView.SelectedItem =
				MainPropertiesWindowNavigationView
				.MenuItemsSource
				.OfType<NavigationViewItemButtonStyleItem>()
				.FirstOrDefault(x => string.Compare(x.Tag.ToString(), pageTag, true) == 0)
				?? defaultItem;

			SelectionChangedAutomatically = true;
		}

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
			if (MainContentFrame.Content is not null)
			{
				if (MainContentFrame.Content is GeneralPage propertiesGeneral)
				{
					await propertiesGeneral.SaveChangesAsync();
				}
				else
				{
					await ((BasePropertiesPage)MainContentFrame.Content).SaveChangesAsync();
				}
			}
		}

		private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
				ClosePage();
		}

		private void ClosePage()
		{
			if (IsWinUI3)
				// AppWindow.Destroy() doesn't seem to work well. (#11461)
				Window.Close();
			else
				_propertiesDialog?.Hide();
		}

		private void AppWindow_Destroying(AppWindow sender, object args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Destroying -= AppWindow_Destroying;

			if (_tokenSource is not null && !_tokenSource.IsCancellationRequested)
			{
				_tokenSource.Cancel();
				_tokenSource = null;
			}
		}

		private void PropertiesDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
		{
			AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Closed -= PropertiesDialog_Closed;

			if (_tokenSource is not null && !_tokenSource.IsCancellationRequested)
			{
				_tokenSource.Cancel();
				_tokenSource = null;
			}

			_propertiesDialog.Hide();
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

	// TODO: Should move to a general place to use in the Settings Dialog as well
	public class NavigationViewItemButtonStyleItem : ObservableObject
	{
		public string? _Name;
		public string? Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		private PropertyNavigationViewItemEnums _ItemType;
		public PropertyNavigationViewItemEnums ItemType
		{
			get => _ItemType;
			set => SetProperty(ref _ItemType, value);
		}

		private Style _OpacityIconStyle = (Style)Application.Current.Resources["ColorIconGeneralProperties"];
		public Style OpacityIconStyle
		{
			get => _OpacityIconStyle;
			set => SetProperty(ref _OpacityIconStyle, value);
		}

		private bool _IsSelected;
		public bool IsSelected
		{
			get => _IsSelected;
			set => SetProperty(ref _IsSelected, value);
		}

		private bool _IsCompact;
		public bool IsCompact
		{
			get => _IsCompact;
			set => SetProperty(ref _IsCompact, value);
		}
	}

	public enum PropertyNavigationViewItemEnums
	{
		General = 1,
		Shortcut,
		Library,
		Details,
		Security,
		Customization,
		Compatibility,
		Hashes,
	}
}
