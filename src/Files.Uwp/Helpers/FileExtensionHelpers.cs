using System;

namespace Files.Uwp.Helpers
{
    public static class FileExtensionHelpers
    {
        /// <summary>
        /// Check if the file extension is an image file.
        /// </summary>
        /// <param name="fileExtensionToCheck">The file extension to check.</param>
        /// <returns><c>true</c> if the fileExtensionToCheck is an image;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsImageFile(string fileExtensionToCheck)
        {
            if (string.IsNullOrEmpty(fileExtensionToCheck))
            {
                return false;
            }

            return fileExtensionToCheck.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   fileExtensionToCheck.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   fileExtensionToCheck.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                   fileExtensionToCheck.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if the file extension is a PowerShell script.
        /// </summary>
        /// <param name="fileExtensionToCheck">The file extension to check.</param>
        /// <returns><c>true</c> if the fileExtensionToCheck is a PowerShell script;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsPowerShellFile(string fileExtensionToCheck)
        {
            if (string.IsNullOrEmpty(fileExtensionToCheck))
            {
                return false;
            }

            return fileExtensionToCheck.Equals(".ps1", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if the file extension is a zip file.
        /// </summary>
        /// <param name="fileExtensionToCheck">The file extension to check.</param>
        /// <returns><c>true</c> if the fileExtensionToCheck is a zip bundle file;
        /// otherwise <c>false</c>.</returns>
        public static bool IsZipFile(string fileExtensionToCheck)
        {
            if (string.IsNullOrEmpty(fileExtensionToCheck))
            {
                return false;
            }

            return fileExtensionToCheck.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                   fileExtensionToCheck.Equals(".msix", StringComparison.OrdinalIgnoreCase) ||
                   fileExtensionToCheck.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInfFile(string fileExtensionToCheck)
        {
            if (string.IsNullOrEmpty(fileExtensionToCheck))
            {
                return false;
            }

            return fileExtensionToCheck.Equals(".inf", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if the file extension is a font file.
        /// </summary>
        /// <param name="fileExtensionToCheck">The file extension to check.</param>
        /// <returns><c>true</c> if the fileExtensionToCheck is a font file;
        /// otherwise <c>false</c>.</returns>
        /// <remarks>Font file types are; fon, otf, ttc, ttf</remarks>
        public static bool IsFontFile(string fileExtensionToCheck)
        {
            if (string.IsNullOrEmpty(fileExtensionToCheck))
            {
                return false;
            }

            return fileExtensionToCheck.Equals(".fon", StringComparison.OrdinalIgnoreCase) ||
                     fileExtensionToCheck.Equals(".otf", StringComparison.OrdinalIgnoreCase) ||
                     fileExtensionToCheck.Equals(".ttc", StringComparison.OrdinalIgnoreCase) ||
                     fileExtensionToCheck.Equals(".ttf", StringComparison.OrdinalIgnoreCase);
        }
    }
}
