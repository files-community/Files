// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Utils
{
	public sealed class RecentItemsManager
	{
		private static readonly Lazy<RecentItemsManager> lazy = new(() => new RecentItemsManager());
		private static readonly string recentItemsPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
		private static readonly string automaticDestinationsPath = Path.Combine(recentItemsPath, "AutomaticDestinations");
		private const string QuickAccessJumpListFileName = "5f7b5f1e01b83767.automaticDestinations-ms";
		private const string QuickAccessGuid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";
		private DateTime quickAccessLastReadTime = DateTime.MinValue;
		private FileSystemWatcher? quickAccessJumpListWatcher;

		public event EventHandler? RecentItemsChanged;

		public static RecentItemsManager Default
			=> lazy.Value;

		private RecentItemsManager()
		{
			Initialize();
		}

		private void Initialize()
		{
			StartQuickAccessJumpListWatcher();
		}

		private void StartQuickAccessJumpListWatcher()
		{
			if (quickAccessJumpListWatcher is not null)
				return;

			quickAccessJumpListWatcher = new FileSystemWatcher
			{
				Path = automaticDestinationsPath,
				Filter = QuickAccessJumpListFileName,
				NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
			};
			quickAccessJumpListWatcher.Changed += QuickAccessJumpList_Changed;
			quickAccessJumpListWatcher.Deleted += QuickAccessJumpList_Changed;
			quickAccessJumpListWatcher.EnableRaisingEvents = true;
		}

		private void QuickAccessJumpList_Changed(object sender, FileSystemEventArgs e)
		{
			Debug.WriteLine($"{nameof(QuickAccessJumpList_Changed)}: {e.ChangeType}, {e.FullPath}");

			// Skip if multiple events occurred for singular change
			var lastWriteTime = File.GetLastWriteTime(e.FullPath);
			if (quickAccessLastReadTime >= lastWriteTime)
				return;
			else
				quickAccessLastReadTime = lastWriteTime;

			RecentItemsChanged?.Invoke(this, e);
		}

		private void Unregister()
		{
			quickAccessJumpListWatcher?.Dispose();
		}

		~RecentItemsManager()
		{
			Unregister();
		}
	}
}
