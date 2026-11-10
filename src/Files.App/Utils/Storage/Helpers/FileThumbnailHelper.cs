// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		// The shell serves these from hand-tuned icon frames and cache buckets; in-between sizes can yield padded or regenerated results
		private static readonly uint[] _standardSizes = [16, 24, 32, 48, 64, 96, 128, 256];

		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<byte[]?> GetIconAsync(string? path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var scaledSize = requestedSize * App.AppModel.AppWindowDPI;

			// Snap up to the next standard size; the result is never below the displayed size, so it only ever downscales.
			// Callers can opt out (sidebar) because snapping a small icon up makes the shell return a folder-style variant.
			var size = iconOptions.HasFlag(IconOptions.SkipSizeSnapping)
				? Math.Max(1u, (uint)Math.Round(scaledSize))
				: _standardSizes.FirstOrDefault(s => s >= scaledSize, _standardSizes[^1]);

			if (!isFolder && !iconOptions.HasFlag(IconOptions.ReturnIconOnly) && !iconOptions.HasFlag(IconOptions.ReturnOnlyIfCached))
			{
				var extension = Path.GetExtension(path);

				//Restrict to only %windir%\fonts
				if (FileExtensionHelpers.IsFontFile(extension) && path is not null && PathHelpers.IsInSystemFontsFolder(path))
				{
					var winrtThumbnail = await FontFileHelper.GetWinRTThumbnailAsync(path, (uint)size);
					if (winrtThumbnail is not null)
						return winrtThumbnail;

					if (!string.Equals(extension, ".fon", StringComparison.OrdinalIgnoreCase))
					{
						var fontThumbnail = await STATask.Run(() => FontFileHelper.GenerateFontThumbnail(path, (int)size), App.Logger);
						if (fontThumbnail is not null)
							return fontThumbnail;
					}
				}
			}

			return await GetServerIconAsync(path, (int)size, isFolder, (uint)iconOptions, false);
		}

		/// <summary>
		/// Returns overlay for given file or folder
		/// /// </summary>
		/// <param name="path"></param>
		/// <param name="isFolder"></param>
		/// <returns></returns>
		public static async Task<byte[]?> GetIconOverlayAsync(string? path, uint requestedSize, bool isFolder)
		{
			// Overlays render at 32px in thumbnail layouts and 16px in details/columns; scale by DPI so the badge isn't upscaled at fractional scaling
			var overlaySize = (requestedSize >= 48 ? 32u : 16u) * App.AppModel.AppWindowDPI;
			return await GetServerIconAsync(path, (int)overlaySize, isFolder, 0, true);
		}

		private static async Task<byte[]?> GetServerIconAsync(string? path, int size, bool isFolder, uint options, bool overlay)
		{
			try
			{
				var buffer = await Server.IconService.GetIconAsync(path ?? string.Empty, size, isFolder, options, overlay);
				return buffer.Length == 0 ? null : buffer.ToArray();
			}
			catch (COMException ex)
			{
				App.Logger.LogInformation(ex, "Could not load an icon from the server.");
				return null;
			}
		}

		[Obsolete]
		public static async Task<byte[]?> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions, bool isFolder = false)
		{
			var result = await GetIconAsync(filePath, thumbnailSize, isFolder, IconOptions.None);
			return result;
		}
	}
}
