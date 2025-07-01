// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.AI.Actions.Hosting;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls
{
	public sealed partial class NavigationToolbar : UserControl
	{
		// Dependency injections

		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly MainPageViewModel MainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly StatusCenterViewModel OngoingTasksViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		[GeneratedDependencyProperty]
		public partial bool IsSidebarPaneOpenToggleButtonVisible { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowOngoingTasks { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowSettingsButton { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowSearchBox { get; set; }

		[GeneratedDependencyProperty]
		public partial NavigationToolbarViewModel ViewModel { get; set; }

		// Constructor

		public NavigationToolbar()
		{
			InitializeComponent();
		}

		// Methods

		private void NavToolbar_Loading(FrameworkElement _, object e)
		{
			Loading -= NavToolbar_Loading;
			if (OngoingTasksViewModel is not null)
				OngoingTasksViewModel.NewItemAdded += OngoingTasksActions_ProgressBannerPosted;
		}

		private void VisiblePath_Loaded(object _, RoutedEventArgs e)
		{
			// AutoSuggestBox won't receive focus unless it's fully loaded
			VisiblePath.Focus(FocusState.Programmatic);

			if (DependencyObjectHelpers.FindChild<TextBox>(VisiblePath) is TextBox textBox)
			{
				if (textBox.Text.StartsWith(">"))
					textBox.Select(1, textBox.Text.Length - 1);
				else
					textBox.SelectAll();
			}
		}

		private void ManualPathEntryItem_Click(object _, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
			{
				var ptrPt = e.GetCurrentPoint(NavToolbar);
				if (ptrPt.Properties.IsMiddleButtonPressed)
					return;
			}
			ViewModel.IsEditModeEnabled = true;
		}

		private async void VisiblePath_KeyDown(object _, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape)
				ViewModel.IsEditModeEnabled = false;

			if (e.Key is VirtualKey.Tab)
			{
				ViewModel.IsEditModeEnabled = false;
				// Delay to ensure clickable path is ready to be focused
				await Task.Delay(10);
				ClickablePath.Focus(FocusState.Keyboard);
			}
		}
		private void VisiblePath_LostFocus(object _, RoutedEventArgs e)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			var element = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot);
			if (element is FlyoutBase or AppBarButton or Popup)
				return;

			if (element is not Control control)
			{
				if (ViewModel.IsEditModeEnabled)
					ViewModel.IsEditModeEnabled = false;
				return;
			}

			if (control.FocusState is not FocusState.Programmatic and not FocusState.Keyboard)
				ViewModel.IsEditModeEnabled = false;
			else if (ViewModel.IsEditModeEnabled)
				VisiblePath.Focus(FocusState.Programmatic);
		}

		private void SearchRegion_OnGotFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_GotFocus(sender, e);
		private void SearchRegion_LostFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_LostFocus(sender, e);
		private void SearchRegion_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
			else
				sender.Focus(FocusState.Keyboard);
		}

		private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
			=> ViewModel.VisiblePath_QuerySubmitted(sender, args);

		private void OngoingTasksActions_ProgressBannerPosted(object? _, StatusCenterItem e)
		{
			if (OngoingTasksViewModel is not null)
				OngoingTasksViewModel.NewItemAdded -= OngoingTasksActions_ProgressBannerPosted;

			// Displays a teaching tip the first time a banner is posted
			if (userSettingsService.AppSettingsService.ShowStatusCenterTeachingTip)
			{
				StatusCenterTeachingTip.IsOpen = true;
				userSettingsService.AppSettingsService.ShowStatusCenterTeachingTip = false;
			}
		}

		private void Button_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
		}

		private async void BackHistoryFlyout_Opening(object? sender, object e)
		{
			var shellPage = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage;
			if (shellPage is null)
				return;

			await AddHistoryItemsAsync(shellPage.BackwardStack, BackHistoryFlyout.Items, true);
		}

		private async void ForwardHistoryFlyout_Opening(object? sender, object e)
		{
			var shellPage = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage;
			if (shellPage is null)
				return;

			await AddHistoryItemsAsync(shellPage.ForwardStack, ForwardHistoryFlyout.Items, false);
		}

		private async Task AddHistoryItemsAsync(IEnumerable<PageStackEntry> items, IList<MenuFlyoutItemBase> flyoutItems, bool isBackMode)
		{
			// This may not seem performant, however it's the most viable trade-off to make.
			// Instead of constantly keeping track of back/forward stack and performing lookups
			// (which may degrade performance), we only add items in bulk when it's needed.
			// There's also a high chance the user might not use the feature at all in which case
			// the former approach would just waste extra performance gain

			flyoutItems.Clear();
			foreach (var item in items.Reverse())
			{
				if (item.Parameter is not NavigationArguments args || args.NavPathParam is null)
					continue;

				var fileName = SystemIO.Path.GetFileName(args.NavPathParam);

				// The fileName is empty if the path is (root) drive path
				if (string.IsNullOrEmpty(fileName))
					fileName = args.NavPathParam;

				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon() { Glyph = "\uE8B7" }, // Placeholder icon
					Text = fileName,
					Command = new RelayCommand<ToolbarHistoryItemModel?>(HistoryItemClicked),
					CommandParameter = new ToolbarHistoryItemModel(item, isBackMode)
				};

				flyoutItems?.Add(flyoutItem);

				// Start loading the thumbnail in the background
				_ = LoadFlyoutItemIconAsync(flyoutItem, args.NavPathParam);

				async Task LoadFlyoutItemIconAsync(MenuFlyoutItem flyoutItem, string path)
				{
					var imageSource = await NavigationHelpers.GetIconForPathAsync(path);

					if (imageSource is not null)
						flyoutItem.Icon = new ImageIcon() { Source = imageSource };
				}

				void HistoryItemClicked(ToolbarHistoryItemModel? itemModel)
				{
					if (itemModel is null)
						return;

					var shellPage = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage;
					if (shellPage is null)
						return;

					if (itemModel.IsBackMode)
					{
						// Remove all entries after the target entry in the BackwardStack
						while (shellPage.BackwardStack.Last() != itemModel.PageStackEntry)
						{
							shellPage.BackwardStack.RemoveAt(shellPage.BackwardStack.Count - 1);
						}

						// Navigate back
						shellPage.Back_Click();
					}
					else
					{
						// Remove all entries before the target entry in the ForwardStack
						while (shellPage.ForwardStack.First() != itemModel.PageStackEntry)
						{
							shellPage.ForwardStack.RemoveAt(0);
						}

						// Navigate forward
						shellPage.Forward_Click();
					}
				}
			}
		}

		private void ClickablePath_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			if (args.InputDevice != FocusInputDeviceKind.Keyboard)
				return;

			var previousControl = args.OldFocusedElement as FrameworkElement;
			if (previousControl?.Name == nameof(HomeButton) || previousControl?.Name == nameof(Refresh))
				ViewModel.IsEditModeEnabled = true;
		}

		private async void Omnibar_QuerySubmitted(Omnibar sender, OmnibarQuerySubmittedEventArgs args)
		{
			var mode = Omnibar.CurrentSelectedMode;

			// Path mode
			if (mode == OmnibarPathMode)
			{
				await ViewModel.HandleItemNavigationAsync(args.Text);
				(MainPageViewModel.SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
				return;
			}

			// Command palette mode
			else if (mode == OmnibarCommandPaletteMode)
			{
				var item = args.Item as NavigationBarSuggestionItem;

				// Try invoking built-in command
				foreach (var command in Commands)
				{
					if (command == Commands.None)
						continue;

					if (!string.Equals(command.Description, item?.Text, StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(command.Description, args.Text, StringComparison.OrdinalIgnoreCase))
						continue;

					await command.ExecuteAsync();
					(MainPageViewModel.SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
					return;
				}

				// Try invoking Windows app action
				if (ActionManager.Instance.ActionRuntime is not null && item?.ActionInstance is ActionInstance actionInstance)
				{
					// Workaround for https://github.com/microsoft/App-Actions-On-Windows-Samples/issues/7
					var action = ActionManager.Instance.ActionRuntime.ActionCatalog.GetAllActions()
						.FirstOrDefault(a => a.Id == actionInstance.Context.ActionId);

					if (action is not null)
					{
						var overload = action.GetOverloads().FirstOrDefault();
						await overload?.InvokeAsync(actionInstance.Context);
					}

					(MainPageViewModel.SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
					return;
				}

				await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidCommand.GetLocalizedResource(),
					string.Format(Strings.InvalidCommandContent.GetLocalizedResource(), args.Text));

				(MainPageViewModel.SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
				return;
			}

			// Search mode
			else if (mode == OmnibarSearchMode)
			{
			}
		}

		private async void Omnibar_TextChanged(Omnibar sender, OmnibarTextChangedEventArgs args)
		{
			if (args.Reason is not OmnibarTextChangeReason.UserInput)
				return;

			if (Omnibar.CurrentSelectedMode == OmnibarPathMode)
			{
				await ViewModel.PopulateOmnibarSuggestionsForPathMode();
			}
			else if (Omnibar.CurrentSelectedMode == OmnibarCommandPaletteMode)
			{
				ViewModel.PopulateOmnibarSuggestionsForCommandPaletteMode();
			}
			else if (Omnibar.CurrentSelectedMode == OmnibarSearchMode)
			{
			}
		}

		private async void BreadcrumbBar_ItemClicked(Controls.BreadcrumbBar sender, Controls.BreadcrumbBarItemClickedEventArgs args)
		{
			if (args.IsRootItem)
			{
				await ViewModel.HandleItemNavigationAsync("Home");
				return;
			}

			// Navigation to the current folder should not happen
			if (args.Index == ViewModel.PathComponents.Count - 1 ||
				ViewModel.PathComponents[args.Index].Path is not { } path)
				return;

			await ViewModel.HandleFolderNavigationAsync(path);
		}

		private async void BreadcrumbBar_ItemDropDownFlyoutOpening(object sender, BreadcrumbBarItemDropDownFlyoutEventArgs e)
		{
			if (e.IsRootItem)
			{
				IHomeFolder homeFolder = new HomeFolder();
				IContentPageContext contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

				e.Flyout.Items.Add(new MenuFlyoutHeaderItem() { Text = Strings.QuickAccess.GetLocalizedResource() });

				await foreach (var storable in homeFolder.GetQuickAccessFolderAsync())
				{
					if (storable is not IWindowsStorable windowsStorable)
						continue;

					var flyoutItem = new MenuFlyoutItem()
					{
						Text = windowsStorable.GetDisplayName(Windows.Win32.UI.Shell.SIGDN.SIGDN_PARENTRELATIVEFORUI),
						DataContext = windowsStorable.GetDisplayName(Windows.Win32.UI.Shell.SIGDN.SIGDN_DESKTOPABSOLUTEPARSING),
						Icon = new FontIcon() { Glyph = "\uE8B7" }, // As a placeholder
					};

					e.Flyout.Items.Add(flyoutItem);

					windowsStorable.TryGetThumbnail((int)(16f * App.AppModel.AppWindowDPI), Windows.Win32.UI.Shell.SIIGBF.SIIGBF_ICONONLY, out var thumbnailData);
					flyoutItem.Icon = new ImageIcon() { Source = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal) };

					windowsStorable.Dispose();

					flyoutItem.Click += (sender, args) =>
					{
						// NOTE: We should not pass a path string but pass the storable object itself in the future.
						contentPageContext.ShellPage!.NavigateToPath((string)flyoutItem.DataContext);
					};
				}

				e.Flyout.Items.Add(new MenuFlyoutHeaderItem() { Text = Strings.Drives.GetLocalizedResource() });

				await foreach (var storable in homeFolder.GetLogicalDrivesAsync())
				{
					if (storable is not IWindowsStorable windowsStorable)
						continue;

					var flyoutItem = new MenuFlyoutItem()
					{
						Text = windowsStorable.GetDisplayName(Windows.Win32.UI.Shell.SIGDN.SIGDN_PARENTRELATIVEFORUI),
						DataContext = windowsStorable.GetDisplayName(Windows.Win32.UI.Shell.SIGDN.SIGDN_DESKTOPABSOLUTEPARSING),
						Icon = new FontIcon() { Glyph = "\uE8B7" }, // As a placeholder
					};

					e.Flyout.Items.Add(flyoutItem);

					windowsStorable.TryGetThumbnail((int)(16f * App.AppModel.AppWindowDPI), Windows.Win32.UI.Shell.SIIGBF.SIIGBF_ICONONLY, out var thumbnailData);
					flyoutItem.Icon = new ImageIcon() { Source = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal) };

					windowsStorable.Dispose();

					flyoutItem.Click += (sender, args) =>
					{
						// NOTE: We should not pass a path string but pass the storable object itself in the future.
						contentPageContext.ShellPage!.NavigateToPath((string)flyoutItem.DataContext);
					};
				}

				return;
			}

			await ViewModel.SetPathBoxDropDownFlyoutAsync(e.Flyout, ViewModel.PathComponents[e.Index]);
		}

		private void BreadcrumbBar_ItemDropDownFlyoutClosed(object sender, BreadcrumbBarItemDropDownFlyoutEventArgs e)
		{
			// Clear the flyout items to save memory
			e.Flyout.Items.Clear();
		}

		private void Omnibar_ModeChanged(object sender, OmnibarModeChangedEventArgs e)
		{
			// Reset the command palette text when switching modes
			if (Omnibar.CurrentSelectedMode == OmnibarCommandPaletteMode)
				ViewModel.OmnibarCommandPaletteModeText = string.Empty;
		}

		private void Omnibar_LostFocus(object sender, RoutedEventArgs e)
		{
			// Reset to the default mode when Omnibar loses focus
			Omnibar.CurrentSelectedMode = Omnibar.Modes?.FirstOrDefault();
		}

		private async void Omnibar_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape)
			{
				Omnibar.IsFocused = false;
				(MainPageViewModel.SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
			}
			else if (e.Key is VirtualKey.Tab && Omnibar.IsFocused && !InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
			{
				var currentSelectedMode = Omnibar.CurrentSelectedMode;
				Omnibar.IsFocused = false;
				await Task.Delay(15);

				if (currentSelectedMode == OmnibarPathMode)
					BreadcrumbBar.Focus(FocusState.Keyboard);
				else if (currentSelectedMode == OmnibarCommandPaletteMode)
					OmnibarCommandPaletteMode.Focus(FocusState.Keyboard);
			}
		}

		private void NavigationButtonOverflowFlyoutButton_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			// Prevent the Omnibar from taking focus if the overflow button is hidden while the button is focused
			if (args.NewFocusedElement is TextBox)
				args.Cancel = true;
		}
	}
}
