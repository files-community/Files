using Files.Uwp.Dialogs;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.System;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Files.Uwp.Helpers
{
    public static class DynamicDialogFactory
    {
        public static DynamicDialog GetFor_PropertySaveErrorDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "PropertySaveErrorDialog/Title".GetLocalized(),
                SubtitleText = "PropertySaveErrorMessage/Text".GetLocalized(), // We can use subtitle here as our content
                PrimaryButtonText = "Retry".GetLocalized(),
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

            inputText.BeforeTextChanging += async (textBox, args) =>
            {
                if (FilesystemHelpers.ContainsRestrictedCharacters(args.NewText))
                {
                    args.Cancel = true;
                    await inputText.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var oldSelection = textBox.SelectionStart + textBox.SelectionLength;
                        var oldText = textBox.Text;
                        textBox.Text = FilesystemHelpers.FilterRestrictedCharacters(args.NewText);
                        textBox.SelectionStart = oldSelection + textBox.Text.Length - oldText.Length;
                        tipText.Opacity = 1.0d;
                    });
                }
                else
                {
                    dialog.ViewModel.AdditionalData = args.NewText;

                    if (!string.IsNullOrWhiteSpace(args.NewText))
                    {
                        dialog.ViewModel.DynamicButtonsEnabled = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel;
                    }
                    else
                    {
                        dialog.ViewModel.DynamicButtonsEnabled = DynamicDialogButtons.Cancel;
                    }

                    tipText.Opacity = 0.0d;
                }
            };

            inputText.Loaded += (s, e) =>
            {
                // dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
                _ = inputText.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                    () => inputText.Focus(Windows.UI.Xaml.FocusState.Programmatic));
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

        public static DynamicDialog GetFor_FileInUseDialog(List<Shared.Win32Process> lockingProcess = null)
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "FileInUseDialog/Title".GetLocalized(),
                SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".GetLocalized() :
                    string.Format("FileInUseByDialog/Text".GetLocalized(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
                PrimaryButtonText = "OK",
                DynamicButtons = DynamicDialogButtons.Primary
            });
            return dialog;
        }
    }
}