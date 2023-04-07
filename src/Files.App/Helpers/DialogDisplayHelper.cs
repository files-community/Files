using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	internal class DialogDisplayHelper
	{
		/// <summary>
		/// Standard dialog, to ensure consistency.
		/// The secondaryText can be un-assigned to hide its respective button.
		/// Result is true if the user presses primary text button
		/// </summary>
		/// <param name="title">
		/// The title of this dialog
		/// </param>
		/// <param name="message">
		/// THe main body message displayed within the dialog
		/// </param>
		/// <param name="primaryText">
		/// Text to be displayed on the primary button (which returns true when pressed).
		/// If not set, defaults to 'OK'
		/// </param>
		/// <param name="secondaryText">
		/// The (optional) secondary button text.
		/// If not set, it won't be presented to the user at all.
		/// </param>
		public static Task<bool> ShowDialogAsync(string title, string message, string primaryText = "OK", string secondaryText = null)
		{
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = title,
				SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
				PrimaryButtonText = primaryText,
				SecondaryButtonText = secondaryText,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});

			return ShowDialogAsync(dialog).ContinueWith(t => t.Result == DynamicDialogResult.Primary);
		}

		public static Task<DynamicDialogResult> ShowDialogAsync(DynamicDialog dialog)
		{
			try
			{
				if (App.Window.Content is Frame rootFrame)
				{
					return dialog.ShowAsync().ContinueWith(_ => dialog.DynamicResult);
				}
			}
			catch (Exception)
			{
			}

			return Task.FromResult(DynamicDialogResult.Cancel);
		}
	}
}