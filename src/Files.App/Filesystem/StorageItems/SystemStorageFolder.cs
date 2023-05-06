// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.App.Filesystem.StorageItems
{
	public sealed class SystemStorageFolder : BaseStorageFolder
	{
		public StorageFolder Folder { get; }

		public override string Path => Folder.Path;
		public override string Name => Folder.Name;
		public override string DisplayName => Folder.DisplayName;
		public override string DisplayType => Folder.DisplayType;
		public override string FolderRelativeId => Folder.FolderRelativeId;

		public override DateTimeOffset DateCreated => Folder.DateCreated;
		public override FileAttributes Attributes => Folder.Attributes;
		public override IStorageItemExtraProperties Properties => Folder.Properties;

		public SystemStorageFolder(StorageFolder folder) => Folder = folder;

		public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => new SystemStorageFolder(await StorageFolder.GetFolderFromPathAsync(path)));

		public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => Task.FromResult(Folder).AsAsyncOperation();

		public override bool IsEqual(IStorageItem item) => Folder.IsEqual(item);
		public override bool IsOfType(StorageItemTypes type) => Folder.IsOfType(type);

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => new SystemStorageFolder(await Folder.GetParentAsync()));
		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
			=> AsyncInfo.Run<BaseBasicProperties>(async (cancellationToken) => new SystemFolderBasicProperties(await Folder.GetBasicPropertiesAsync()));

		public override IAsyncOperation<IndexedState> GetIndexedStateAsync() => Folder.GetIndexedStateAsync();

		public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
			=> Folder.GetItemAsync(name);
		public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name)
			=> Folder.TryGetItemAsync(name);
		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
			=> Folder.GetItemsAsync();
		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
			=> Folder.GetItemsAsync(startIndex, maxItemsToRetrieve);

		public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
			=> AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => new SystemStorageFile(await Folder.GetFileAsync(name)));
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
				=> (await Folder.GetFilesAsync()).Select(item => new SystemStorageFile(item)).ToList()
			);
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
				=> (await Folder.GetFilesAsync(query)).Select(x => new SystemStorageFile(x)).ToList());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
				=> (await Folder.GetFilesAsync(query, startIndex, maxItemsToRetrieve)).Select(x => new SystemStorageFile(x)).ToList());

		public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => new SystemStorageFolder(await Folder.GetFolderAsync(name)));
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken)
				=> (await Folder.GetFoldersAsync()).Select(item => new SystemStorageFolder(item)).ToList()
			);
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken)
				=> (await Folder.GetFoldersAsync(query)).Select(x => new SystemStorageFolder(x)).ToList());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken)
				=> (await Folder.GetFoldersAsync(query, startIndex, maxItemsToRetrieve)).Select(x => new SystemStorageFolder(x)).ToList());

		public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
			=> AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => new SystemStorageFile(await Folder.CreateFileAsync(desiredName)));
		public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
			=> AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => new SystemStorageFile(await Folder.CreateFileAsync(desiredName, options)));

		public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => new SystemStorageFolder(await Folder.CreateFolderAsync(desiredName)));
		public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => new SystemStorageFolder(await Folder.CreateFolderAsync(desiredName, options)));

		public override IAsyncAction RenameAsync(string desiredName) => Folder.RenameAsync(desiredName);
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => Folder.RenameAsync(desiredName, option);

		public override IAsyncAction DeleteAsync() => Folder.DeleteAsync();
		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => Folder.DeleteAsync(option);

		public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => Folder.AreQueryOptionsSupported(queryOptions);
		public override bool IsCommonFileQuerySupported(CommonFileQuery query) => Folder.IsCommonFileQuerySupported(query);
		public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => Folder.IsCommonFolderQuerySupported(query);

		public override StorageItemQueryResult CreateItemQuery()
			=> Folder.CreateItemQuery();
		public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions)
			=> new SystemStorageItemQueryResult(Folder.CreateItemQueryWithOptions(queryOptions));

		public override StorageFileQueryResult CreateFileQuery()
			=> Folder.CreateFileQuery();
		public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query)
			=> Folder.CreateFileQuery(query);
		public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions)
			=> new SystemStorageFileQueryResult(Folder.CreateFileQueryWithOptions(queryOptions));

		public override StorageFolderQueryResult CreateFolderQuery()
			=> Folder.CreateFolderQuery();
		public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query)
			=> Folder.CreateFolderQuery(query);
		public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions)
			=> new SystemStorageFolderQueryResult(Folder.CreateFolderQueryWithOptions(queryOptions));

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> Folder.GetThumbnailAsync(mode);
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> Folder.GetThumbnailAsync(mode, requestedSize);
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> Folder.GetThumbnailAsync(mode, requestedSize, options);

		private class SystemFolderBasicProperties : BaseBasicProperties
		{
			private readonly IStorageItemExtraProperties basicProps;

			public override ulong Size => (basicProps as BasicProperties)?.Size ?? 0;

			public override DateTimeOffset ItemDate => (basicProps as BasicProperties)?.ItemDate ?? DateTimeOffset.Now;
			public override DateTimeOffset DateModified => (basicProps as BasicProperties)?.DateModified ?? DateTimeOffset.Now;

			public SystemFolderBasicProperties(IStorageItemExtraProperties basicProps) => this.basicProps = basicProps;

			public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
				=> basicProps.RetrievePropertiesAsync(propertiesToRetrieve);

			public override IAsyncAction SavePropertiesAsync()
				=> basicProps.SavePropertiesAsync();
			public override IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
				=> basicProps.SavePropertiesAsync(propertiesToSave);
		}
	}
}
