using Files.Common;
using Files.Filesystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    /// <summary>
    /// Helper class for listing and managing Shell libraries.
    /// </summary>
    internal class LibraryHelper
    {
        // https://docs.microsoft.com/en-us/windows/win32/shell/library-ovw

        // TODO: move everything to LibraryManager from here?
        // TODO: do Rename and Delete like in case of normal files

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
    }
}