// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Filesystem.StorageItems
{
	/// <summary>
	/// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
	/// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden,
	/// shortcut, or link items.
	/// </summary>
	public class VirtualStorageItem : IStorageItem
	{
		private static BasicProperties props;

		public Windows.Storage.FileAttributes Attributes { get; init; }

		public DateTimeOffset DateCreated { get; init; }

		public string Name { get; init; }

		public string Path { get; init; }

		private VirtualStorageItem() { }

		public static VirtualStorageItem FromListedItem(ListedItem item)
		{
			return new VirtualStorageItem()
			{
				Name = item.ItemNameRaw,
				Path = item.ItemPath,
				DateCreated = item.ItemDateCreatedReal,
				Attributes = item.IsArchive || item.PrimaryItemAttribute == StorageItemTypes.File ? Windows.Storage.FileAttributes.Normal : Windows.Storage.FileAttributes.Directory
			};
		}

		private async void StreamedFileWriter(StreamedFileDataRequest request)
		{
			try
			{
				using (var stream = request.AsStreamForWrite())
				{
					await stream.FlushAsync();
				}
				request.Dispose();
			}
			catch (Exception)
			{
				request.FailAndClose(StreamedFileFailureMode.Incomplete);
			}
		}

		public IAsyncAction RenameAsync(string desiredName)
		{
			throw new NotImplementedException();
		}

		public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
		{
			throw new NotImplementedException();
		}

		public IAsyncAction DeleteAsync()
		{
			throw new NotImplementedException();
		}

		public IAsyncAction DeleteAsync(StorageDeleteOption option)
		{
			throw new NotImplementedException();
		}

		public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				async Task<BasicProperties> GetFakeBasicProperties()
				{
					var streamedFile = await StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriter, null);
					return await streamedFile.GetBasicPropertiesAsync();
				}
				return props ?? (props = await GetFakeBasicProperties());
			});
		}

		public bool IsOfType(StorageItemTypes type)
		{
			return Attributes.HasFlag(Windows.Storage.FileAttributes.Directory) ? type == StorageItemTypes.Folder : type == StorageItemTypes.File;
		}
	}
}
