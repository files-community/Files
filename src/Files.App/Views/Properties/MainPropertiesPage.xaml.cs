using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Properties
{
	public sealed partial class MainPropertiesPage : Page
	{
		private CancellationTokenSource? _tokenSource;

		private ContentDialog _propertiesDialog;

		private object _navParamItem;

		private IShellPage _appInstance;

		private bool _usingWinUI;

		public SettingsViewModel AppSettings
			=> App.AppSettings;

		public Window Window;
		public AppWindow AppWindow;

		public ObservableCollection<SquareNavViewItem> NavViewItems { get; set; }

		public MainPropertiesPage()
		{
			InitializeComponent();

			_tokenSource = new();

			NavViewItems = new();

			_usingWinUI = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8);

			// TODO:
			//  ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
			//  Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
			//  replace the new instance created below with correct instance.
			//  Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
			var flowDirectionSetting = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];
			if (flowDirectionSetting == "RTL")
				FlowDirection = FlowDirection.RightToLeft;
		}

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

			if (_usingWinUI)
			{
				// WINUI3: Set rectangle for the Titlebar
				TitlebarArea.SizeChanged += TitlebarArea_SizeChanged;
				AppWindow.Destroying += AppWindow_Destroying;

				await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
			}
			else
			{
				_propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
				_propertiesDialog.Closed += PropertiesDialog_Closed;
			}
		}

		private void TitlebarArea_SizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			var scaleAdjustment = XamlRoot.RasterizationScale;
			var width = (int)(TitlebarArea.ActualWidth * scaleAdjustment);
			var height = (int)(TitlebarArea.ActualHeight * scaleAdjustment);

			// Sets properties window drag region.
			AppWindow.TitleBar.SetDragRectangles(new RectInt32[] { new RectInt32(0, 0, width, height) });
		}

		private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
		{
			await DispatcherQueue.EnqueueAsync(() =>
			{
				((Frame)Parent).RequestedTheme = ThemeHelper.RootTheme;

				if (!_usingWinUI)
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
			var navParam = new PropertyNavParam()
			{
				tokenSource = _tokenSource,
				navParameter = _navParamItem,
				AppInstanceArgument = _appInstance
			};

			switch ((PropertyNavigationViewItemEnums)args.SelectedItemContainer.Tag)
			{
				case PropertyNavigationViewItemEnums.ItemGeneral:
					contentFrame.Navigate(typeof(GeneralPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.ItemShortcut:
					contentFrame.Navigate(typeof(ShortcutPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.ItemLibrary:
					contentFrame.Navigate(typeof(LibraryPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.ItemDetails:
					contentFrame.Navigate(typeof(DetailsPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.ItemSecurity:
					contentFrame.Navigate(typeof(SecurityPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.ItemCustomization:
					contentFrame.Navigate(typeof(CustomizationPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
				case PropertyNavigationViewItemEnums.ItemCompatibility:
					contentFrame.Navigate(typeof(CompatibilityPage), navParam, args.RecommendedNavigationTransitionInfo);
					break;
			}
		}

		private void AddNavigationViewItemsToControl(object item)
		{
			var generalItem = new SquareNavViewItem()
			{
				Name = "General".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemGeneral,
				OutlinePathIcon = (string)Application.Current.Resources["PageIconRegular"],
				FilledPathIcon = (string)Application.Current.Resources["PageIconFilled"],
			};
			var securityItem = new SquareNavViewItem()
			{
				Name = "Security".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemSecurity,
				OutlinePathIcon = (string)Application.Current.Resources["ShieldIconRegular"],
				FilledPathIcon = (string)Application.Current.Resources["ShieldIconFilled"],
			};
			var shortcutItem = new SquareNavViewItem()
			{
				Name = "Shortcut".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemShortcut,
				OutlinePathIcon = (string)Application.Current.Resources["LinkIconRegular"],
				FilledPathIcon = (string)Application.Current.Resources["LinkIconFilled"],
			};
			var libraryItem = new SquareNavViewItem()
			{
				Name = "Library".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemLibrary,
				OutlinePathIcon = (string)Application.Current.Resources["LibraryIconRegular"],
				FilledPathIcon = (string)Application.Current.Resources["LibraryIconFilled"],
			};
			var detailsItem = new SquareNavViewItem()
			{
				Name = "Details".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemDetails,
				OutlinePathIcon = (string)Application.Current.Resources["InfoIconRegular"],
				FilledPathIcon = (string)Application.Current.Resources["InfoIconFilled"],
			};
			var customizationItem = new SquareNavViewItem()
			{
				Name = "Customization".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemCustomization,
				OutlinePathIcon = (string)Application.Current.Resources["ColorIconRegular"],
				FilledPathIcon = (string)Application.Current.Resources["ColorIconFilled"],
			};
			var compatibilityItem = new SquareNavViewItem()
			{
				Name = "Compatibility".GetLocalizedResource(),
				ItemType = PropertyNavigationViewItemEnums.ItemCompatibility,
				OutlinePathIcon = (string)Application.Current.Resources["WindowsAppsRegular"],
				FilledPathIcon = (string)Application.Current.Resources["WindowsAppsFilled"],
			};

			NavViewItems.Add(generalItem);
			NavViewItems.Add(securityItem);
			NavViewItems.Add(shortcutItem);
			NavViewItems.Add(libraryItem);
			NavViewItems.Add(detailsItem);
			NavViewItems.Add(customizationItem);
			NavViewItems.Add(compatibilityItem);

			MainPropertiesWindowNavigationView.SelectedItem = generalItem;

			// Unable unavailable property tabs
			if (item is ListedItem listedItem)
			{
				var isShortcut = listedItem.IsShortcut;
				var isLibrary = listedItem.IsLibrary;
				var fileExt = listedItem.FileExtension;

				var securityItemEnabled = !isLibrary && !listedItem.IsRecycleBinItem;
				var detailsItemEnabled = fileExt is not null && !isShortcut && !isLibrary;
				var customizationItemEnabled = !isLibrary && (
					(listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !listedItem.IsArchive) ||
					(isShortcut && !listedItem.IsLinkItem));
				var compatibilityItemEnabled = FileExtensionHelpers.IsExecutableFile(listedItem is ShortcutItem sht ? sht.TargetPath : fileExt, true);

				if (!securityItemEnabled)
					NavViewItems.Remove(securityItem);

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
				NavViewItems.Remove(shortcutItem);
				NavViewItems.Remove(libraryItem);
				NavViewItems.Remove(detailsItem);
				NavViewItems.Remove(customizationItem);
				NavViewItems.Remove(compatibilityItem);
			}
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
			if (contentFrame.Content is not null)
			{
				if (contentFrame.Content is GeneralPage propertiesGeneral)
				{
					await propertiesGeneral.SaveChangesAsync();
				}
				else
				{
					await ((BasePropertiesPage)contentFrame.Content).SaveChangesAsync();
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
			if (_usingWinUI)
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

	public class SquareNavViewItem : ObservableObject
	{
		public string Name;

		public PropertyNavigationViewItemEnums ItemType;

		public string? OutlinePathIcon;

		public string? FilledPathIcon;

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set => SetProperty(ref _isSelected, value);
		}
	}

	public enum PropertyNavigationViewItemEnums
	{
		ItemGeneral = 1,
		ItemShortcut,
		ItemLibrary,
		ItemDetails,
		ItemSecurity,
		ItemCustomization,
		ItemCompatibility,
	}
}
