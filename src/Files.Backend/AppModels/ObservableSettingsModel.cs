using Files.Sdk.Storage.MutableStorage;
using Files.Shared.Utils;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace Files.Backend.AppModels
{
	/// <inheritdoc cref="IPersistable"/>
	public abstract class ObservableSettingsModel : SettingsModel, IDisposable
	{
		private readonly IFolderWatcher _folderWatcher;
		private readonly string _filter;

		protected ObservableSettingsModel(IFolderWatcher folderWatcher, string filter)
		{
			_folderWatcher = folderWatcher;
			_filter = filter;
			_folderWatcher.CollectionChanged += FolderWatcher_CollectionChanged;
		}

		private async void FolderWatcher_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems is null)
				return;

			// Check if contents of an object changed
			if (e.Action != NotifyCollectionChangedAction.Replace)
				return;

			var item = Path.GetFileName(e.NewItems.Cast<string>().FirstOrDefault());
			if (false) // TODO: Use filter
				await LoadAsync();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			_folderWatcher.CollectionChanged -= FolderWatcher_CollectionChanged;
			_folderWatcher.Dispose();
		}
	}
}
