using Files.Dialogs;
using Files.Filesystem;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
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
                CloseButtonText= "PropertySaveErrorDialog/CloseButtonText".GetLocalized(),
                DynamicButtons = DynamicButtons.Primary | DynamicButtons.Secondary | DynamicButtons.Cancel
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
                DynamicButtons = DynamicButtons.Primary
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
                Visibility = Windows.UI.Xaml.Visibility.Collapsed
            };

            inputText.TextChanged += (s, e) =>
            {
                var textBox = s as TextBox;
                dialog.ViewModel.AdditionalData = textBox.Text;

                if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
                {
                    dialog.ViewModel.DynamicButtonsEnabled = DynamicButtons.Cancel;
                    tipText.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    return;
                }
                else if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    dialog.ViewModel.DynamicButtonsEnabled = DynamicButtons.Primary | DynamicButtons.Cancel;
                }
                else
                {
                    dialog.ViewModel.DynamicButtonsEnabled = DynamicButtons.Cancel;
                }

                tipText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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
                            Orientation = Orientation.Vertical,
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
                CloseButtonText = "RenameDialog/SecondaryButtonText".GetLocalized(),
                DynamicButtonsEnabled = DynamicButtons.Cancel,
                DynamicButtons = DynamicButtons.Primary | DynamicButtons.Cancel
            })

            return dialog;
        }
    }
}
