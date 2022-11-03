using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using static Files.App.Views.Properties;

namespace Files.App.Helpers
{
	public class FilePropertiesHelpers
	{
		private static string? logoPath;

		public static string GetFilesLogoPath()
		{
			if (logoPath is not null) return logoPath;

			var appTilesPath = Path.Combine(Package.Current.InstalledLocation.Path, "Assets/AppTiles");
			if (Directory.Exists(Path.Combine(appTilesPath, "Dev")))
			{
				logoPath = Path.Combine(appTilesPath, "Dev", "Logo.ico");
			}
			else if (Directory.Exists(Path.Combine(appTilesPath, "Preview")))
			{
				logoPath = Path.Combine(appTilesPath, "Preview", "Logo.ico");
			}
			else if (Directory.Exists(Path.Combine(appTilesPath, "Release")))
			{
				logoPath = Path.Combine(appTilesPath, "Release", "Logo.ico");
			}
			else throw new InvalidOperationException("Cannot find Logo.ico from Assets/AppTiles.");

			return logoPath;
		}

		public static async void ShowProperties(IShellPage associatedInstance)
		{
			if (associatedInstance.SlimContentPage.IsItemSelected)
			{
				if (associatedInstance.SlimContentPage.SelectedItems.Count > 1)
					await OpenPropertiesWindowAsync(associatedInstance.SlimContentPage.SelectedItems, associatedInstance);
				else
					await OpenPropertiesWindowAsync(associatedInstance.SlimContentPage.SelectedItem, associatedInstance);
			}
			else
			{
				var path = System.IO.Path.GetPathRoot(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
				if (path is not null && path.Equals(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, StringComparison.OrdinalIgnoreCase))
					await OpenPropertiesWindowAsync(associatedInstance.FilesystemViewModel.CurrentFolder, associatedInstance);
				else
					await OpenPropertiesWindowAsync(App.DrivesManager.Drives
						.Single(x => x.Path.Equals(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath)), associatedInstance);
			}
		}

		public static async Task OpenPropertiesWindowAsync(object item, IShellPage associatedInstance)
		{
			if (item is null)
				return;

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				var frame = new Frame();
				frame.RequestedTheme = ThemeHelper.RootTheme;
				frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
				{
					Item = item,
					AppInstanceArgument = associatedInstance
				}, new SuppressNavigationTransitionInfo());

				// Initialize window
				var propertiesWindow = new WinUIEx.WindowEx()
				{
					IsMaximizable = false,
					IsMinimizable = false
				};
				var appWindow = propertiesWindow.AppWindow;

				// Set icon
				appWindow.SetIcon(GetFilesLogoPath());

				// Set content
				propertiesWindow.Content = frame;
				if (frame.Content is Properties properties)
					properties.appWindow = appWindow;

				// Set min size
				propertiesWindow.MinWidth = 460;
				propertiesWindow.MinHeight = 550;

				// Set backdrop
				propertiesWindow.Backdrop = new WinUIEx.MicaSystemBackdrop() { DarkTintOpacity = 0.8 };

				appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

				// Set window buttons background to transparent
				appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
				appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

				appWindow.Title = "PropertiesTitle".GetLocalizedResource();
				appWindow.Resize(new SizeInt32(460, 550));
				appWindow.Show();

				if (true) // WINUI3: move window to cursor position
				{
					UWPToWinAppSDKUpgradeHelpers.InteropHelpers.GetCursorPos(out var pointerPosition);
					var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
					var appWindowPos = new PointInt32()
					{
						X = displayArea.WorkArea.X + Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
						Y = displayArea.WorkArea.Y + Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y))
					};
					appWindow.Move(appWindowPos);
				}
			}
			else
			{
				var propertiesDialog = new PropertiesDialog();
				propertiesDialog.propertiesFrame.Tag = propertiesDialog;
				propertiesDialog.propertiesFrame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
				{
					Item = item,
					AppInstanceArgument = associatedInstance
				}, new SuppressNavigationTransitionInfo());
				await propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
			}
		}
	}
}
