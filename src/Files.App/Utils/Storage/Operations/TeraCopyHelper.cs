// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Hands copy and move operations off to TeraCopy when the integration is enabled.
	/// </summary>
	public static class TeraCopyHelper
	{
		public static string? DetectTeraCopyPath()
		{
			string[] candidates =
			[
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TeraCopy", "TeraCopy.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "TeraCopy", "TeraCopy.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TeraCopy", "TeraCopy.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeraCopy", "TeraCopy.exe"),
			];

			return candidates.FirstOrDefault(File.Exists);
		}

		public static bool CanRun(IList<IStorageItemWithPath> source, IList<string> destination)
		{
			if (source.Count == 0 || source.Count != destination.Count)
				return false;

			if (DetectTeraCopyPath() is null)
				return false;

			var trashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

			if (source.Any(item => !IsSupportedPath(item.Path) || trashBinService.IsUnderTrashBin(item.Path)))
				return false;

			// TeraCopy takes a single destination folder; require every item to land in the
			// same folder under its original name, otherwise the built-in engine handles it
			var destinationFolder = Path.GetDirectoryName(destination[0]);
			if (string.IsNullOrEmpty(destinationFolder) || !IsSupportedPath(destinationFolder) || trashBinService.IsUnderTrashBin(destinationFolder))
				return false;

			for (int i = 0; i < source.Count; i++)
			{
				if (!string.Equals(Path.GetDirectoryName(destination[i]), destinationFolder, StringComparison.OrdinalIgnoreCase) ||
					!string.Equals(Path.GetFileName(destination[i]), source[i].Name, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			return true;
		}

		public static async Task<ReturnResult> RunAsync(FilesystemOperationType operationType, IList<IStorageItemWithPath> source, IList<string> destination)
		{
			var teraCopyPath = DetectTeraCopyPath();
			if (teraCopyPath is null)
				return ReturnResult.Failed;

			var destinationFolder = Path.GetDirectoryName(destination[0])!;

			// TeraCopy reads the source list from a UTF-8 file, one path per line
			var fileList = Path.Combine(Path.GetTempPath(), $"Files-TeraCopy-{Guid.NewGuid():N}.txt");
			await File.WriteAllLinesAsync(fileList, source.Select(item => item.Path), new UTF8Encoding(true));

			var operation = operationType == FilesystemOperationType.Move ? "Move" : "Copy";
			var arguments = $"{operation} *{QuoteArgument(fileList)} {QuoteArgument(destinationFolder)}";

			var launched = await LaunchHelper.LaunchAppAsync(teraCopyPath, arguments, destinationFolder);

			return launched ? ReturnResult.Success : ReturnResult.Failed;
		}

		private static bool IsSupportedPath(string? path)
		{
			return !string.IsNullOrWhiteSpace(path) &&
				!path.StartsWith(@"\\?\", StringComparison.Ordinal) &&
				!FtpHelpers.IsFtpPath(path) &&
				!ZipStorageFolder.IsZipPath(path, false);
		}

		private static string QuoteArgument(string path)
		{
			// A trailing backslash (e.g. drive roots) would escape the closing quote
			return "\"" + (path.EndsWith('\\') ? path + "\\" : path) + "\"";
		}
	}
}
