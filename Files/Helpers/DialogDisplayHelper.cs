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
        public static async Task<bool> ShowDialog(string title, string message, string primaryText = "OK", string secondaryText = null)
        {
            bool result = false;

            try
            {
                if (Window.Current.Content is Frame rootFrame)
                {
                    var dialog = new ContentDialog
                    {
                        Title = title,
                        Content = message,
                        PrimaryButtonText = primaryText
                    };

                    if (!string.IsNullOrEmpty(secondaryText))
                    {
                        dialog.SecondaryButtonText = secondaryText;
                    }
                    var dialogResult = await dialog.ShowAsync();

                    result = (dialogResult == ContentDialogResult.Primary);
                }
            }
            catch (Exception)
            {
            }

            return result;
        }
    }
}