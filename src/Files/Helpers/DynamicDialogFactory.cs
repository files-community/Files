using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.ApplicationModel.Core;
using Windows.System;
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
                Margin = new Windows.UI.Xaml.Thickness(0, 0, 4, 0),
                TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap,
                Opacity = 0.0d
            };

            string previousInput = "";
            int previousCursorPosition = 0;
            bool ignoreTextChange = false;

            inputText.TextChanging += (textBox, e) =>
            {
                if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
                {
                    tipText.Opacity = 1.0d;
                    ignoreTextChange = true;
                    previousCursorPosition = textBox.SelectionStart - 1;
                    textBox.Text = previousInput;
                    textBox.SelectionStart = previousCursorPosition;
                    return;
                }
            };

            inputText.TextChanged += (s, e) =>
            {
                if (!ignoreTextChange)
                {
                    var textBox = s as TextBox;
                    dialog.ViewModel.AdditionalData = textBox.Text;
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        dialog.ViewModel.DynamicButtonsEnabled = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel;
                    }
                    else
                    {
                        dialog.ViewModel.DynamicButtonsEnabled = DynamicDialogButtons.Cancel;
                    }
                    tipText.Opacity = 0.0d;
                    previousInput = textBox.Text;

                }
                ignoreTextChange = false;
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