// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for <see cref="ContentDialog"/>.
	/// </summary>
	internal static class ContentDialogHelper
	{
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
	}
}
