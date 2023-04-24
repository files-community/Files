// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using Files.App.Helpers;
using Files.Shared.Extensions;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.App.Filesystem.StorageItems
{
	public sealed class FtpStorageFolder : BaseStorageFolder
	{
		public override string Path { get; }
		public override string Name { get; }
		public override string DisplayName => Name;
		public override string DisplayType => "Folder".GetLocalizedResource();
		public string FtpPath { get; }
		public override string FolderRelativeId => $"0\\{Name}";

		public override DateTimeOffset DateCreated { get; }
		public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Directory;
		public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

		public FtpStorageFolder(string path, string name, DateTimeOffset dateCreated)
		{
			Path = path;
			Name = name;
			FtpPath = FtpHelpers.GetFtpPath(path);
			DateCreated = dateCreated;
		}
		public FtpStorageFolder(string folder, FtpListItem ftpItem)
		{
			Path = PathNormalization.Combine(folder, ftpItem.Name);
			Name = ftpItem.Name;
			FtpPath = FtpHelpers.GetFtpPath(Path);
			DateCreated = ftpItem.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : ftpItem.RawCreated;
		}
		public FtpStorageFolder(IStorageItemWithPath item)
		{
			Path = item.Path;
			Name = System.IO.Path.GetFileName(item.Path);
			FtpPath = FtpHelpers.GetFtpPath(item.Path);
		}

		public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
			=> FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path)
				? Task.FromResult<BaseStorageFolder>(new FtpStorageFolder(new StorageFolderWithPath(null, path))).AsAsyncOperation()
				: Task.FromResult<BaseStorageFolder>(null).AsAsyncOperation();

		public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => throw new NotSupportedException();

		public FtpStorageFolder CloneWithPath(string path) => new(new StorageFolderWithPath(null, path));

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;
		public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.Folder;

		public override IAsyncOperation<IndexedState> GetIndexedStateAsync() => Task.FromResult(IndexedState.NotIndexed).AsAsyncOperation();

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return new BaseBasicProperties();
				}

				var item = await ftpClient.GetObjectInfo(FtpPath);
				return item is null ? new BaseBasicProperties() : new FtpFolderBasicProperties(item);
			});
		}

		public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
		{
			return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				var item = await ftpClient.GetObjectInfo(FtpHelpers.GetFtpPath(PathNormalization.Combine(Path, name)));
				if (item is not null)
				{
					if (item.Type is FtpObjectType.File)
					{
						return new FtpStorageFile(Path, item);
					}
					if (item.Type is FtpObjectType.Directory)
					{
						return new FtpStorageFolder(Path, item);
					}
				}
				return null;
			});
		}
		public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				try
				{
					return await GetItemAsync(name);
				}
				catch
				{
					return null;
				}
			});
		}
		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				var items = new List<IStorageItem>();
				var list = await ftpClient.GetListing(FtpPath);
				foreach (var item in list)
				{
					if (item.Type is FtpObjectType.File)
					{
						items.Add(new FtpStorageFile(Path, item));
					}
					else if (item.Type is FtpObjectType.Directory)
					{
						items.Add(new FtpStorageFolder(Path, item));
					}
				}
				return (IReadOnlyList<IStorageItem>)items;
			});
		}
		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
			=> AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken)
				=> (await GetItemsAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList());

		public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
			=> AsyncInfo.Run(async (cancellationToken) => await GetItemAsync(name) as BaseStorageFile);
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<FtpStorageFile>().ToList());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
			=> AsyncInfo.Run(async (cancellationToken) => await GetFilesAsync());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
				=> (await GetFilesAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList());

		public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
			=> AsyncInfo.Run(async (cancellationToken) => await GetItemAsync(name) as BaseStorageFolder);
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<FtpStorageFolder>().ToList());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
			=> AsyncInfo.Run(async (cancellationToken) => await GetFoldersAsync());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken)
				=> (await GetFoldersAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList());

		public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
			=> CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
		{
			return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				using var stream = new MemoryStream();

				string remotePath = $"{FtpPath}/{desiredName}";
				var ftpRemoteExists = options is CreationCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

				var result = await ftpClient.UploadStream(stream, remotePath, ftpRemoteExists);
				if (result is FtpStatus.Success)
				{
					return new FtpStorageFile(new StorageFileWithPath(null, $"{Path}/{desiredName}"));
				}
				if (result is FtpStatus.Skipped)
				{
					if (options is CreationCollisionOption.FailIfExists)
					{
						throw new IOException("File already exists.");
					}

					return null;
				}

				throw new IOException($"Failed to create file {remotePath}.");
			});
		}

		public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
			=> CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
		{
			return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					throw new IOException($"Failed to connect to FTP server.");
				}

				string fileName = $"{FtpPath}/{desiredName}";
				if (await ftpClient.DirectoryExists(fileName))
				{
					return new FtpStorageFolder(new StorageFileWithPath(null, fileName));
				}

				bool replaceExisting = options is CreationCollisionOption.ReplaceExisting;
				bool isSuccessful = await ftpClient.CreateDirectory(fileName, replaceExisting, cancellationToken);
				if (!isSuccessful)
				{
					throw new IOException($"Failed to create folder {desiredName}.");
				}

				return new FtpStorageFolder(new StorageFileWithPath(null, $"{Path}/{desiredName}"));
			});
		}

		public override IAsyncAction RenameAsync(string desiredName)
			=> RenameAsync(desiredName, NameCollisionOption.FailIfExists);
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return;
				}

				string destination = $"{PathNormalization.GetParentDir(FtpPath)}/{desiredName}";
				var ftpOption = option is NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
				bool isSuccessful = await ftpClient.MoveDirectory(FtpPath, destination, ftpOption, cancellationToken);
				if (!isSuccessful && option is NameCollisionOption.GenerateUniqueName)
				{
					// TODO: handle name generation
				}
			});
		}

		public override IAsyncAction DeleteAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				using var ftpClient = GetFtpClient();
				if (await ftpClient.EnsureConnectedAsync())
				{
					await ftpClient.DeleteDirectory(FtpPath, cancellationToken);
				}
			});
		}
		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => DeleteAsync();

		public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => false;
		public override bool IsCommonFileQuerySupported(CommonFileQuery query) => false;
		public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => false;

		public override StorageItemQueryResult CreateItemQuery() => throw new NotSupportedException();
		public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

		public override StorageFileQueryResult CreateFileQuery() => throw new NotSupportedException();
		public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query) => throw new NotSupportedException();
		public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

		public override StorageFolderQueryResult CreateFolderQuery() => throw new NotSupportedException();
		public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query) => throw new NotSupportedException();
		public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

		private AsyncFtpClient GetFtpClient()
		{
			string host = FtpHelpers.GetFtpHost(Path);
			ushort port = FtpHelpers.GetFtpPort(Path);
			var credentials = FtpManager.Credentials.Get(host, FtpManager.Anonymous);

			return new(host, credentials, port);
		}

		private class FtpFolderBasicProperties : BaseBasicProperties
		{
			public override ulong Size { get; }

			public override DateTimeOffset ItemDate { get; }
			public override DateTimeOffset DateModified { get; }

			public FtpFolderBasicProperties(FtpListItem item)
			{
				Size = (ulong)item.Size;

				ItemDate = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
				DateModified = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
			}
		}
	}
}