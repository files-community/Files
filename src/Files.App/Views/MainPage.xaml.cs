using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.UserControls;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.Backend.Extensions;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UWPToWinAppSDKUpgradeHelpers;
using Windows.ApplicationModel;
using Windows.Services.Store;
using Windows.Storage;
using Windows.System;

namespace Files.App.Views
{
	/// <summary>
	/// The root page of Files
	/// </summary>
	public sealed partial class MainPage : Page, INotifyPropertyChanged
	{
		private VirtualKeyModifiers currentModifiers = VirtualKeyModifiers.None;

		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		public IWindowContext WindowContext { get; } = Ioc.Default.GetRequiredService<IWindowContext>();

		public AppModel AppModel => App.AppModel;

		public MainPageViewModel ViewModel
		{
			get => (MainPageViewModel)DataContext;
			set => DataContext = value;
		}

		/// <summary>
		/// True if the user is currently resizing the preview pane
		/// </summary>
		private bool draggingPreviewPane;

		private bool keyReleased = true;

		public SidebarViewModel SidebarAdaptiveViewModel = new SidebarViewModel();

		public readonly OngoingTasksViewModel OngoingTasksViewModel;

		public MainPage()
		{
			InitializeComponent();
			DataContext = Ioc.Default.GetRequiredService<MainPageViewModel>();
			OngoingTasksViewModel = Ioc.Default.GetRequiredService<OngoingTasksViewModel>();
			var flowDirectionSetting = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];
			if (flowDirectionSetting == "RTL")
				FlowDirection = FlowDirection.RightToLeft;

			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
		}

