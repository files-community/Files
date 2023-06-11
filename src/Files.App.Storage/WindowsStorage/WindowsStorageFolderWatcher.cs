using Files.Sdk.Storage.MutableStorage;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.App.Storage.WindowsStorage
{
	public class WindowsStorageFolderWatcher : IFolderWatcher
	{
		public IMutableFolder Folder { get; }

		private CancellationTokenSource watcherTokenSource;
		private WindowsStorageFolder folder => (WindowsStorageFolder)Folder;

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		// These aren't intended for use beyond triggering a collection refresh
		public event EventHandler<FileSystemEventArgs> ItemAdded;
		public event EventHandler<FileSystemEventArgs> ItemRemoved;
		public event EventHandler<FileSystemEventArgs> ItemChanged;
		public event EventHandler<RenamedEventArgs> ItemRenamed;

		public WindowsStorageFolderWatcher(IMutableFolder folder)
		{
			Folder = folder;
		}

		private Task WatchForStorageFolderChangesAsync()
		{
			if (folder is null)
				return Task.FromException(new NullReferenceException());

			return Task.Factory.StartNew(() =>
			{
				var options = new QueryOptions()
				{
					FolderDepth = FolderDepth.Shallow,
					IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
				};

				options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
				options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);

				if (folder.storage.AreQueryOptionsSupported(options))
				{
					var itemQueryResult = folder.storage.CreateItemQueryWithOptions(options);
					itemQueryResult.ContentsChanged += ItemQueryResult_ContentsChanged;

					// Just get one item to start getting notifications
					var watchedItemsOperation = itemQueryResult.GetItemsAsync(0, 1);

					watcherTokenSource.Token.Register(() =>
					{
						itemQueryResult.ContentsChanged -= ItemQueryResult_ContentsChanged;
						watchedItemsOperation?.Cancel();
					});
				}
			},
			default,
			TaskCreationOptions.LongRunning,
			TaskScheduler.Default);
		}

		private void ItemQueryResult_ContentsChanged(IStorageQueryResultBase sender, object args)
		{
			// Query options have to be reapplied otherwise old results are returned
			var options = new QueryOptions()
			{
				FolderDepth = FolderDepth.Shallow,
				IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
			};

			options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
			options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);

			sender.ApplyNewQueryOptions(options);

			ItemAdded?.Invoke(sender, default);
			ItemRemoved?.Invoke(sender, default);
			ItemChanged?.Invoke(sender, default);
			ItemRenamed?.Invoke(sender, default);

			CollectionChanged?.Invoke(sender, new(NotifyCollectionChangedAction.Reset));
		}

		public void Dispose()
		{
			watcherTokenSource.Cancel();
			watcherTokenSource.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}

		public async void Start()
		{
			watcherTokenSource = new CancellationTokenSource();
			await WatchForStorageFolderChangesAsync();
		}

		public void Stop()
		{
			watcherTokenSource.Cancel();
		}
	}
}
