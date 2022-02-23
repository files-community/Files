﻿using Files.Dialogs;
using Files.Enums;
using Files.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
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
        public static async Task<bool> ShowDialogAsync(string title, string message, string primaryText = "OK", string secondaryText = null)
        {
            bool result = false;

            try
            {
                if (Window.Current.Content is Frame rootFrame)
                {
                    DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
                    {
                        TitleText = title,
                        SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
                        PrimaryButtonText = primaryText,
                        SecondaryButtonText = secondaryText,
                        DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
                    });

                    await dialog.ShowAsync();

                    result = dialog.DynamicResult == DynamicDialogResult.Primary;
                }
            }
            catch (Exception)
            {
            }

            return result;
        }
    }
}