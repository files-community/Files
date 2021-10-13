using Files.Common;
using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    /// <summary>
    /// Helper class for listing and managing Shell libraries.
    /// </summary>
    internal class LibraryHelper
    {
        // https://docs.microsoft.com/en-us/windows/win32/shell/library-ovw

        // TODO: move everything to LibraryManager from here?

        public static bool IsDefaultLibrary(string libraryFilePath)
        {
            // TODO: try to find a better way for this
            switch (Path.GetFileNameWithoutExtension(libraryFilePath))
            {
                case "CameraRoll":
                case "Documents":
                case "Music":
                case "Pictures":
                case "SavedPictures":
                case "Videos":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get libraries of the current user with the help of the FullTrust process.
        /// </summary>
        /// <returns>List of library items</returns>
        public static async Task<List<LibraryLocationItem>> ListUserLibraries()
        {
            List<LibraryLocationItem> libraries = null;
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return null;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Enumerate" }
            });
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("Enumerate"))
            {
                libraries = JsonConvert.DeserializeObject<List<ShellLibraryItem>>((string)response["Enumerate"]).Select(lib => new LibraryLocationItem(lib)).ToList();
            }
            return libraries;
        }

        /// <summary>
        /// Create new library with the specified name.
        /// </summary>
        /// <param name="name">The name of the new library (must be unique)</param>
        /// <returns>The new library if successfully created</returns>
        public static async Task<LibraryLocationItem> CreateLibrary(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return null;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Create" },
                { "library", name }
            });
            LibraryLocationItem library = null;
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("Create"))
            {
                library = new LibraryLocationItem(JsonConvert.DeserializeObject<ShellLibraryItem>((string)response["Create"]));
            }
            return library;
        }

        /// <summary>
        /// Update library details.
        /// </summary>
        /// <param name="libraryFilePath">Library file path</param>
        /// <param name="defaultSaveFolder">Update the default save folder or null to keep current</param>
        /// <param name="folders">Update the library folders or null to keep current</param>
        /// <param name="isPinned">Update the library pinned status or null to keep current</param>
        /// <returns>The new library if successfully updated</returns>
        public static async Task<LibraryLocationItem> UpdateLibrary(string libraryFilePath, string defaultSaveFolder = null, string[] folders = null, bool? isPinned = null)
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath) || (defaultSaveFolder == null && folders == null && isPinned == null))
            {
                // Nothing to update
                return null;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return null;
            }
            var request = new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Update" },
                { "library", libraryFilePath }
            };
            if (!string.IsNullOrEmpty(defaultSaveFolder))
            {
                request.Add("defaultSaveFolder", defaultSaveFolder);
            }
            if (folders != null)
            {
                request.Add("folders", JsonConvert.SerializeObject(folders));
            }
            if (isPinned != null)
            {
                request.Add("isPinned", isPinned);
            }
            var (status, response) = await connection.SendMessageForResponseAsync(request);
            LibraryLocationItem library = null;
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("Update"))
            {
                library = new LibraryLocationItem(JsonConvert.DeserializeObject<ShellLibraryItem>((string)response["Update"]));
            }
            return library;
        }

        public static async void ShowRestoreDefaultLibrariesDialog()
        {
            var dialog = new DynamicDialog(new DynamicDialogViewModel
            {
                TitleText = "DialogRestoreLibrariesTitleText".GetLocalized(),
                SubtitleText = "DialogRestoreLibrariesSubtitleText".GetLocalized(),
                PrimaryButtonText = "DialogRestoreLibrariesButtonText".GetLocalized(),
                CloseButtonText = "Cancel".GetLocalized(),
                PrimaryButtonAction = async (vm, e) =>
                {
                    var connection = await AppServiceConnectionHelper.Instance;
                    if (connection != null)
                    {
                        await connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "InvokeVerb" },
                            { "FilePath", ShellLibraryItem.LibrariesPath },
                            { "Verb", "restorelibraries" }
                        });
                    }
                    await App.LibraryManager.EnumerateLibrariesAsync();
                },
                CloseButtonAction = (vm, e) => vm.HideDialog(),
                KeyDownAction = (vm, e) =>
                {
                    if (e.Key == VirtualKey.Escape)
                    {
                        vm.HideDialog();
                    }
                },
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
            });
            await dialog.ShowAsync();
        }

        public static async void ShowCreateNewLibraryDialog()
        {
            var inputText = new TextBox
            {
                PlaceholderText = "FolderWidgetCreateNewLibraryInputPlaceholderText".GetLocalized()
            };
            var tipText = new TextBlock
            {
                Text = string.Empty,
                Visibility = Visibility.Collapsed
            };

            var dialog = new DynamicDialog(new DynamicDialogViewModel
            {
                DisplayControl = new Grid
                {
                    Children =
                    {
                        new StackPanel
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
                TitleText = "FolderWidgetCreateNewLibraryDialogTitleText".GetLocalized(),
                SubtitleText = "FolderWidgetCreateNewLibraryDialogSubtitleText".GetLocalized(),
                PrimaryButtonText = "DialogCreateLibraryButtonText".GetLocalized(),
                CloseButtonText = "Cancel".GetLocalized(),
                PrimaryButtonAction = async (vm, e) =>
                {
                    var (result, reason) = App.LibraryManager.CanCreateLibrary(inputText.Text);
                    tipText.Text = reason;
                    tipText.Visibility = result ? Visibility.Collapsed : Visibility.Visible;
                    if (!result)
                    {
                        e.Cancel = true;
                        return;
                    }
                    await App.LibraryManager.CreateNewLibrary(inputText.Text);
                },
                CloseButtonAction = (vm, e) =>
                {
                    vm.HideDialog();
                },
                KeyDownAction = async (vm, e) =>
                {
                    if (e.Key == VirtualKey.Enter)
                    {
                        await App.LibraryManager.CreateNewLibrary(inputText.Text);
                    }
                    else if (e.Key == VirtualKey.Escape)
                    {
                        vm.HideDialog();
                    }
                },
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
            });
            await dialog.ShowAsync();
        }

        public static bool IsLibraryPath(string path) => !string.IsNullOrEmpty(path) && path.ToLower().EndsWith(ShellLibraryItem.EXTENSION);
    }
}