		private async Task PromptForReview()
		{
			var promptForReviewDialog = new ContentDialog
			{
				Title = "ReviewFiles".ToLocalized(),
				Content = "ReviewFilesContent".ToLocalized(),
				PrimaryButtonText = "Yes".ToLocalized(),
				SecondaryButtonText = "No".ToLocalized()
			};

			var result = await SetContentDialogRoot(promptForReviewDialog).ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				try
				{
					var storeContext = StoreContext.GetDefault();
					await storeContext.RequestRateAndReviewAppAsync();

					UserSettingsService.ApplicationSettingsService.ClickedToReviewApp = true;
				}
				catch (Exception) { }
			}
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			return contentDialog;
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(IPreviewPaneSettingsService.IsEnabled):
					LoadPaneChanged();
					break;
			}
		}

		private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
		{
			TabControl.DragArea.SizeChanged += (_, _) => SetRectDragRegion();

			if (ViewModel.MultitaskingControl is not HorizontalMultitaskingControl)
			{
				ViewModel.MultitaskingControl = TabControl;
				ViewModel.MultitaskingControls.Add(TabControl);
				ViewModel.MultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
			}
		}

		private void SetRectDragRegion()
		{
			DragZoneHelper.SetDragZones(App.Window,
				dragZoneLeftIndent: (int)(TabControl.ActualWidth + TabControl.Margin.Left - TabControl.DragArea.ActualWidth));
		}

		public void TabItemContent_ContentChanged(object? sender, TabItemArguments e)
		{
			if (SidebarAdaptiveViewModel.PaneHolder is null)
				return;

			var paneArgs = e.NavigationArg as PaneNavigationArguments;
			SidebarAdaptiveViewModel.UpdateSidebarSelectedItemFromArgs(SidebarAdaptiveViewModel.PaneHolder.IsLeftPaneActive ?
				paneArgs.LeftPaneNavPathParam : paneArgs.RightPaneNavPathParam);
			UpdateStatusBarProperties();
			LoadPaneChanged();
			UpdateNavToolbarProperties();
			ViewModel.UpdateInstanceProperties(paneArgs);
		}

		public void MultitaskingControl_CurrentInstanceChanged(object? sender, CurrentInstanceChangedEventArgs e)
		{
			if (SidebarAdaptiveViewModel.PaneHolder is not null)
				SidebarAdaptiveViewModel.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;

			var navArgs = e.CurrentInstance.TabItemArguments?.NavigationArg;
			SidebarAdaptiveViewModel.PaneHolder = e.CurrentInstance as IPaneHolder;
			SidebarAdaptiveViewModel.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
			SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged((navArgs as PaneNavigationArguments).LeftPaneNavPathParam);
			UpdateStatusBarProperties();
			UpdateNavToolbarProperties();
			LoadPaneChanged();
			ViewModel.UpdateInstanceProperties(navArgs);
			e.CurrentInstance.ContentChanged -= TabItemContent_ContentChanged;
			e.CurrentInstance.ContentChanged += TabItemContent_ContentChanged;
		}

		private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged(SidebarAdaptiveViewModel.PaneHolder.ActivePane?.TabItemArguments?.NavigationArg?.ToString());
			UpdateStatusBarProperties();
			UpdateNavToolbarProperties();
			LoadPaneChanged();
		}

		private void UpdateStatusBarProperties()
		{
			if (StatusBarControl is not null)
			{
				StatusBarControl.DirectoryPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.DirectoryPropertiesViewModel;
				StatusBarControl.SelectedItemsPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.SelectedItemsPropertiesViewModel;
			}
		}

		private void UpdateNavToolbarProperties()
		{
			if (NavToolbar is not null)
				NavToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.ToolbarViewModel;

			if (InnerNavigationToolbar is not null)
			{
				InnerNavigationToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.ToolbarViewModel;
				InnerNavigationToolbar.ShowMultiPaneControls = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneEnabled ?? false;
				InnerNavigationToolbar.IsMultiPaneActive = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
			}
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			ViewModel.OnNavigatedTo(e);
			SidebarControl.SidebarItemInvoked += SidebarControl_SidebarItemInvoked;
			SidebarControl.SidebarItemPropertiesInvoked += SidebarControl_SidebarItemPropertiesInvoked;
			SidebarControl.SidebarItemDropped += SidebarControl_SidebarItemDropped;
			SidebarControl.SidebarItemNewPaneInvoked += SidebarControl_SidebarItemNewPaneInvoked;
		}

		protected override async void OnPreviewKeyDown(KeyRoutedEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			switch (e.Key)
			{
				case VirtualKey.Menu:
				case VirtualKey.Control:
				case VirtualKey.Shift:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					break;
				default:
					var currentModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
					HotKey hotKey = new(e.Key, currentModifiers);

					// A textbox takes precedence over certain hotkeys.
					bool isTextBox = e.OriginalSource is DependencyObject source && source.FindAscendantOrSelf<TextBox>() is not null;
					if (isTextBox)
					{
						if (hotKey.IsTextBoxHotKey())
						{
							break;
						}
						if (currentModifiers is VirtualKeyModifiers.None && !e.Key.IsGlobalKey())
						{
							break;
						}
					}

					// Execute command for hotkey
					var command = Commands[hotKey];
					if (command.Code is not CommandCodes.None && keyReleased)
					{
						keyReleased = false;
						e.Handled = command.IsExecutable;
						await command.ExecuteAsync();
					}
					break;
			}
		}
		protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			switch (e.Key)
			{
				case VirtualKey.Menu:
				case VirtualKey.Control:
				case VirtualKey.Shift:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					break;
				default:
					keyReleased = true;
					break;
			}
		}

		private async void SidebarControl_SidebarItemDropped(object sender, SidebarItemDroppedEventArgs e)
		{
			await SidebarAdaptiveViewModel.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.ItemPath, false, true);
			e.SignalEvent?.Set();
		}

		private async void SidebarControl_SidebarItemPropertiesInvoked(object sender, SidebarItemPropertiesInvokedEventArgs e)
		{
			if (e.InvokedItemDataContext is DriveItem)
			{
				await FilePropertiesHelpers.OpenPropertiesWindowAsync(e.InvokedItemDataContext, SidebarAdaptiveViewModel.PaneHolder.ActivePane);
			}
			else if (e.InvokedItemDataContext is LibraryLocationItem library)
			{
				await FilePropertiesHelpers.OpenPropertiesWindowAsync(new LibraryItem(library), SidebarAdaptiveViewModel.PaneHolder.ActivePane);
			}
			else if (e.InvokedItemDataContext is LocationItem locationItem)
			{
				ListedItem listedItem = new ListedItem(null!)
				{
					ItemPath = locationItem.Path,
					ItemNameRaw = locationItem.Text,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};
				await FilePropertiesHelpers.OpenPropertiesWindowAsync(listedItem, SidebarAdaptiveViewModel.PaneHolder.ActivePane);
			}
		}

		private void SidebarControl_SidebarItemNewPaneInvoked(object sender, SidebarItemNewPaneInvokedEventArgs e)
		{
			if (e.InvokedItemDataContext is INavigationControlItem navItem)
				SidebarAdaptiveViewModel.PaneHolder.OpenPathInNewPane(navItem.Path);
		}

		private void SidebarControl_SidebarItemInvoked(object sender, SidebarItemInvokedEventArgs e)
		{
			var invokedItemContainer = e.InvokedItemContainer;

			string? navigationPath; // path to navigate
			Type? sourcePageType = null; // type of page to navigate

			switch ((invokedItemContainer.DataContext as INavigationControlItem)?.ItemType)
			{
				case NavigationControlItemType.Location:
					{
						var ItemPath = (invokedItemContainer.DataContext as INavigationControlItem)?.Path; // Get the path of the invoked item

						if (string.IsNullOrEmpty(ItemPath)) // Section item
						{
							navigationPath = invokedItemContainer.Tag?.ToString();
						}
						else if (ItemPath.Equals("Home", StringComparison.OrdinalIgnoreCase)) // Home item
						{
							if (ItemPath.Equals(SidebarAdaptiveViewModel.SidebarSelectedItem?.Path, StringComparison.OrdinalIgnoreCase))
								return; // return if already selected

							navigationPath = "Home";
							sourcePageType = typeof(WidgetsPage);
						}
						else // Any other item
						{
							navigationPath = invokedItemContainer.Tag?.ToString();
						}

						break;
					}

				case NavigationControlItemType.FileTag:
					var tagPath = (invokedItemContainer.DataContext as INavigationControlItem)?.Path; // Get the path of the invoked item
					if (SidebarAdaptiveViewModel.PaneHolder?.ActivePane is IShellPage shp)
					{
						shp.NavigateToPath(tagPath, new NavigationArguments()
						{
							IsSearchResultPage = true,
							SearchPathParam = "Home",
							SearchQuery = tagPath,
							AssociatedTabInstance = shp,
							NavPathParam = tagPath
						});
					}
					return;

				default:
					{
						navigationPath = invokedItemContainer.Tag?.ToString();
						break;
					}
			}

			if (SidebarAdaptiveViewModel.PaneHolder?.ActivePane is IShellPage shellPage)
				shellPage.NavigateToPath(navigationPath, sourcePageType);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			// Defers the status bar loading until after the page has loaded to improve startup perf
			FindName(nameof(StatusBarControl));
			FindName(nameof(InnerNavigationToolbar));
			FindName(nameof(TabControl));
			FindName(nameof(NavToolbar));

			if (Package.Current.Id.Name != "49306atecsolution.FilesUWP" || UserSettingsService.ApplicationSettingsService.ClickedToReviewApp)
				return;

			var totalLaunchCount = SystemInformation.Instance.TotalLaunchCount;
			if (totalLaunchCount is 10 or 20 or 30 or 40 or 50)
			{
				// Prompt user to review app in the Store
				DispatcherQueue.TryEnqueue(async () => await PromptForReview());
			}
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			switch (PreviewPane?.Position)
			{
				case PreviewPanePositions.Right when ContentColumn.ActualWidth == ContentColumn.MinWidth:
					UserSettingsService.PreviewPaneSettingsService.VerticalSizePx += e.NewSize.Width - e.PreviousSize.Width;
					UpdatePositioning();
					break;
				case PreviewPanePositions.Bottom when ContentRow.ActualHeight == ContentRow.MinHeight:
					UserSettingsService.PreviewPaneSettingsService.HorizontalSizePx += e.NewSize.Height - e.PreviousSize.Height;
					UpdatePositioning();
					break;
			}
		}

		private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
		{
			SidebarAdaptiveViewModel.UpdateTabControlMargin(); // Set the correct tab margin on startup
		}

		private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) => LoadPaneChanged();

		/// <summary>
		/// Call this function to update the positioning of the preview pane.
		/// This is a workaround as the VisualStateManager causes problems.
		/// </summary>
		private void UpdatePositioning()
		{
			if (PreviewPane is null || !ShouldPreviewPaneBeActive)
			{
				PaneRow.MinHeight = 0;
				PaneRow.MaxHeight = double.MaxValue;
				PaneRow.Height = new GridLength(0);
				PaneColumn.MinWidth = 0;
				PaneColumn.MaxWidth = double.MaxValue;
				PaneColumn.Width = new GridLength(0);
			}
			else
			{
				PreviewPane.UpdatePosition(RootGrid.ActualWidth, RootGrid.ActualHeight);
				switch (PreviewPane.Position)
				{
					case PreviewPanePositions.None:
						PaneRow.MinHeight = 0;
						PaneRow.Height = new GridLength(0);
						PaneColumn.MinWidth = 0;
						PaneColumn.Width = new GridLength(0);
						break;
					case PreviewPanePositions.Right:
						PreviewPane.SetValue(Grid.RowProperty, 1);
						PreviewPane.SetValue(Grid.ColumnProperty, 2);
						PaneSplitter.SetValue(Grid.RowProperty, 1);
						PaneSplitter.SetValue(Grid.ColumnProperty, 1);
						PaneSplitter.Width = 2;
						PaneSplitter.Height = RootGrid.ActualHeight;
						PaneSplitter.GripperCursor = GridSplitter.GripperCursorType.SizeWestEast;
						PaneColumn.MinWidth = PreviewPane.MinWidth;
						PaneColumn.MaxWidth = PreviewPane.MaxWidth;
						PaneColumn.Width = new GridLength(UserSettingsService.PreviewPaneSettingsService.VerticalSizePx, GridUnitType.Pixel);
						PaneRow.MinHeight = 0;
						PaneRow.MaxHeight = double.MaxValue;
						PaneRow.Height = new GridLength(0);
						break;
					case PreviewPanePositions.Bottom:
						PreviewPane.SetValue(Grid.RowProperty, 3);
						PreviewPane.SetValue(Grid.ColumnProperty, 0);
						PaneSplitter.SetValue(Grid.RowProperty, 2);
						PaneSplitter.SetValue(Grid.ColumnProperty, 0);
						PaneSplitter.Height = 2;
						PaneSplitter.Width = RootGrid.ActualWidth;
						PaneSplitter.GripperCursor = GridSplitter.GripperCursorType.SizeNorthSouth;
						PaneColumn.MinWidth = 0;
						PaneColumn.MaxWidth = double.MaxValue;
						PaneColumn.Width = new GridLength(0);
						PaneRow.MinHeight = PreviewPane.MinHeight;
						PaneRow.MaxHeight = PreviewPane.MaxHeight;
						PaneRow.Height = new GridLength(UserSettingsService.PreviewPaneSettingsService.HorizontalSizePx, GridUnitType.Pixel);
						break;
				}
			}
		}

		private void PaneSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			draggingPreviewPane = true;
		}

		private void PaneSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			switch (PreviewPane?.Position)
			{
				case PreviewPanePositions.Right:
					UserSettingsService.PreviewPaneSettingsService.VerticalSizePx = PreviewPane.ActualWidth;
					break;
				case PreviewPanePositions.Bottom:
					UserSettingsService.PreviewPaneSettingsService.HorizontalSizePx = PreviewPane.ActualHeight;
					break;
			}

			draggingPreviewPane = false;
		}

		private void PaneSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (draggingPreviewPane)
				return;

			var paneSplitter = (GridSplitter)sender;
			paneSplitter.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void PaneSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var paneSplitter = (GridSplitter)sender;
			paneSplitter.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		public bool ShouldPreviewPaneBeActive => UserSettingsService.PreviewPaneSettingsService.IsEnabled && ShouldPreviewPaneBeDisplayed;

		public bool ShouldPreviewPaneBeDisplayed
		{
			get
			{
				var isHomePage = !(SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false);
				var isMultiPane = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
				var isBigEnough = App.Window.Bounds.Width > 450 && App.Window.Bounds.Height > 400;
				var isEnabled = (!isHomePage || isMultiPane) && isBigEnough;

				return isEnabled;
			}
		}

		private void LoadPaneChanged()
		{
			OnPropertyChanged(nameof(ShouldPreviewPaneBeActive));
			OnPropertyChanged(nameof(ShouldPreviewPaneBeDisplayed));
			UpdatePositioning();
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void RootGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			switch (e.Key)
			{
				case VirtualKey.Menu:
				case VirtualKey.Control:
				case VirtualKey.Shift:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					break;
				default:
					var currentModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
					HotKey hotKey = new(e.Key, currentModifiers);

					// Prevents the arrow key events from navigating the list instead of switching compact overlay
					if (Commands[hotKey].Code is CommandCodes.EnterCompactOverlay or CommandCodes.ExitCompactOverlay)
						Focus(FocusState.Keyboard);
					break;
			}
		}

		private void NavToolbar_Loaded(object sender, RoutedEventArgs e) => UpdateNavToolbarProperties();
	}
}