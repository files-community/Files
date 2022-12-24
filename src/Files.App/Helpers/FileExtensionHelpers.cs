using System;
using System.Linq;

namespace Files.App.Helpers
{
	public static class FileExtensionHelpers
	{
		/// <summary>
		/// Check if the file extension is an image file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an image;
		/// otherwise, <c>false</c>.</returns>
		public static bool IsImageFile(string? fileExtensionToCheck)
		{
			if (string.IsNullOrWhiteSpace(fileExtensionToCheck))
				return false;

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
			return !string.IsNullOrEmpty(fileExtensionToCheck) &&
				fileExtensionToCheck.Equals(".ps1", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Check if the file extension is a zip file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a zip bundle file;
		/// otherwise <c>false</c>.</returns>
		public static bool IsZipFile(string? fileExtensionToCheck)
		{
			if (string.IsNullOrWhiteSpace(fileExtensionToCheck))
				return false;

			return new[] { ".zip", ".msix", ".appx", ".msixbundle", ".7z", ".rar", ".tar" }
				.Contains(fileExtensionToCheck, StringComparer.OrdinalIgnoreCase);
		}

		public static bool IsBrowsableZipFile(string? filePath, out string? ext)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				ext = null;
				return false;
			}

			ext = new[] { ".zip", ".7z", ".rar", ".tar" } // Only ext we want to browse
				.FirstOrDefault(x => filePath.Contains(x, StringComparison.OrdinalIgnoreCase));
			return ext is not null;
		}

		public static bool IsInfFile(string? fileExtensionToCheck)
		{
			return !string.IsNullOrWhiteSpace(fileExtensionToCheck) &&
				fileExtensionToCheck.Equals(".inf", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Check if the file extension is a font file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a font file;
		/// otherwise <c>false</c>.</returns>
		/// <remarks>Font file types are; fon, otf, ttc, ttf</remarks>
		public static bool IsFontFile(string? fileExtensionToCheck)
		{
			if (string.IsNullOrWhiteSpace(fileExtensionToCheck))
				return false;

			return fileExtensionToCheck.Equals(".fon", StringComparison.OrdinalIgnoreCase) ||
					 fileExtensionToCheck.Equals(".otf", StringComparison.OrdinalIgnoreCase) ||
					 fileExtensionToCheck.Equals(".ttc", StringComparison.OrdinalIgnoreCase) ||
					 fileExtensionToCheck.Equals(".ttf", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Check if the file path is a shortcut file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is a shortcut file;
		/// otherwise <c>false</c>.</returns>
		/// <remarks>Shortcut file type is .lnk</remarks>
		public static bool IsShortcutFile(string? filePathToCheck)
		{
			return !string.IsNullOrWhiteSpace(filePathToCheck) &&
				filePathToCheck.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsWebLinkFile(string? filePathToCheck)
		{
			return !string.IsNullOrWhiteSpace(filePathToCheck) &&
				filePathToCheck.EndsWith(".url", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsShortcutOrUrlFile(string? filePathToCheck)
			=> IsShortcutFile(filePathToCheck) || IsWebLinkFile(filePathToCheck);
	}
}
