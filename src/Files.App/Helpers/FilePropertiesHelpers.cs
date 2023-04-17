using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels;
using Files.App.Dialogs;
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
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics;

namespace Files.App.Helpers
{
	public static class FilePropertiesHelpers
	{
		public static readonly bool FlowDirectionSettingIsRightToLeft =
			new ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"] == "RTL";

		private static readonly Lazy<string> logoPath = new(GetFilesLogoPath);

		public static string LogoPath
			=> logoPath.Value;

		public static nint GetWindowHandle(Window w)
			=> WinRT.Interop.WindowNative.GetWindowHandle(w);

		public static void ShowProperties(IShellPage associatedInstance)
		{
			var item = GetItem(associatedInstance);
			OpenPropertiesWindowAsync(item, associatedInstance);

			static object GetItem(IShellPage instance)
			{
				var page = instance.SlimContentPage;
				if (page.IsItemSelected)
				{
					return page.SelectedItems?.Count is 1
						? page.SelectedItem!
						: page.SelectedItems!;
				}

				var folder = instance.FilesystemViewModel.CurrentFolder;

				var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

				var drives = drivesViewModel.Drives;
				foreach (var drive in drives)
					if (drive.Path.Equals(folder.ItemPath))
						return drive;

				return folder;
			}
		}

		public static void OpenPropertiesWindowAsync(object item, IShellPage associatedInstance)
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

		private static string GetFilesLogoPath()
			=> Path.Combine(Package.Current.InstalledLocation.Path, App.LogoPath);
	}
}
