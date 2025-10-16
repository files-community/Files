// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Storage;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Files.App.BackgroundTasks
{
	public sealed class UpdateTask : IBackgroundTask
	{
		public async void Run(IBackgroundTaskInstance taskInstance) => await RunAsync(taskInstance);

		private async Task RunAsync(IBackgroundTaskInstance taskInstance)
		{
			var deferral = taskInstance.GetDeferral();

			// Sync the jump list with Explorer
			try { RefreshJumpList(); } catch { }

			// Delete previous version log files
			try { DeleteLogFiles(); } catch { }

			deferral.Complete();
		}

		private void DeleteLogFiles()
		{
			File.Delete(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log"));
			File.Delete(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug_fulltrust.log"));
		}

		private void RefreshJumpList()
		{
			// Make sure to delete the Files' custom destinations binary files
			var recentFolder = JumpListManager.Default.GetRecentFolderPath();
			File.Delete($"{recentFolder}\\CustomDestinations\\3b19d860a346d7da.customDestinations-ms");
			File.Delete($"{recentFolder}\\CustomDestinations\\1265066178db259d.customDestinations-ms");
			File.Delete($"{recentFolder}\\CustomDestinations\\8e2322986488aba5.customDestinations-ms");
			File.Delete($"{recentFolder}\\CustomDestinations\\6b0bf5ca007c8bea.customDestinations-ms");

			_ = STATask.Run(() =>
			{
				JumpListManager.Default.PullJumpListFromExplorer();
			});
		}
	}
}
