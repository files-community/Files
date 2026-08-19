// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.IO;
using Windows.Storage;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the app data locations, with fallbacks that allow the app to run
	/// without package identity (portable distribution).
	/// </summary>
	public static class AppDataHelper
	{
		/// <summary>
		/// Gets the local app data folder path.
		/// </summary>
		public static string LocalFolderPath { get; }

		/// <summary>
		/// Gets the local cache folder path.
		/// </summary>
		public static string LocalCacheFolderPath { get; }

		/// <summary>
		/// Gets the temporary folder path.
		/// </summary>
		public static string TemporaryFolderPath { get; }

		/// <summary>
		/// Gets the key/value settings store shared by all instances of the app.
		/// </summary>
		public static IDictionary<string, object> LocalSettingsValues { get; }

		static AppDataHelper()
		{
			if (AppRuntimeHelper.IsPackaged)
			{
				LocalFolderPath = ApplicationData.Current.LocalFolder.Path;
				LocalCacheFolderPath = ApplicationData.Current.LocalCacheFolder.Path;
				TemporaryFolderPath = ApplicationData.Current.TemporaryFolder.Path;
				LocalSettingsValues = ApplicationData.Current.LocalSettings.Values;
			}
			else
			{
				// LocalSettings carries only cross-instance coordination state; user data
				// files live under the exe-relative LocalFolderPath
				LocalFolderPath = GetPortableDataPath();
				LocalCacheFolderPath = Path.Combine(LocalFolderPath, "Cache");
				TemporaryFolderPath = Path.Combine(Path.GetTempPath(), "FilesPortable");

				Directory.CreateDirectory(LocalCacheFolderPath);
				Directory.CreateDirectory(TemporaryFolderPath);

				LocalSettingsValues = Microsoft.Windows.Storage.ApplicationData
					.GetForUnpackaged("Files Community", AppRuntimeHelper.PackageName)
					.LocalSettings.Values;
			}
		}

		/// <summary>
		/// Gets the local app data folder as a <see cref="StorageFolder"/>.
		/// </summary>
		public static Task<StorageFolder> GetLocalFolderAsync()
			=> StorageFolder.GetFolderFromPathAsync(LocalFolderPath).AsTask();

		/// <summary>
		/// Gets the temporary folder as a <see cref="StorageFolder"/>.
		/// </summary>
		public static Task<StorageFolder> GetTemporaryFolderAsync()
			=> StorageFolder.GetFolderFromPathAsync(TemporaryFolderPath).AsTask();

		private static string GetPortableDataPath()
		{
			// User data must sit beside the executable the user runs, not the single-file
			// extraction folder that AppContext.BaseDirectory points at
			var portablePath = Path.Combine(AppRuntimeHelper.EffectivePath, "UserData");

			try
			{
				Directory.CreateDirectory(portablePath);

				// Probe for writability; the install folder may be read-only (e.g. Program Files)
				var probeFilePath = Path.Combine(portablePath, ".writeprobe");
				File.WriteAllBytes(probeFilePath, []);
				File.Delete(probeFilePath);

				return portablePath;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				var fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FilesPortable");
				Directory.CreateDirectory(fallbackPath);

				return fallbackPath;
			}
		}
	}
}
