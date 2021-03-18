using Files.Common;
using Files.Filesystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    /// <summary>
    /// Helper class for listing and managing Shell libraries.
    /// </summary>
    public class LibraryHelper
    {
        // https://docs.microsoft.com/en-us/windows/win32/shell/library-ovw

        /// TODO:
        /// - Create UI part of CreateLibrary method and test it
        /// - Watch library updates: https://docs.microsoft.com/windows/win32/shell/library-be-library-aware#keeping-in-sync-with-a-library 
        /// - Implement library rename
        /// - Implement library deletion
        /// - 

        public static LibraryHelper Instance => new LibraryHelper();

        /// <summary>
        /// LibraryLocationItem cache. Access with <see cref="ListUserLibraries"/>.
        /// </summary>
        private readonly List<LibraryLocationItem> libraryItems = new List<LibraryLocationItem>();

        /// <summary>
        /// Get libraries of the current user with the help of the FullTrust process in case there are no cached items or the 2nd parameter is true.
        /// </summary>
        /// <param name="allowEmpty">Keep empty libraries in result (where Path == null)</param>
        /// <param name="refresh">Re-enumerate libraries in case <see cref="libraryItems"/> is empty</param>
        /// <returns>List of library items</returns>
        public async Task<List<LibraryLocationItem>> ListUserLibraries(bool allowEmpty, bool refresh = false)
        {
            if (libraryItems.Count == 0 || refresh)
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var request = new ValueSet
                    {
                        { "Arguments", "ShellLibrary" },
                        { "action", "Enumerate" }
                    };
                    var (status, response) = await connection.SendMessageForResponseAsync(request);

                    if (status == AppServiceResponseStatus.Success && response.ContainsKey("Enumerate"))
                    {
                        libraryItems.Clear();
                        foreach (var lib in JsonConvert.DeserializeObject<List<ShellLibraryItem>>((string)response["Enumerate"]))
                        {
                            libraryItems.Add(new LibraryLocationItem(lib.Path, lib.Name, lib.DefaultSaveFolder, lib.Folders, lib.IsPinned));
                        }
                        libraryItems.Sort((a, b) => a.Text.CompareTo(b.Text));
                    }
                }
            }
            if (!allowEmpty)
            {
                return libraryItems.Where(l => l.Path != null).ToList();
            }
            return libraryItems;
        }

        /// <summary>
        /// Find library with the specified path.
        /// </summary>
        /// <param name="path">The path contained by a library</param>
        /// <param name="defaultSavePathOnly">True to check default save path only, false to check all</param>
        /// <returns>The <see cref="LibraryLocationItem"/> with the specified default save path or null if not found.</returns>
        public async Task<LibraryLocationItem> FindByPath(string path, bool defaultSavePathOnly = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            var libs = await ListUserLibraries(false);
            return libs.FirstOrDefault(l =>
            {
                if (defaultSavePathOnly)
                {
                    return string.Equals(l.Path, path, StringComparison.InvariantCultureIgnoreCase);
                }
                else
                {
                    return l.Paths.Any(p => string.Equals(p, path, StringComparison.InvariantCultureIgnoreCase));
                }
            });
        }

        /// <summary>
        /// Check whether path belongs to a library.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <param name="defaultSavePathOnly">True to check default save path only, false to check all</param>
        /// <returns>True if the specified path belongs to a library.</returns>
        public async Task<bool> IsLibraryPath(string path, bool defaultSavePathOnly = true) => await FindByPath(path, defaultSavePathOnly) != null;

        /// <summary>
        /// Get additional library folder paths from the library default save path.
        /// </summary>
        /// <param name="defaultSavePath">The default save path of the library</param>
        /// <returns>The additional library folder paths. Empty array returned in case of a single directory library or null if the path is not a default save path of any library.</returns>
        public async Task<string[]> GetExtraLibraryPaths(string defaultSavePath)
        {
            var lib = await FindByPath(defaultSavePath);
            if (lib == null)
            {
                return null;
            }
            return lib.Paths?.Where(p => p != defaultSavePath)?.ToArray();
        }

        /// <summary>
        /// Opens the Shell library management dialog for the specified library.
        /// </summary>
        /// <param name="lib">The library to manage</param>
        public async void OpenLibraryManagerDialog(LibraryLocationItem lib)
        {
            if (lib == null)
            {
                return;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return;
            }
            var libs = await ListUserLibraries(true);
            if (!libs.Any(l => l.LibraryPath == lib.LibraryPath))
            {
                return;
            }
            await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Manage" },
                { "library", lib.LibraryPath },
                { "dialogOwnerHandle", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
            });
        }

        /// <summary>
        /// Create new library with the specified name.
        /// </summary>
        /// <param name="name">The name of the new library (must be unique)</param>
        /// <returns>True if the new library successfully created</returns>
        public async Task<bool> CreateLibrary(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return false;
            }
            var libs = await ListUserLibraries(true);
            if (libs.Any(l => string.Equals(l.Text, name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Create" },
                { "library", name }
            });
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("ShellLibrary"))
            {
                return response["ShellLibrary"] as string == "Create";
            }
            return false;
        }

        public async Task<string> RenameLibrary(LibraryLocationItem lib, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return null;
            }
            var libs = await ListUserLibraries(true);
            if (!libs.Any(l => l.LibraryPath == lib.LibraryPath))
            {
                return null;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Rename" },
                { "library", lib.LibraryPath },
                { "name", name }
            });
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("ShellLibrary") && response.ContainsKey("name"))
            {
                if (response["name"] as string == name)
                {
                    return name;
                }
            }
            return null;
        }
    }
}
