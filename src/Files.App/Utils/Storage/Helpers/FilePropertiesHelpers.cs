// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Concurrent;
using Windows.Graphics;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Represents a helper class that helps users open and handle item properties window
	/// </summary>
	public static class FilePropertiesHelpers
	{
		/// <summary>
		/// Whether LayoutDirection (FlowDirection) is set to right-to-left (RTL)
		/// </summary>
		public static readonly bool FlowDirectionSettingIsRightToLeft =
			new ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"] == "RTL";

		/// <summary>
		/// Get window handle (hWnd) of the given properties window instance
		/// </summary>
		/// <param name="w">Window instance</param>
		/// <returns></returns>
		public static nint GetWindowHandle(Window w)
			=> WinRT.Interop.WindowNative.GetWindowHandle(w);

		private static TaskCompletionSource? PropertiesWindowsClosingTCS;
		private static BlockingCollection<WinUIEx.WindowEx> WindowCache = new();

		/// <summary>
		/// Open properties window
		/// </summary>
		/// <param name="associatedInstance">Associated main window instance</param>
		public static void OpenPropertiesWindow(IShellPage associatedInstance)
		{
			if (associatedInstance is null)
				return;

			object item;

			var page = associatedInstance.SlimContentPage;

			// Item(s) selected
			if (page is not null && page.IsItemSelected)
			{
				// Selected item(s)
				item = page.SelectedItems?.Count is 1
					? page.SelectedItem!
					: page.SelectedItems!;
			}
			// No items selected
			else
			{
				// Instance's current folder
				var folder = associatedInstance.FilesystemViewModel?.CurrentFolder;
				if (folder is null)
					return;

				item = folder;

				var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
				var drives = drivesViewModel.Drives;
				foreach (var drive in drives)
				{
					// Current folder is drive
					if (drive.Path.Equals(folder.ItemPath))
					{
						item = drive;
						break;
					}
				}
			}

			OpenPropertiesWindow(item, associatedInstance);
		}

		/// <summary>
		/// Open properties window with an explicitly specified item
		/// </summary>
		/// <param name="item">An item to view properties</param>
		/// <param name="associatedInstance">Associated main window instance</param>
		public static void OpenPropertiesWindow(object item, IShellPage associatedInstance)
		{
			var applicationService = Ioc.Default.GetRequiredService<IApplicationService>();

			if (item is null)
				return;

			var frame = new Frame
			{
				RequestedTheme = ThemeHelper.RootTheme
			};

			WinUIEx.WindowEx propertiesWindow;
			if (!WindowCache.TryTake(out propertiesWindow!))
			{
				propertiesWindow = new();
				propertiesWindow.Closed += PropertiesWindow_Closed;
			}

			propertiesWindow.IsMinimizable = false;
			propertiesWindow.IsMaximizable = false;
			propertiesWindow.MinWidth = 460;
			propertiesWindow.MinHeight = 550;
			propertiesWindow.Width = 800;
			propertiesWindow.Height = 550;
			propertiesWindow.Content = frame;
			propertiesWindow.SystemBackdrop = new AppSystemBackdrop(true);

			var appWindow = propertiesWindow.AppWindow;
			appWindow.Title = "Properties".GetLocalizedResource();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			appWindow.SetIcon(applicationService.AppIcoPath);

			frame.Navigate(
				typeof(Views.Properties.MainPropertiesPage),
				new PropertiesPageNavigationParameter
				{
					Parameter = item,
					AppInstance = associatedInstance,
					AppWindow = appWindow,
					Window = propertiesWindow
				},
				new SuppressNavigationTransitionInfo());

			// WINUI3: Move window to cursor position
			InteropHelpers.GetCursorPos(out var pointerPosition);
			var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
			var appWindowPos = new PointInt32
			{
				X = displayArea.WorkArea.X
					+ Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
				Y = displayArea.WorkArea.Y
					+ Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y)),
			};

			if (App.AppModel.IncrementPropertiesWindowCount() == 1)
				PropertiesWindowsClosingTCS = new();

			appWindow.Move(appWindowPos);
			appWindow.Show();
		}

		// Destruction of Window objects seems to cause access violation. (#12057)
		// So instead of destroying the Window object, cache it and reuse it as a workaround.
		private static void PropertiesWindow_Closed(object sender, WindowEventArgs args)
		{
			if (!App.AppModel.IsMainWindowClosed && sender is WinUIEx.WindowEx window)
			{
				args.Handled = true;

				window.AppWindow.Hide();
				window.Content = null;
				WindowCache.Add(window);

				if (App.AppModel.DecrementPropertiesWindowCount() == 0)
				{
					PropertiesWindowsClosingTCS!.TrySetResult();
					PropertiesWindowsClosingTCS = null;
				}
			}
			else
				App.AppModel.DecrementPropertiesWindowCount();
		}

		/// <summary>
		/// Destroy all cached properties windows
		/// </summary>
		/// <returns></returns>
		public static void DestroyCachedWindows()
		{
			while (WindowCache.TryTake(out var window))
			{
				window.Closed -= PropertiesWindow_Closed;
				window.Close();
			}
		}

		/// <summary>
		/// Returns task to wait for all properties windows to close
		/// </summary>
		/// <returns>Task to wait</returns>
		public static Task WaitClosingAll() => PropertiesWindowsClosingTCS?.Task ?? Task.CompletedTask;
	}
}
