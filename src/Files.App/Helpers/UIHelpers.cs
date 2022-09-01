using Files.App.Shell;
using Files.Shared;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class UIHelpers
	{
		public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog)
		{
			try
			{
				return await SetContentDialogRoot(dialog).ShowAsync();
			}
			catch // A content dialog is already open
			{
				return ContentDialogResult.None;
			}
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		public static void CloseAllDialogs()
		{
			var openedDialogs = VisualTreeHelper.GetOpenPopups(App.Window);

			foreach (var item in openedDialogs)
			{
				if (item.Child is ContentDialog dialog)
				{
					dialog.Hide();
				}
			}
		}

		private static IEnumerable<IconFileInfo> IconResources = UIHelpers.LoadSidebarIconResources();

		public static IconFileInfo GetIconResourceInfo(int index)
		{
			var icons = UIHelpers.IconResources;
			if (icons != null)
			{
				return icons.FirstOrDefault(x => x.Index == index);
			}
			return null;
		}

		public static async Task<BitmapImage> GetIconResource(int index)
		{
			var iconInfo = GetIconResourceInfo(index);
			if (iconInfo != null)
			{
				return await iconInfo.IconData.ToBitmapAsync();
			}
			return null;
		}

		private static IEnumerable<IconFileInfo> LoadSidebarIconResources()
		{
			string imageres = Path.Combine(CommonPaths.SystemRootPath, "System32", "imageres.dll");
			var imageResList = Win32API.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
					Constants.ImageRes.RecycleBin,
					Constants.ImageRes.NetworkDrives,
					Constants.ImageRes.Libraries,
					Constants.ImageRes.ThisPC,
					Constants.ImageRes.CloudDrives,
					Constants.ImageRes.Folder
				}, 32);

			return imageResList;
		}
	}
}