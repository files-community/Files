// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
	/// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden,
	/// shortcut, or link items.
	/// </summary>
	public sealed partial class VirtualStorageItem : IStorageItem
	{
		private static BasicProperties? props;

		public Windows.Storage.FileAttributes Attributes { get; init; }

		public DateTimeOffset DateCreated { get; init; }

		public string Name { get; init; } = null!;

		public string Path { get; init; } = null!;

		private VirtualStorageItem() { }

		public static VirtualStorageItem FromListedItem(ListedItem item)
		{
			return new VirtualStorageItem()
			{
				Name = item.ItemNameRaw!,
				Path = item.ItemPath!,
				DateCreated = item.ItemDateCreatedReal,
				Attributes = item.IsArchive || item.PrimaryItemAttribute == StorageItemTypes.File ? Windows.Storage.FileAttributes.Normal : Windows.Storage.FileAttributes.Directory
			};
		}

		public static unsafe VirtualStorageItem? FromPath(string path)
		{
			WIN32_FIND_DATAW findData = default;
			using FindCloseSafeHandle hFile = PInvoke.FindFirstFileEx(path, FINDEX_INFO_LEVELS.FindExInfoBasic, &findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			if (!hFile.IsInvalid)
			{
				// https://learn.microsoft.com/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
				bool isReparsePoint = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
				bool isSymlink = isReparsePoint && findData.dwReserved0 == PInvoke.IO_REPARSE_TAG_SYMLINK;
				bool isHidden = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;
				bool isDirectory = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;

				if (!(isHidden && isSymlink))
				{
					DateTime itemCreatedDate;

					try
					{
						PInvoke.FileTimeToSystemTime(findData.ftCreationTime, out SYSTEMTIME systemCreatedDateOutput);
						itemCreatedDate = systemCreatedDateOutput.ToDateTime();
					}
					catch (ArgumentException)
					{
						// Invalid date means invalid findData, do not add to list
						return null;
					}

					return new VirtualStorageItem()
					{
						Name = findData.cFileName.ToString(),
						Path = path,
						DateCreated = itemCreatedDate,
						Attributes = isDirectory ? Windows.Storage.FileAttributes.Directory : Windows.Storage.FileAttributes.Normal
					};
				}
			}

			return null;
		}

		private async void StreamedFileWriterAsync(StreamedFileDataRequest request)
		{
			try
			{
				await using (var stream = request.AsStreamForWrite())
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
					var streamedFile = await StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriterAsync, null);
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
