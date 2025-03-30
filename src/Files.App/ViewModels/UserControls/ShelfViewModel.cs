// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using Files.Shared.Utils;

namespace Files.App.ViewModels.UserControls
{
	[Bindable(true)]
	public sealed partial class ShelfViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly Dictionary<string, (IFolderWatcher, int)> _watchers;

		public ObservableCollection<ShelfItem> Items { get; }

		public ShelfViewModel()
		{
			_watchers = new();
			Items = new();
			Items.CollectionChanged += Items_CollectionChanged;
		}

		/// <inheritdoc/>
		public Task InitAsync(CancellationToken cancellationToken = default)
		{
			// TODO: Load persisted shelf items
			return Task.CompletedTask;
		}

		[RelayCommand]
		private void ClearItems()
		{
			Items.Clear();
		}

		private async void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add when e.NewItems is not null:
				{
					if (e.NewItems[0] is not ShelfItem shelfItem)
						return;

					var parentPath = SystemIO.Path.GetDirectoryName(shelfItem.Inner.Id) ?? string.Empty;
					if (_watchers.TryGetValue(parentPath, out var reference))
					{
						// Only increase the reference count if the watcher already exists
						reference.Item2++;
						return;
					}
					
					if (await shelfItem.Inner.GetParentAsync() is not IMutableFolder mutableFolder)
						return;

					// Register new watcher
					var watcher = await mutableFolder.GetFolderWatcherAsync();
					watcher.CollectionChanged += Watcher_CollectionChanged;

					_watchers.Add(parentPath, (watcher, 1));
					break;
				}

				case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
				{
					if (e.OldItems[0] is not ShelfItem shelfItem)
						return;

					var parentPath = SystemIO.Path.GetDirectoryName(shelfItem.Inner.Id) ?? string.Empty;
					if (!_watchers.TryGetValue(parentPath, out var reference))
						return;

					// Decrease the reference count and remove the watcher if no references are present
					reference.Item2--;
					if (reference.Item2 < 1)
					{
						reference.Item1.CollectionChanged -= Watcher_CollectionChanged;
						reference.Item1.Dispose();
						_watchers.Remove(parentPath);
					}

					break;
				}
			}
		}

		private async void Watcher_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (sender is not IFolderWatcher watcher)
				return;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
				{
					// Remove the matching item notified from the watcher
					var item = e.OldItems.Cast<IStorable>().ElementAt(0);
					var itemToRemove = Items.FirstOrDefault(x => x.Inner.Id == item.Id);
					if (itemToRemove is null)
						return;

					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => Items.Remove(itemToRemove));
					break;
				}
			}
		}
	}
}
