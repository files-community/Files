﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface IUpdateService : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets a value indicating whether updates are available.
		/// </summary>
		bool IsUpdateAvailable { get; }

		/// <summary>
		/// Gets a value indicating if an update is in progress.
		/// </summary>
		bool IsUpdating { get; }

		/// <summary>
		/// Gets a value indicating if the apps being used the first time after an update.
		/// </summary>
		bool IsAppUpdated { get; }

		/// <summary>
		/// Gets a value indicating if release notes are available.
		/// </summary>
		bool IsReleaseNotesAvailable { get; }

		Task DownloadUpdatesAsync();

		Task DownloadMandatoryUpdatesAsync();

		Task CheckForUpdatesAsync();

		Task CheckLatestReleaseNotesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets release notes for the latest release
		/// </summary>
		Task<string?> GetLatestReleaseNotesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Replace Files.App.Launcher.exe if it is used and has been updated
		/// </summary>
		Task CheckAndUpdateFilesLauncherAsync();
	}
}
