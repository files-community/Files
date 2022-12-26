using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		public static async Task<ILocatableStorable> GetStorableFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			if (await storageService.DirectoryExistsAsync(path, cancellationToken))
			{
				return await storageService.GetFolderFromPathAsync(path, cancellationToken);
			}
			else
			{
				return await storageService.GetFileFromPathAsync(path, cancellationToken);
			}
		}

		public static async Task<ILocatableStorable?> TryGetStorableFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			return (ILocatableStorable?)await storageService.TryGetFolderFromPathAsync(path, cancellationToken) ?? await storageService.TryGetFileFromPathAsync(path, cancellationToken);
		}

		/// <inheritdoc cref="IStorageService.GetFolderFromPathAsync"/>
		public static async Task<ILocatableFolder?> TryGetFolderFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			try
			{
				return await storageService.GetFolderFromPathAsync(path, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <inheritdoc cref="IStorageService.GetFileFromPathAsync"/>
		public static async Task<ILocatableFile?> TryGetFileFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			try
			{
				return await storageService.GetFileFromPathAsync(path, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
