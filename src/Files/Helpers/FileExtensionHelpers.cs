using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Helpers
{
    public static class FileExtensionHelpers
    {
        /// <summary>
        /// Checks the file extension.
        /// </summary>
        /// <param name="fileExtensionToCheck">The file extension to check.</param>
        /// <param name="fileExtensions">The file extensions.</param>
        /// <returns><c>true</c> if the fileExtensionToCheck matches one of the fileExtensions,
        /// <c>false</c> otherwise.</returns>
        public static bool CheckFileExtension(string fileExtensionToCheck, IEnumerable<string> fileExtensions)
        {
            if (string.IsNullOrEmpty(fileExtensionToCheck))
            {
                return false;
            }
            
            return fileExtensions.Any(ext => fileExtensionToCheck.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
