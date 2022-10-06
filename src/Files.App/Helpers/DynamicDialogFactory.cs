using Files.App.Dialogs;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.App.Filesystem;
using Files.App.ViewModels.Dialogs;
using Files.App.Extensions;
using CommunityToolkit.WinUI;
using System;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.Helpers
{
    public static class DynamicDialogFactory
    {
        public static DynamicDialog GetFor_PropertySaveErrorDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "PropertySaveErrorDialog/Title".GetLocalizedResource(),
                SubtitleText = "PropertySaveErrorMessage/Text".GetLocalizedResource(), // We can use subtitle here as our content
                PrimaryButtonText = "Retry".GetLocalizedResource(),
                SecondaryButtonText = "PropertySaveErrorDialog/SecondaryButtonText".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
            });
            return dialog;
        }

        public static DynamicDialog GetFor_ConsentDialog()
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "WelcomeDialog/Title".GetLocalizedResource(),
                SubtitleText = "WelcomeDialogTextBlock/Text".GetLocalizedResource(), // We can use subtitle here as our content
                PrimaryButtonText = "WelcomeDialog/PrimaryButtonText".GetLocalizedResource(),
                PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
                DynamicButtons = DynamicDialogButtons.Primary
            });
            return dialog;
        }

        public static DynamicDialog GetFor_ShortcutNotFound(string targetPath)
        {
            DynamicDialog dialog = new(new DynamicDialogViewModel
            {
                TitleText = "ShortcutCannotBeOpened".GetLocalizedResource(),
                SubtitleText = string.Format("DeleteShortcutDescription".GetLocalizedResource(), targetPath),
                PrimaryButtonText = "Delete".GetLocalizedResource(),
                SecondaryButtonText = "No".GetLocalizedResource(),
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
            });
            return dialog;
        }

        public static DynamicDialog GetFor_RenameDialog()
        {
            DynamicDialog dialog = null;
            TextBox inputText = new TextBox()
            {
                Height = 35d,
                PlaceholderText = "RenameDialogInputText/PlaceholderText".GetLocalizedResource()
            };

            TextBlock tipText = new TextBlock()
            {
                Text = "RenameDialogSymbolsTip/Text".GetLocalizedResource(),
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 4, 0),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Opacity = 0.0d
            };

            inputText.BeforeTextChanging += async (textBox, args) =>
            {
                if (FilesystemHelpers.ContainsRestrictedCharacters(args.NewText))
                {
                    args.Cancel = true;
                    await inputText.DispatcherQueue.EnqueueAsync(() =>
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
                _ = inputText.DispatcherQueue.EnqueueAsync(() => inputText.Focus(Microsoft.UI.Xaml.FocusState.Programmatic));
            };

            dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "RenameDialog/Title".GetLocalizedResource(),
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
                PrimaryButtonText = "RenameDialog/PrimaryButtonText".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
            });

            return dialog;
        }

        public static DynamicDialog GetFor_FileInUseDialog(List<Shared.Win32Process> lockingProcess = null)
        {
            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "FileInUseDialog/Title".GetLocalizedResource(),
                SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".GetLocalizedResource() :
                    string.Format("FileInUseByDialog/Text".GetLocalizedResource(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
                PrimaryButtonText = "OK",
                DynamicButtons = DynamicDialogButtons.Primary
            });
            return dialog;
        }
    }
}