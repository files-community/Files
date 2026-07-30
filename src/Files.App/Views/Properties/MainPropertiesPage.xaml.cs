// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Graphics;
using Windows.System;

namespace Files.App.Views.Properties
{
	public sealed partial class MainPropertiesPage : BasePropertiesPage
	{
		private IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();

		private AppWindow? AppWindow => Window?.AppWindow;

		private Window? Window;

		private MainPropertiesViewModel? MainPropertiesViewModel { get; set; }

		public MainPropertiesPage()
		{
			InitializeComponent();

			if (AppLanguageHelper.IsPreferredLanguageRtl)
				FlowDirection = FlowDirection.RightToLeft;
		}


		// Navigates to specified properties page
		public bool TryNavigateToPage(PropertiesNavigationViewItemType pageType)
		{
			var viewModel = MainPropertiesViewModel;
			if (viewModel is null)
				return false;

			var page = viewModel.NavigationItems.FirstOrDefault(item => item.ItemType == pageType);
			if (page is null)
				return false;

			viewModel.SelectedNavigationItem = page;
			return true;
		}

		private void PropertiesSidebar_ItemInvoked(object sender, ItemInvokedEventArgs e)
		{
			if (sender is not SidebarItem { Item: PropertiesNavigationItem navItem })
				return;

			if (MainPropertiesViewModel is { } viewModel)
				viewModel.SelectedNavigationItem = navItem;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;

			Window = parameter.Window;

			base.OnNavigatedTo(e);

			MainPropertiesViewModel = new(
				Window,
				MainContentFrame,
				BaseProperties ?? throw new InvalidOperationException("The properties model has not been initialized."),
				parameter);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			if (Window is not { } window || MainPropertiesViewModel is not { } viewModel)
				return;

			AppThemeModeService.AppThemeModeChanged += AppThemeModeService_AppThemeModeChanged;
			window.Closed += Window_Closed;

			AppThemeModeService.ApplyResources();
			UpdatePageLayout(this.Width);
			PropertiesSidebar.SelectedItem = viewModel.SelectedNavigationItem;
			window.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
			window.AppWindow.Changed += AppWindow_Changed;
		}

		private int SetTitleBarDragRegion(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect)
		{
			source.SetRegionRects(NonClientRegionKind.Passthrough, [getScaledRect(BackwardNavigationButton, null)]);
			return (int)TitlebarArea.ActualHeight;
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
			=> UpdatePageLayout(e.NewSize.Width);

		private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
				Window?.Close();
		}

		private void UpdatePageLayout(double pageWidth)
		{
			VisualStateManager.GoToState(this, pageWidth < 600 ? "Narrow" : "Wide", true);
		}

		private async void AppThemeModeService_AppThemeModeChanged(object? sender, EventArgs e)
		{
			if (Parent is null || Window is not { } window)
				return;

			await DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				AppThemeModeService.SetAppThemeMode(window, window.AppWindow.TitleBar, AppThemeModeService.AppThemeMode, false);
			});
		}

		private void Window_Closed(object sender, WindowEventArgs args)
		{
			if (Window is not { } window)
				return;

			AppThemeModeService.AppThemeModeChanged -= AppThemeModeService_AppThemeModeChanged;
			window.Closed -= Window_Closed;
			window.AppWindow.Changed -= AppWindow_Changed;

			if (MainPropertiesViewModel?.ChangedPropertiesCancellationTokenSource is { IsCancellationRequested: false } cancellation)
			{
				cancellation.Cancel();
			}
		}

		private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs e)
		{
			Window?.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
		}

		public override async Task<bool> SaveChangesAsync()
			=> await Task.FromResult(false);

		public override void Dispose()
		{
		}
	}
}
