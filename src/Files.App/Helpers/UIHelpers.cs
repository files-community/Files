using CommunityToolkit.WinUI.Notifications;
using Files.App.Extensions;
using Files.App.Shell;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Files.App.Helpers
{
	public static class UIHelpers
	{
		public static event PropertyChangedEventHandler? PropertyChanged;

		private static bool canShowDialog = true;
		public static bool CanShowDialog
		{
			get => canShowDialog;
			private set
			{
				if (value == canShowDialog)
					return;
				canShowDialog = value;
				PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(CanShowDialog)));
			}
		}

		/// <summary>
		/// Displays a toast or dialog to indicate the result of
		/// a device ejection operation.
		/// </summary>
		/// <param name="result">Only true implies a successful device ejection</param>
		/// <returns></returns>
		public static async Task ShowDeviceEjectResultAsync(bool result)
		{
			if (result)
			{
				Debug.WriteLine("Device successfully ejected");

				var toastContent = new ToastContent()
				{
					Visual = new ToastVisual()
					{
						BindingGeneric = new ToastBindingGeneric()
						{
							Children =
							{
								new AdaptiveText()
								{
									Text = "EjectNotificationHeader".GetLocalizedResource()
								},
								new AdaptiveText()
								{
									Text = "EjectNotificationBody".GetLocalizedResource()
								}
							},
							Attribution = new ToastGenericAttributionText()
							{
								Text = "SettingsAboutAppName".GetLocalizedResource()
							}
						}
					},
					ActivationType = ToastActivationType.Protocol
				};

				// Create the toast notification
				var toastNotif = new ToastNotification(toastContent.GetXml());

				// And send the notification
				ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
			}
			else
			{
				Debug.WriteLine("Can't eject device");

				await DialogDisplayHelper.ShowDialogAsync(
					"EjectNotificationErrorDialogHeader".GetLocalizedResource(),
					"EjectNotificationErrorDialogBody".GetLocalizedResource());
			}
		}

		public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog)
		{
			if (!canShowDialog)
				return ContentDialogResult.None;

			try
			{
				CanShowDialog = false;
				return await SetContentDialogRoot(dialog).ShowAsync();
			}
			catch // A content dialog is already open
			{
				return ContentDialogResult.None;
			}
			finally
			{
				CanShowDialog = true;
			}
		}

		public static async Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog)
			where TViewModel : class, INotifyPropertyChanged
		{
			return (DialogResult)await ((ContentDialog)dialog).TryShowAsync();
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

		private static IEnumerable<IconFileInfo> SidebarIconResources = LoadSidebarIconResources();

		private static IconFileInfo ShieldIconResource = LoadShieldIconResource();

		public static IconFileInfo GetSidebarIconResourceInfo(int index)
		{
			var icons = UIHelpers.SidebarIconResources;
			return icons is not null ? icons.FirstOrDefault(x => x.Index == index) : null;
		}

		public static async Task<BitmapImage?> GetSidebarIconResource(int index)
		{
			var iconInfo = GetSidebarIconResourceInfo(index);

			return iconInfo is not null
				? await iconInfo.IconData.ToBitmapAsync()
				: null;
		}

		public static async Task<BitmapImage?> GetShieldIconResource()
		{
			return ShieldIconResource is not null
				? await ShieldIconResource.IconData.ToBitmapAsync()
				: null;
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

		private static IconFileInfo LoadShieldIconResource()
		{
			string imageres = Path.Combine(CommonPaths.SystemRootPath, "System32", "imageres.dll");
			var imageResList = Win32API.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
					Constants.ImageRes.ShieldIcon
				}, 16);

			return imageResList.First();
		}
	}
}