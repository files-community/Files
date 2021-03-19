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

        // TODO: Watch library updates: https://docs.microsoft.com/windows/win32/shell/library-be-library-aware#keeping-in-sync-with-a-library 

        public static LibraryHelper Instance => new LibraryHelper();

        /// <summary>
        /// LibraryLocationItem cache. Access with <see cref="ListUserLibraries"/> or <see cref="Get"/>.
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
                        libraryItems.AddRange(JsonConvert.DeserializeObject<List<ShellLibraryItem>>((string)response["Enumerate"]).Select(lib => new LibraryLocationItem(lib)));
                        libraryItems.Sort((a, b) => a.Text.CompareTo(b.Text));
                    }
                }
            }
            if (!allowEmpty)
            {
                return libraryItems.Where(l => l.DefaultSaveFolder != null && l.Folders != null).ToList();
            }
            return libraryItems;
        }

        /// <summary>
        /// Check whether path belongs to a library.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <param name="defaultSaveFolderOnly">True to check default save folder only, false to check all</param>
        /// <returns>True if the specified path belongs to a library.</returns>
        public async Task<bool> IsLibraryPath(string path, bool defaultSaveFolderOnly = true)
        {
            var lib = await Get(path);
            if (lib == null || lib.IsEmpty)
            {
                return false;
            }
            if (string.Equals(path, lib.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (!defaultSaveFolderOnly && lib.Folders.Any(f => string.Equals(path, f, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        public async Task<LibraryLocationItem> Get(string libraryFilePath)
        {
            if (string.IsNullOrEmpty(libraryFilePath))
            {
                return null;
            }
            var libs = await ListUserLibraries(true);
            return libs.FirstOrDefault(l => l.Path == libraryFilePath);
        }

        /// <summary>
        /// Opens the Shell library management dialog for the specified library.
        /// </summary>
        /// <param name="libraryFilePath">The library to manage</param>
        public async void OpenLibraryManagerDialog(string libraryFilePath)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return;
            }
            var lib = await Get(libraryFilePath);
            if (lib == null)
            {
                return;
            }
            await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Manage" },
                { "library", lib.Path },
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
            if (libs.Any(l => string.Equals(l.Text, name, StringComparison.OrdinalIgnoreCase)))
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

        public async Task<string> RenameLibrary(string libraryFilePath, string name)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return null;
            }
            var lib = await Get(libraryFilePath);
            if (lib == null)
            {
                return null;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Rename" },
                { "library", lib.Path },
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

        public async Task<bool> DeleteLibrary(string libraryFilePath)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return false;
            }
            var lib = await Get(libraryFilePath);
            if (lib == null)
            {
                return false;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Delete" },
                { "library", lib.Path }
            });
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("ShellLibrary") && response.ContainsKey("action"))
            {
                if (response["action"] as string == "Delete")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
