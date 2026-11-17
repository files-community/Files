// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.Win32;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Represents the navigation parameter of <see cref="ColumnCustomizationPage"/>.
	/// </summary>
	internal sealed partial class ColumnCustomizationNavigationArguments(WindowEx window, ColumnCustomizationViewModel viewModel)
	{
		public WindowEx Window { get; } = window;

		public ColumnCustomizationViewModel ViewModel { get; } = viewModel;
	}

	/// <summary>
	/// Represents the navigation parameter of <see cref="LayoutPropertyPickerPage"/>.
	/// </summary>
	internal sealed partial class LayoutPropertyPickerNavigationArguments(WindowEx window, LayoutPropertyPickerViewModel viewModel)
	{
		public WindowEx Window { get; } = window;

		public LayoutPropertyPickerViewModel ViewModel { get; } = viewModel;
	}

	/// <summary>
	/// Creates the windows that customize the columns, sorting and grouping of <see cref="DetailsLayoutPage"/>.
	/// </summary>
	internal static class ColumnCustomizationWindow
	{
		private static WindowEx? _window;

		public static void Show(ColumnCustomizationViewModel viewModel)
			=> Show(typeof(ColumnCustomizationPage), viewModel.Title, new(640, 560), new(560, 480), window => new ColumnCustomizationNavigationArguments(window, viewModel));

		public static void Show(LayoutPropertyPickerViewModel viewModel)
			=> Show(typeof(LayoutPropertyPickerPage), viewModel.Title, new(480, 600), new(400, 400), window => new LayoutPropertyPickerNavigationArguments(window, viewModel));

		[DynamicWindowsRuntimeCast(typeof(Frame))]
		private static void Show(Type pageType, string title, SizeInt32 size, SizeInt32 minSize, Func<WindowEx, object> createParameter)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			_window?.Close();

			var themeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();
			var frame = new Frame { RequestedTheme = themeService.AppThemeMode };
			var window = new WindowEx(minSize.Width, minSize.Height)
			{
				ExtendsContentIntoTitleBar = true,
				IsMaximizable = false,
				Content = frame,
				SystemBackdrop = new AppSystemBackdrop(true),
			};

			void ThemeService_AppThemeModeChanged(object? sender, EventArgs e)
			{
				window.DispatcherQueue.TryEnqueue(() =>
					themeService.SetAppThemeMode(window, window.AppWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false));
			}

			themeService.AppThemeModeChanged += ThemeService_AppThemeModeChanged;
			window.Closed += (_, _) =>
			{
				themeService.AppThemeModeChanged -= ThemeService_AppThemeModeChanged;
				if (_window == window)
					_window = null;
			};

			var appWindow = window.AppWindow;
			appWindow.Title = title;
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.SetIcon(AppLifecycleHelper.AppIconPath);
			frame.Navigate(pageType, createParameter(window), new SuppressNavigationTransitionInfo());

			themeService.SetAppThemeMode(window, appWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false);
			appWindow.Resize(new SizeInt32(
				Math.Max(1, Convert.ToInt32(size.Width * App.AppModel.AppWindowDPI)),
				Math.Max(1, Convert.ToInt32(size.Height * App.AppModel.AppWindowDPI))));

			PInvoke.GetCursorPos(out var pointerPosition);
			var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
			appWindow.Move(new PointInt32
			{
				X = displayArea.WorkArea.X + Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
				Y = displayArea.WorkArea.Y + Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y)),
			});

			_window = window;
			appWindow.Show();
			window.Activate();
		}
	}
}
