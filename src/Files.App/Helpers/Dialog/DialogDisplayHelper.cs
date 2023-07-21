// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for <see cref="ContentDialog"/>.
	/// </summary>
	internal static class DialogDisplayHelper
	{
		/// <summary>
		/// Shows a standard dialog provided to ensure consistency.
		/// <br/>
		/// The <paramref name="secondaryText"/> can be un-assigned to hide its respective button.
		/// </summary>
		/// <param name="title">The title of this dialog</param>
		/// <param name="message">THe main body message displayed within the dialog</param>
		/// <param name="primaryText">
		/// Text to be displayed on the primary button (which returns true when pressed).
		/// <br/>
		/// If not set, defaults to 'OK'</param>
		/// <param name="secondaryText">
		/// The (optional) secondary button text.
		/// <br/>
		/// If not set, it won't be presented to the user at all.
		/// </param>
		/// <returns>
		/// Returns true if the user presses primary text button; otherwise, false.
		/// </returns>
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
	}
}
