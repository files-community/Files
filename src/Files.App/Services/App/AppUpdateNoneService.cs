// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	internal sealed class AppUpdateNoneService : IAppUpdateService
	{
		public bool IsUpdateAvailable => false;

		public bool IsUpdating => false;

		public bool IsAppUpdated => true;

		public bool IsReleaseNotesAvailable => true;

		public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

		public Task CheckAndUpdateFilesLauncherAsync()
		{
			return Task.CompletedTask;
		}

		public Task CheckForUpdatesAsync()
		{
			return Task.CompletedTask;
		}

		public Task CheckLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		public Task DownloadMandatoryUpdatesAsync()
		{
			return Task.CompletedTask;
		}

		public Task DownloadUpdatesAsync()
		{
			return Task.CompletedTask;
		}

		public Task<string?> GetLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
		{
			// No localization for dev-only string
			return Task.FromResult((string?)"No release notes available for Dev build.");
		}
	}
}
