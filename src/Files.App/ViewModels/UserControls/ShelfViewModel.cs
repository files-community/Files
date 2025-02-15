// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using Files.Shared.Utils;

namespace Files.App.ViewModels.UserControls
{
	[Bindable(true)]
	public sealed partial class ShelfViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly Dictionary<string, IFolderWatcher> _watchers;

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

		private async void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add when e.NewItems is not null:
				{
					if (e.NewItems[0] is not INestedStorable nestedStorable)
						return;

					var parentPath = SystemIO.Path.GetDirectoryName(nestedStorable.Id) ?? string.Empty;
					if (_watchers.ContainsKey(parentPath))
						return;
					
					if (await nestedStorable.GetParentAsync() is not IMutableFolder mutableFolder)
						return;

					// TODO: Register IFolderWatcher

					break;
				}

				case NotifyCollectionChangedAction.Remove:

					break;
			}
		}
	}
}
