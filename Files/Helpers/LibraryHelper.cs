using Files.Common;
using Files.Filesystem;
using Newtonsoft.Json;
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
                libraries.Sort((a, b) => a.Text.CompareTo(b.Text));
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
        /// Change name of a library.
        /// </summary>
        /// <param name="libraryFilePath">Library file path</param>
        /// <param name="name">The new name of the library (must be unique)</param>
        /// <returns>The new library if successfully renamed</returns>
        public static async Task<LibraryLocationItem> RenameLibrary(string libraryFilePath, string name)
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath) || string.IsNullOrWhiteSpace(name))
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
                { "action", "Rename" },
                { "library", libraryFilePath },
                { "name", name }
            });
            LibraryLocationItem library = null;
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("Rename"))
            {
                library = new LibraryLocationItem(JsonConvert.DeserializeObject<ShellLibraryItem>((string)response["Rename"]));
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
                request.Add("folders", folders);
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

        /// <summary>
        /// Delete a library.
        /// </summary>
        /// <param name="libraryFilePath">Library file path</param>
        /// <returns>True if the library successfully deleted</returns>
        public static async Task<bool> DeleteLibrary(string libraryFilePath)
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath))
            {
                return false;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null)
            {
                return false;
            }
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
            {
                { "Arguments", "ShellLibrary" },
                { "action", "Delete" },
                { "library", libraryFilePath }
            });
            if (status == AppServiceResponseStatus.Success && response.ContainsKey("Delete"))
            {
                if (string.IsNullOrEmpty(response["Delete"] as string))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
