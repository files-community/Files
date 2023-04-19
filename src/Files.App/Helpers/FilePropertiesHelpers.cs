using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Graphics;

namespace Files.App.Helpers
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
		/// App logo location to use as window popup icon and title bar icon
		/// </summary>
		public static string LogoPath
			=> Path.Combine(Package.Current.InstalledLocation.Path, App.LogoPath);

		/// <summary>
		/// Get window handle (hWnd) of the given properties window instance
		/// </summary>
		/// <param name="w">Window instance</param>
		/// <returns></returns>
		public static nint GetWindowHandle(Window w)
			=> WinRT.Interop.WindowNative.GetWindowHandle(w);

		/// <summary>
		/// Open properties window
		/// </summary>
		/// <param name="associatedInstance">Associated main window instance</param>
		public static void OpenPropertiesWindow(IShellPage associatedInstance)
		{
			object item;

			var page = associatedInstance.SlimContentPage;

			// Item(s) selected
			if (page.IsItemSelected)
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
				var folder = associatedInstance.FilesystemViewModel.CurrentFolder;
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

			// Open properties window
			OpenPropertiesWindow(item, associatedInstance);
		}

		/// <summary>
		/// Open properties window with an explicitly specified item
		/// </summary>
		/// <param name="item">An item to view properties</param>
		/// <param name="associatedInstance">Associated main window instance</param>
		public static void OpenPropertiesWindow(object item, IShellPage associatedInstance)
		{
			if (item is null)
				return;

			var frame = new Frame
			{
				RequestedTheme = ThemeHelper.RootTheme
			};

			var propertiesWindow = new WinUIEx.WindowEx
			{
				IsMinimizable = false,
				IsMaximizable = false,
				MinWidth = 460,
				MinHeight = 550,
				Width = 800,
				Height = 550,
				Content = frame,
				Backdrop = new WinUIEx.MicaSystemBackdrop(),
			};

			var appWindow = propertiesWindow.AppWindow;
			appWindow.Title = "Properties".GetLocalizedResource();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			appWindow.SetIcon(LogoPath);

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

			appWindow.Show();

			// WINUI3: Move window to cursor position
			UWPToWinAppSDKUpgradeHelpers.InteropHelpers.GetCursorPos(out var pointerPosition);
			var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
			var appWindowPos = new PointInt32
			{
				X = displayArea.WorkArea.X
					+ Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
				Y = displayArea.WorkArea.Y
					+ Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y)),
			};

			appWindow.Move(appWindowPos);
		}
	}
}
