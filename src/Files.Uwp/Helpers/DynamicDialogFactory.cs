using Files.Uwp.Dialogs;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Uwp.Imaging;
using Windows.UI.Xaml.Media.Imaging;

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

        public static DynamicDialog GetFor_FileInUseDialogWithDetails(IEnumerable<string> filePath, List<Shared.Win32Process> lockingProcess = null)
        {
            var listView = new ListView() { SelectionMode = ListViewSelectionMode.None };

            DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
            {
                TitleText = "FileInUseDialog/Title".GetLocalized(),
                SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".GetLocalized() :
                    string.Format("FileInUseByDialog/Text".GetLocalized(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
                DisplayControl = new Grid()
                {
                    MinWidth = 300d,
                    Children =
                    {
                        listView
                    }
                },
                DisplayControlOnLoaded = (vm, e) =>
                {
                    var imagingService = Ioc.Default.GetRequiredService<IImagingService>();
                    filePath.ForEach(async (item) =>
                    {
                        await SafetyExtensions.IgnoreExceptions(async () =>
                        {
                            var imageModel = await imagingService.GetImageModelFromPathAsync(item, 48u);
                            var image = (imageModel as BitmapImageModel)?.GetImage<BitmapImage>();

                            var contentGrid = new Grid() { ColumnSpacing = 12 };
                            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = Windows.UI.Xaml.GridLength.Auto });
                            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star) });

                            var icon = new Image()
                            {
                                Source = image,
                                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left,
                                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                            };
                            Grid.SetColumn(icon, 0);
                            contentGrid.Children.Add(icon);

                            var content = new StackPanel()
                            {
                                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                                Children =
                                {
                                    new TextBlock()
                                    {
                                        Text = System.IO.Path.GetFileName(item),
                                    },
                                    new TextBlock()
                                    {
                                        Text = System.IO.Path.GetDirectoryName(item),
                                        TextTrimming = Windows.UI.Xaml.TextTrimming.CharacterEllipsis,
                                        FontSize = 12,
                                        Opacity = 0.6
                                    }
                                }
                            };
                            Grid.SetColumn(content, 1);
                            contentGrid.Children.Add(content);

                            listView.Items.Add(contentGrid);
                        });
                    });
                },
                PrimaryButtonAction = (vm, e) =>
                {
                    vm.HideDialog();
                },
                PrimaryButtonText = "OK",
                DynamicButtons = DynamicDialogButtons.Primary
            });

            return dialog;
        }
    }
}