using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.ExtendableStorage;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using Files.Sdk.Storage.MutableStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IFolder"/>
	public sealed class NativeFolder : NativeStorable, ILocatableFolder, IModifiableFolder, IMutableFolder, IFolderExtended
	{
		public NativeFolder(string path)
			: base(path)
		{
		}

		/// <inheritdoc/>
		public Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			var path = System.IO.Path.Combine(Path, fileName);

			if (!File.Exists(path))
				throw new FileNotFoundException();

			return Task.FromResult<IFile>(new NativeFile(path));
		}

		/// <inheritdoc/>
		public Task<IFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			var path = System.IO.Path.Combine(Path, folderName);

			if (!Directory.Exists(path))
				throw new FileNotFoundException();

			return Task.FromResult<IFolder>(new NativeFolder(path));
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (kind == StorableKind.Files)
			{
				foreach (var item in Directory.EnumerateFiles(Path))
					yield return new NativeFile(item);
			}
			else if (kind == StorableKind.Folders)
			{
				foreach (var item in Directory.EnumerateDirectories(Path))
					yield return new NativeFolder(item);
			}
			else
			{
				foreach (var item in Directory.EnumerateFileSystemEntries(Path))
				{
					if (File.Exists(item))
						yield return new NativeFile(item);
					else
						yield return new NativeFolder(item);
				}
			}

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task DeleteAsync(IStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			_ = permanently;

			if (item is ILocatableFile locatableFile)
			{
				File.Delete(locatableFile.Path);
			}
			else if (item is ILocatableFolder locatableFolder)
			{
				Directory.Delete(locatableFolder.Path, true);
			}
			else
				throw new ArgumentException($"Could not delete {item}.");

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is IFile sourceFile)
			{
				if (itemToCopy is ILocatableFile sourceLocatableFile)
				{
					var newPath = System.IO.Path.Combine(Path, itemToCopy.Name);
					File.Copy(sourceLocatableFile.Path, newPath, overwrite);

					return new NativeFile(newPath);
				}

				var copiedFile = await CreateFileAsync(itemToCopy.Name, overwrite, cancellationToken);
				await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);

				return copiedFile;
			}
			else if (itemToCopy is IFolder sourceFolder)
			{
				// TODO: Implement folder copy
				throw new NotSupportedException();
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IStorable> MoveFromAsync(IStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is IFile sourceFile)
			{
				if (itemToMove is ILocatableFile sourceLocatableFile)
				{
					var newPath = System.IO.Path.Combine(Path, itemToMove.Name);
					File.Move(sourceLocatableFile.Path, newPath, overwrite);

					return new NativeFile(newPath);
				}

				var copiedFile = await CreateFileAsync(itemToMove.Name, overwrite, cancellationToken);
				await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);
				await source.DeleteAsync(itemToMove, true, cancellationToken);

				return copiedFile;
			}
			else if (itemToMove is IFolder sourceFolder)
			{
				throw new NotImplementedException();
			}

			throw new ArgumentException($"Could not move type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var path = System.IO.Path.Combine(Path, desiredName);
			if (overwrite || !File.Exists(path))
				await File.Create(path).DisposeAsync();

			return new NativeFile(path);
		}

		/// <inheritdoc/>
		public Task<IFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var path = System.IO.Path.Combine(Path, desiredName);
			if (overwrite)
				Directory.Delete(path, true);

			_ = Directory.CreateDirectory(path);
			return Task.FromResult<IFolder>(new NativeFolder(path));
		}

		/// <inheritdoc/>
		public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IFolderWatcher>(new NativeFolderWatcher(this));
		}
	}
}
