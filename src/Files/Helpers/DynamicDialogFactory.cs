using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    public static class DynamicDialogFactory
    {
        public static DynamicDialog GetFor_PropertySaveErrorDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "PropertySaveErrorDialog/Title".GetLocalized(),
                SubtitleText = "PropertySaveErrorMessage/Text".GetLocalized(), // We can use subtitle here as our content
                PrimaryButtonText = "PropertySaveErrorDialog/PrimaryButtonText".GetLocalized(),
                SecondaryButtonText = "PropertySaveErrorDialog/SecondaryButtonText".GetLocalized(),
                CloseButtonText = "Cancel".GetLocalized(),
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
            });
            return dialog;
        }

        public static DynamicDialog GetFor_ConsentDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "WelcomeDialog/Title".GetLocalized(),
                SubtitleText = "WelcomeDialogTextBlock/Text".GetLocalized(), // We can use subtitle here as our content
                PrimaryButtonText = "WelcomeDialog/PrimaryButtonText".GetLocalized(),
                PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
                DynamicButtons = DynamicDialogButtons.Primary
            });
            return dialog;
        }

        public static DynamicDialog GetFor_RenameDialog()
        {
            DynamicDialog dialog = null;
            TextBox inputText = new TextBox()
            {
                Height = 35d,
                PlaceholderText = "RenameDialogInputText/PlaceholderText".GetLocalized()
            };

            TextBlock tipText = new TextBlock()
            {
                Text = "RenameDialogSymbolsTip/Text".GetLocalized(),
                Margin = new Thickness(0, 0, 4, 0),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.0d
            };

            bool renameTextBoxPasted = false;
            string renameTextBoxPreviousRestrictedAttempt = "";
            int renameTextBoxPreviousCursorPosition = 0;
            string renameTextBoxPreviousInput = "";

            inputText.TextChanging += (textBox, args) =>
            {
                bool hasRestrictedCharacter = FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text);
                if (hasRestrictedCharacter)
                {
                    switch (renameTextBoxPasted, renameTextBoxPreviousRestrictedAttempt == textBox.Text)
                    {
                        case (true, true):
                            textBox.Text = textBox.Text.Remove(textBox.Text.Length - renameTextBoxPreviousRestrictedAttempt.Length);
                            string filtered = FilesystemHelpers.FilterRestrictedCharacters(renameTextBoxPreviousRestrictedAttempt);
                            textBox.Text += filtered;
                            textBox.SelectionStart = renameTextBoxPreviousCursorPosition + Math.Abs(textBox.Text.Length - renameTextBoxPreviousInput.Length);
                            tipText.Opacity = 0.0d;
                            break;
                        case (true, false):
                            tipText.Opacity = 1.0d;
                            renameTextBoxPreviousRestrictedAttempt = textBox.Text;
                            textBox.Text = renameTextBoxPreviousInput;
                            textBox.SelectionStart = renameTextBoxPreviousCursorPosition;
                            break;
                        default:
                            tipText.Opacity = 1.0d;
                            textBox.Text = renameTextBoxPreviousInput;
                            textBox.SelectionStart = renameTextBoxPreviousCursorPosition;
                            break;
                    }
                }
                else
                {
                    textBox.SelectionStart++;
                    tipText.Opacity = 0.0d;
                }
                renameTextBoxPreviousInput = textBox.Text;
                renameTextBoxPasted = false;
                dialog.ViewModel.AdditionalData = textBox.Text;
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    dialog.ViewModel.DynamicButtonsEnabled = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel;
                }
                else
                {
                    dialog.ViewModel.DynamicButtonsEnabled = DynamicDialogButtons.Cancel;
                }
            };

            inputText.SelectionChanged += (s, e) =>
            {
                renameTextBoxPreviousCursorPosition = ((TextBox)s).SelectionStart;
            };

            inputText.Paste += (s, e) =>
            {
                renameTextBoxPasted = true;
            };

            inputText.Loaded += (s, e) =>
            {
                // dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
                _ = CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => inputText.Focus(Windows.UI.Xaml.FocusState.Programmatic));
            };

            dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "RenameDialog/Title".GetLocalized(),
                SubtitleText = null,
                DisplayControl = new Grid()
                {
                    MinWidth = 300d,
                    Children =
                    {
                        new StackPanel()
                        {
                            Spacing = 4d,
                            Children =
                            {
                                inputText,
                                tipText
                            }
                        }
                    }
                },
                PrimaryButtonAction = (vm, e) =>
                {
                    vm.HideDialog(); // Rename successful
                },
                PrimaryButtonText = "RenameDialog/PrimaryButtonText".GetLocalized(),
                CloseButtonText = "Cancel".GetLocalized(),
                DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
            });

            return dialog;
        }
    }
}