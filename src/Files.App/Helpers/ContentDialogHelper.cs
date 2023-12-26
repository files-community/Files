// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.UI.Notifications;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for <see cref="ContentDialog"/>.
	/// </summary>
	internal static class ContentDialogHelper
	{
		public static event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Shows the app standard dialog to ensure consistency.
		/// The secondaryText can be un-assigned to hide its respective button.
		/// </summary>
		public static async Task<bool> ShowDialogAsync(string title, string message, string primaryText = "OK", string secondaryText = null)
		{
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = title,
				SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
				PrimaryButtonText = primaryText,
				SecondaryButtonText = secondaryText,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});

			return await ShowDialogAsync(dialog) == DynamicDialogResult.Primary;
		}

		public static async Task<DynamicDialogResult> ShowDialogAsync(DynamicDialog dialog)
		{
			try
			{
				if (MainWindow.Instance.Content is Frame rootFrame)
				{
					await dialog.ShowAsync();
					return dialog.DynamicResult;
				}
			}
			catch (Exception)
			{
			}

			return DynamicDialogResult.Cancel;
		}

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
		/// <param name="type">Type of drive to eject</param>
		/// <param name="result">Only true implies a successful device ejection</param>
		/// <returns></returns>
		public static async Task ShowDeviceEjectResultAsync(Data.Items.DriveType type, bool result)
		{
			if (type != Data.Items.DriveType.CDRom && result)
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
			else if (!result)
			{
				Debug.WriteLine("Can't eject device");

				await ContentDialogHelper.ShowDialogAsync(
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
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;
			}
			return contentDialog;
		}

		public static void CloseAllDialogs()
		{
			var openedDialogs = VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot);

			foreach (var item in openedDialogs)
			{
				if (item.Child is ContentDialog dialog)
				{
					dialog.Hide();
				}
			}
		}
	}
}
