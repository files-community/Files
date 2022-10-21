using Files.App.Shell;
using Files.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Helpers
{
    public static class LibrariesHelper
    {
        public static Task<ShellLibraryItem?> CreateAsync(string libraryName)
        {
            return Win32API.StartSTATask(() =>
            {
                try
                {
                    using var library = new ShellLibrary2(libraryName, Shell32.KNOWNFOLDERID.FOLDERID_Libraries, false);
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
            });
        }

        public static Task<ShellLibraryItem?> UpdateAsync(string defaultSaveFolder, string libPath, string[]? folders = null, bool? isPinned = null)
        {
            return Win32API.StartSTATask(() =>
            {
                try
                {
                    bool updated = false;
                    using var library = new ShellLibrary2(Shell32.ShellUtil.GetShellItemForPath(libPath), false);
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
                        ShellFolderExtensions.GetShellLibraryItem(library, libPath);
                    }
                }
                catch (Exception e)
                {
                    App.Logger.Warn(e);
                }

                return Task.FromResult<ShellLibraryItem>(null);
            });
        }

        public static Task<List<ShellLibraryItem>?> EnumerateLibrariesAsync()
        {
            return Win32API.StartSTATask(() =>
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
        }
    }
}
