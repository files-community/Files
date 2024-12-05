namespace Files.App.Services
{
	internal sealed class DummyUpdateService : IUpdateService
	{
		public bool IsUpdateAvailable => false;

		public bool IsUpdating => false;

		public bool IsAppUpdated => false;

		public bool AreReleaseNotesAvailable => false;

		public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

		public Task CheckAndUpdateFilesLauncherAsync()
		{
			return Task.CompletedTask;
		}

		public Task CheckForUpdatesAsync()
		{
			return Task.CompletedTask;
		}
		
		public Task CheckForReleaseNotesAsync()
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
	}
}
