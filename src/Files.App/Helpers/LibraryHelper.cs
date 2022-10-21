using Files.Shared;
using Files.Shared.Enums;
using Files.App.Dialogs;
using Files.App.Filesystem;
using Files.App.ViewModels.Dialogs;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Files.App.Shell;
using Vanara.Windows.Shell;
using Vanara.PInvoke;
using Visibility = Microsoft.UI.Xaml.Visibility;

namespace Files.App.Helpers
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
        /// Get libraries of the current user.
        /// </summary>
        /// <returns>List of library items</returns>
        public static async Task<List<LibraryLocationItem>> ListUserLibraries()
        {
            var libraries = await Win32API.StartSTATask(() =>
            {
                try
                {
                    var libraryItems = new List<ShellLibraryItem>();
                    // https://docs.microsoft.com/en-us/windows/win32/search/-search-win7-development-scenarios#library-descriptions
                    var libFiles = Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*" + ShellLibraryItem.EXTENSION);
                    foreach (var libFile in libFiles)
                    {
                        using var shellItem = new ShellLibrary2(Shell32.ShellUtil.GetShellItemForPath(libFile), true);
                        if (shellItem is ShellLibrary2 library)
                        {
                            libraryItems.Add(ShellFolderExtensions.GetShellLibraryItem(library, libFile));
                        }
                    }
                    return libraryItems;
                }
                catch (Exception e)
                {
                    App.Logger.Warn(e);
                }

                return new();
            });

            return libraries.Select(lib => new LibraryLocationItem(lib)).ToList();
        }

        /// <summary>
        /// Create new library with the specified name.
        /// </summary>
        /// <param name="name">The name of the new library (must be unique)</param>
        /// <returns>The new library if successfully created</returns>
        public static async Task<LibraryLocationItem?> CreateLibrary(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return new(await Win32API.StartSTATask(() =>
            {
                try
                {
                    using var library = new ShellLibrary2(name, Shell32.KNOWNFOLDERID.FOLDERID_Libraries, false);
                    library.Folders.Add(ShellItem.Open(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))); // Add default folder so it's not empty
                    library.Commit();
                    library.Reload();
                    return Task.FromResult(ShellFolderExtensions.GetShellLibraryItem(library, library.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing)));
                }
                catch (Exception e)
                {
                    App.Logger.Warn(e);
                }

                return Task.FromResult<ShellLibraryItem>(null);
            }));
        }

        /// <summary>
        /// Update library details.
        /// </summary>
        /// <param name="libraryFilePath">Library file path</param>
        /// <param name="defaultSaveFolder">Update the default save folder or null to keep current</param>
        /// <param name="folders">Update the library folders or null to keep current</param>
        /// <param name="isPinned">Update the library pinned status or null to keep current</param>
        /// <returns>The new library if successfully updated</returns>
        public static async Task<LibraryLocationItem?> UpdateLibrary(string libraryFilePath, string defaultSaveFolder = null, string[] folders = null, bool? isPinned = null)
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath) || (defaultSaveFolder == null && folders == null && isPinned == null))
                // Nothing to update
                return null;

            var item = await Win32API.StartSTATask(() =>
            {
                try
                {
                    bool updated = false;
                    using var library = new ShellLibrary2(Shell32.ShellUtil.GetShellItemForPath(libraryFilePath), false);
                    if (folders != null)
                    {
                        if (folders.Length > 0)
                        {
                            var foldersToRemove = library.Folders.Where(f => !folders.Any(folderPath => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)));
                            foreach (var toRemove in foldersToRemove)
                            {
                                library.Folders.Remove(toRemove);
                                updated = true;
                            }
                            var foldersToAdd = folders.Distinct(StringComparer.OrdinalIgnoreCase)
                                                      .Where(folderPath => !library.Folders.Any(f => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)))
                                                      .Select(ShellItem.Open);
                            foreach (var toAdd in foldersToAdd)
                            {
                                library.Folders.Add(toAdd);
                                updated = true;
                            }
                            foreach (var toAdd in foldersToAdd)
                            {
                                toAdd.Dispose();
                            }
                        }
                    }
                    if (defaultSaveFolder != null)
                    {
                        library.DefaultSaveFolder = ShellItem.Open(defaultSaveFolder);
                        updated = true;
                    }
                    if (isPinned != null)
                    {
                        library.PinnedToNavigationPane = isPinned == true;
                        updated = true;
                    }
                    if (updated)
                    {
                        library.Commit();
                        library.Reload(); // Reload folders list
                        ShellFolderExtensions.GetShellLibraryItem(library, libraryFilePath);
                    }
                }
                catch (Exception e)
                {
                    App.Logger.Warn(e);
                }

                return Task.FromResult<ShellLibraryItem>(null);
            });

            return item != null ? new(item) : null;
        }

        public static async void ShowRestoreDefaultLibrariesDialog()
        {
            var dialog = new DynamicDialog(new DynamicDialogViewModel
            {
                TitleText = "DialogRestoreLibrariesTitleText".GetLocalizedResource(),
                SubtitleText = "DialogRestoreLibrariesSubtitleText".GetLocalizedResource(),
                PrimaryButtonText = "DialogRestoreLibrariesButtonText".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                PrimaryButtonAction = async (vm, e) =>
                {
                    await ContextMenu.InvokeVerb("restorelibraries", ShellLibraryItem.LibrariesPath);
                    await App.LibraryManager.UpdateLibrariesAsync();
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
                PlaceholderText = "FolderWidgetCreateNewLibraryInputPlaceholderText".GetLocalizedResource()
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
                TitleText = "FolderWidgetCreateNewLibraryDialogTitleText".GetLocalizedResource(),
                SubtitleText = "SideBarCreateNewLibrary/Text".GetLocalizedResource(),
                PrimaryButtonText = "DialogCreateLibraryButtonText".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
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

        public static bool IsLibraryPath(string path) => !string.IsNullOrEmpty(path) && path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase);
    }
}