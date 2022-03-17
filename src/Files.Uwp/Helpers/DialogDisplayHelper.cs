using Files.Dialogs;
using Files.Shared.Enums;
using Files.Uwp.Helpers;
using Files.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

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
        public static async Task<bool> ShowDialogAsync(AppWindow window, string title, string message, string primaryText = "OK", string secondaryText = null)
        {
            bool result = false;

            try
            {
                DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
                {
                    TitleText = title,
                    SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
                    PrimaryButtonText = primaryText,
                    SecondaryButtonText = secondaryText,
                    DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
                });
                dialog.XamlRoot = WindowManagementHelpers.GetWindowContentFromUIContext(window.UIContext).XamlRoot;
                await dialog.ShowAsync();

                result = dialog.DynamicResult == DynamicDialogResult.Primary;
            }
            catch (Exception)
            {
            }

            return result;
        }

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
        public static async Task<bool> ShowDialogAsync(Window window, string title, string message, string primaryText = "OK", string secondaryText = null)
        {
            bool result = false;

            try
            {
                DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
                {
                    TitleText = title,
                    SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
                    PrimaryButtonText = primaryText,
                    SecondaryButtonText = secondaryText,
                    DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
                });
                dialog.XamlRoot = window.Content.XamlRoot;
                await dialog.ShowAsync();

                result = dialog.DynamicResult == DynamicDialogResult.Primary;
            }
            catch (Exception)
            {
            }

            return result;
        }


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
        public static async Task<bool> ShowDialogAsync(object window, string title, string message, string primaryText = "OK", string secondaryText = null)
        {
            bool result = false;

            try
            {
                DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
                {
                    TitleText = title,
                    SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
                    PrimaryButtonText = primaryText,
                    SecondaryButtonText = secondaryText,
                    DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
                });
                dialog.XamlRoot = (window is AppWindow aw)? WindowManagementHelpers.GetWindowContentFromUIContext(aw.UIContext).XamlRoot : Window.Current.Content.XamlRoot;
                await dialog.ShowAsync();

                result = dialog.DynamicResult == DynamicDialogResult.Primary;
            }
            catch (Exception)
            {
            }

            return result;
        }
    }
}