// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Properties
{
	public sealed partial class MainPropertiesPage : BasePropertiesPage
	{
		private AppWindow AppWindow;

		private Window Window;

		private MainPropertiesViewModel MainPropertiesViewModel { get; set; }

		public MainPropertiesPage()
		{
			InitializeComponent();

			if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
				FlowDirection = FlowDirection.RightToLeft;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;

			Window = parameter.Window;
			AppWindow = parameter.AppWindow;

			Window.Closed += Window_Closed;

			AppThemeHelper.ThemeModeChanged += AppSettings_ThemeModeChanged;

			MainPropertiesViewModel = new(Window, AppWindow, MainContentFrame, BaseProperties, parameter);

			base.OnNavigatedTo(e);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			AppSettings_ThemeModeChanged(null, null);

			UpdatePageLayout();
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdatePageLayout();
		}

		private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
				Window.Close();
		}

		private void UpdatePageLayout()
		{
			// Drag zone
			DragZoneHelper.SetDragZones(Window, (int)TitlebarArea.ActualHeight, 40);

			// NavigationView Pane Mode
			MainPropertiesWindowNavigationView.PaneDisplayMode =
				ActualWidth <= 600
					? NavigationViewPaneDisplayMode.LeftCompact
					: NavigationViewPaneDisplayMode.Left;

			// Collapse NavigationViewItem Content text
			if (ActualWidth <= 600)
				foreach (var item in MainPropertiesViewModel.NavigationViewItems) item.IsCompact = true;
			else
				foreach (var item in MainPropertiesViewModel.NavigationViewItems) item.IsCompact = false;
		}

		private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
		{
			AppThemeHelper.ApplyTheme(Window, AppThemeHelper.RootTheme, false);
		}

		private void Window_Closed(object sender, WindowEventArgs args)
		{
			AppThemeHelper.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			Window.Closed -= Window_Closed;

			if (MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource is not null &&
				!MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource.IsCancellationRequested)
			{
				MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource.Cancel();
			}
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(false);

		public override void Dispose()
		{
		}
	}
}
